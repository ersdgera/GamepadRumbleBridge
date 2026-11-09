using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using GamepadBridge.Native;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace GamepadBridge;

internal static class Program
{
    private const ushort TargetVid = 0x0079;
    private const ushort TargetPid = 0x0006;

    private static ViGEmClient _client;
    private static IXbox360Controller _xbox;
    private static SafeFileHandle _hidDevice;
    private static HidGamepadDecoder _decoder;

    private static byte _lastSmall = 0, _lastBig = 0;
    private static readonly object _rumbleLock = new();

    private static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "Generic USB Gamepad -> Virtual Xbox 360 Bridge";

        Console.WriteLine("==================================================================");
        Console.WriteLine("  Generic USB Gamepad (VID_0079&PID_0006) -> Xbox 360 Bridge");
        Console.WriteLine("  Поддержка Steam / DirectInput / XInput + Вибрация");
        Console.WriteLine("==================================================================");
        Console.WriteLine();

        try
        {
            _client = new ViGEmClient();
            Console.WriteLine("[+] ViGEmBus подключен успешно.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[-] Ошибка подключения к ViGEmBus: {ex.Message}");
            Console.WriteLine("    Убедитесь, что служба ViGEmBus запущенa.");
            Console.ResetColor();
            return;
        }

        string devicePath = FindDevice();
        if (devicePath == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[-] Геймпад VID_0079&PID_0006 не найден в системе.");
            Console.WriteLine("    Подключите геймпад к USB и перезапустите программу.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"[+] Найден геймпад: {devicePath}");

        _hidDevice = HidDevices.TryOpen(devicePath, writable: true);
        if (_hidDevice == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[-] Не удалось открыть HID-устройство для чтения/записи.");
            Console.ResetColor();
            return;
        }

        try
        {
            _decoder = new HidGamepadDecoder(_hidDevice, devicePath);
            Console.WriteLine($"[+] HID-декодер инициализирован. Размер отчёта: {_decoder.Caps.InputReportByteLength} байт.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[-] Ошибка декодера: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // Создаём виртуальный Xbox 360 контроллер
        _xbox = _client.CreateXbox360Controller();
        _xbox.FeedbackReceived += OnXboxFeedbackReceived;
        _xbox.Connect();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[+] Виртуальный Xbox 360 контроллер успешно создан в системе!");
        Console.WriteLine("    Steam и игры теперь видят ваш джойстик как полноценный Xbox 360 Controller.");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Работаем... Нажмите Ctrl+C для выхода.");

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Cleanup();
            Environment.Exit(0);
        };

        RunLoop();
    }

    private static string FindDevice()
    {
        var paths = HidDevices.EnumeratePaths();
        foreach (var p in paths)
        {
            if (HidDevices.TryParseVidPid(p, out ushort v, out ushort pid))
            {
                if (v == TargetVid && pid == TargetPid) return p;
            }
        }
        return null;
    }

    private static void OnXboxFeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
    {
        // Пересылаем вибрацию от игры (через Steam/XInput) на физический джойстик
        lock (_rumbleLock)
        {
            byte small = e.SmallMotor;
            byte big = e.LargeMotor;

            if (small != _lastSmall || big != _lastBig)
            {
                _lastSmall = small;
                _lastBig = big;
                MotorControl.SendRumble(_hidDevice, small, big);
            }
        }
    }

    private static void RunLoop()
    {
        int reportSize = Math.Max(1, (int)_decoder.Caps.InputReportByteLength);
        byte[] buffer = new byte[reportSize];

        short MapAxis(int val, int logMin = 0, int logMax = 255)
        {
            // Нормализация 0..255 -> -32768..32767
            double norm = (double)(val - logMin) / (logMax - logMin); // 0.0 .. 1.0
            norm = Math.Clamp(norm, 0.0, 1.0);
            return (short)Math.Round((norm * 65535.0) - 32768.0);
        }

        while (true)
        {
            if (!Hid.ReadFile(_hidDevice, buffer, (uint)buffer.Length, out uint read, IntPtr.Zero) || read == 0)
            {
                Thread.Sleep(10);
                continue;
            }

            var axes = _decoder.ReadAxes(buffer);
            var buttons = _decoder.ReadButtons(buffer);
            int hat = _decoder.ReadHat(buffer);

            // 1. Левый стик (X, Y)
            axes.TryGetValue(UsageDesk.X, out int rawX);
            axes.TryGetValue(UsageDesk.Y, out int rawY);
            _xbox.SetAxisValue(Xbox360Axis.LeftThumbX, MapAxis(rawX));
            // В Windows Y ось инвертирована по отношению к Xbox
            _xbox.SetAxisValue(Xbox360Axis.LeftThumbY, (short)-MapAxis(rawY));

            // 2. Правый стик (Z, Rz / Rx, Ry в зависимости от ревизии)
            axes.TryGetValue(UsageDesk.Z, out int rawRx);
            if (!axes.TryGetValue(UsageDesk.Rz, out int rawRy))
                axes.TryGetValue(UsageDesk.Ry, out rawRy);

            _xbox.SetAxisValue(Xbox360Axis.RightThumbX, MapAxis(rawRx));
            _xbox.SetAxisValue(Xbox360Axis.RightThumbY, (short)-MapAxis(rawRy));

            // 3. D-Pad / Крестовина (Hat switch)
            _xbox.SetButtonState(Xbox360Button.Up, hat == 1 || hat == 2 || hat == 8);
            _xbox.SetButtonState(Xbox360Button.Right, hat == 2 || hat == 3 || hat == 4);
            _xbox.SetButtonState(Xbox360Button.Down, hat == 4 || hat == 5 || hat == 6);
            _xbox.SetButtonState(Xbox360Button.Left, hat == 6 || hat == 7 || hat == 8);

            // 4. Кнопки действительности (A, B, X, Y, LB, RB, Back, Start, L3, R3)
            // Физические кнопки нумеруются с 1
            _xbox.SetButtonState(Xbox360Button.A, buttons.Contains(3) || buttons.Contains(1));
            _xbox.SetButtonState(Xbox360Button.B, buttons.Contains(2));
            _xbox.SetButtonState(Xbox360Button.X, buttons.Contains(4));
            _xbox.SetButtonState(Xbox360Button.Y, buttons.Contains(1) && !buttons.Contains(3)); // зашита логика раскладки

            _xbox.SetButtonState(Xbox360Button.LeftShoulder, buttons.Contains(5));
            _xbox.SetButtonState(Xbox360Button.RightShoulder, buttons.Contains(6));

            // Триггеры (LT / RT) — кнопки 7 и 8 на дешевых геймпадах
            _xbox.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(buttons.Contains(7) ? 255 : 0));
            _xbox.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(buttons.Contains(8) ? 255 : 0));

            _xbox.SetButtonState(Xbox360Button.Back, buttons.Contains(9));
            _xbox.SetButtonState(Xbox360Button.Start, buttons.Contains(10));
            _xbox.SetButtonState(Xbox360Button.LeftThumb, buttons.Contains(11));
            _xbox.SetButtonState(Xbox360Button.RightThumb, buttons.Contains(12));

            _xbox.SubmitReport();
            Thread.Sleep(5); // ~200 Гц опрос
        }
    }

    private static void Cleanup()
    {
        Console.WriteLine("\n[+] Завершение работы...");
        try { MotorControl.Stop(_hidDevice); } catch { }
        try { _xbox?.Disconnect(); } catch { }
        try { _client?.Dispose(); } catch { }
        try { _hidDevice?.Dispose(); } catch { }
    }
}
