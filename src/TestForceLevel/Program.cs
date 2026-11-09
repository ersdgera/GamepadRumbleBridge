using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TestForceLevel;

/// <summary>
/// Тестирование уровней силы вибрации геймпада.
/// Отправляет отчёты с разными значениями моторов и просит оценить разницу.
/// Также пытается прочитать Feature Report capability device.
/// </summary>
class Program
{
    const ushort TargetVid = 0x0079;
    const ushort TargetPid = 0x0006;

    // Коды из анализа драйвера
    static readonly byte[] CmdStop     = { 0x00, 0xf3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    static readonly byte[] CmdMotorOn  = { 0x00, 0xfa, 0xfe, 0x00, 0x00, 0x00, 0x00, 0x00 };

    // Попытка записать разные уровни в тот же Output Report 0x51
    // Байт 3 = малый мотор (position 4), байт 5 = большой мотор (position 6)
    static byte[] SetRumble(byte small, byte big)
        => new byte[] { 0x00, 0x51, 0x00, small, 0x00, big, 0x00, 0x00 };

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFile(string name, uint acc, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool CloseHandle(IntPtr h);
    [DllImport("hid.dll", SetLastError = true)] static extern void HidD_GetHidGuid(out Guid g);
    [DllImport("hid.dll", SetLastError = true)] static extern bool HidD_GetAttributes(IntPtr h, out HIDD_ATTR attr);
    [DllImport("hid.dll", SetLastError = true)] static extern bool HidD_SetOutputReport(IntPtr h, byte[] buf, int len);
    [DllImport("hid.dll", SetLastError = true)] static extern bool HidD_GetFeature(IntPtr h, byte[] buf, int len);
    [DllImport("hid.dll", SetLastError = true)] static extern bool HidD_SetFeature(IntPtr h, byte[] buf, int len);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid c, IntPtr e, IntPtr p, uint f);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, uint i, out SP_IFACE iface);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_IFACE iface,
        IntPtr d, uint sz, out uint needed, IntPtr di);
    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiGetDeviceRegistryProperty(IntPtr s, ref SP_IFACE iface,
        uint prop, out uint regType, byte[] propData, uint dataLen, out uint need);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr device, uint IoControlCode,
        byte[] InBuffer, uint nInBufferSize, byte[] OutBuffer, uint nOutBufferSize,
        out uint pBytesReturned, IntPtr Overlapped);

    [StructLayout(LayoutKind.Sequential)] struct HIDD_ATTR { public int Size; public ushort Vid; public ushort Pid; public ushort Ver; }
    [StructLayout(LayoutKind.Sequential)] struct SP_IFACE { public uint cbSize; public Guid ClassGuid; public uint Flags; public IntPtr Reserved; }

    const uint GENERIC_READ      = 0x80000000;
    const uint GENERIC_WRITE     = 0x40000000;
    const uint FILE_SHARE_READ   = 0x00000001;
    const uint FILE_SHARE_WRITE  = 0x00000002;
    const uint OPEN_EXISTING     = 3;
    const uint DIGCF_PRESENT     = 0x00000002;
    const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    static IntPtr OpenDevice(string path) =>
        CreateFile(path, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

    static string? FindGamepad()
    {
        Guid guid; HidD_GetHidGuid(out guid);
        IntPtr hSet = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (hSet == IntPtr.Zero || hSet == (IntPtr)(-1)) return null;
        try
        {
            uint idx = 0;
            while (true)
            {
                var iface = new SP_IFACE { cbSize = (uint)Marshal.SizeOf<SP_IFACE>() };
                if (!SetupDiEnumDeviceInterfaces(hSet, IntPtr.Zero, ref guid, idx, out iface)) break;
                uint needed = 0;
                SetupDiGetDeviceInterfaceDetail(hSet, ref iface, IntPtr.Zero, 0, out needed, IntPtr.Zero);
                if (needed < 16) { idx++; continue; }
                IntPtr ptr = Marshal.AllocHGlobal((int)needed);
                try
                {
                    Marshal.WriteInt32(ptr, 0, 8);
                    if (!SetupDiGetDeviceInterfaceDetail(hSet, ref iface, ptr, needed, out _, IntPtr.Zero))
                    { idx++; continue; }
                    char[] chars = new char[(int)needed / 2];
                    Marshal.Copy(ptr, chars, 0, chars.Length);
                    string full = new string(chars).TrimEnd('\0');
                    if (full.IndexOf("vid_0079", StringComparison.OrdinalIgnoreCase) >= 0
                        && full.IndexOf("pid_0006", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string path = new string(chars, 2, Math.Max(0, chars.Length - 2)).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(path) && path.StartsWith("\\"))
                        {
                            IntPtr test = OpenDevice(path);
                            if (test != IntPtr.Zero && test != (IntPtr)(-1))
                            {
                                try
                                {
                                    if (HidD_GetAttributes(test, out var a)
                                        && a.Vid == TargetVid && a.Pid == TargetPid)
                                        return path;
                                }
                                finally { CloseHandle(test); }
                            }
                        }
                    }
                }
                finally { Marshal.FreeHGlobal(ptr); }
                idx++;
            }
        }
        finally { SetupDiDestroyDeviceInfoList(hSet); }
        return null;
    }

    // Получить Hardware ID из реестра устройства
    static string? GetHardwareId(string path)
    {
        try
        {
            // ИзвлекаемInstanceId из пути устройства
            int sepIdx = path.LastIndexOf('\\');
            if (sepIdx < 0) return null;
            string instanceId = path.Substring(sepIdx + 1);
            string regPath = @"SYSTEM\CurrentControlSet\Enum\" + instanceId;
            object val = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath)?.GetValue("HardwareID");
            return val?.ToString();
        }
        catch { return null; }
    }

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        IntPtr h = IntPtr.Zero;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            string? path = FindGamepad();
            if (path != null)
            {
                h = OpenDevice(path);
                if (h != IntPtr.Zero && h != (IntPtr)(-1)) break;
            }
            Console.WriteLine($"  поиск геймпада... ({attempt+1}/20)");
            await Task.Delay(1000);
        }

        if (h == IntPtr.Zero || h == (IntPtr)(-1))
        {
            Console.WriteLine("Геймпад не найден! Подключите VID_0079 PID_0006.");
            return 1;
        }

        HidD_GetAttributes(h, out var attr);
        Console.WriteLine("=== ТЕСТ УРОВНЕЙ СИЛЫ ВИБРАЦИИ ===");
        Console.WriteLine($"VID = 0x{attr.Vid:X4}   PID = 0x{attr.Pid:X4}   VER = 0x{attr.Ver:X4}");
        Console.WriteLine(new string('-', 60));

        // Проверка Feature Report capability
        Console.WriteLine("\n[1] Проверка Feature Report поддержки...");
        bool featureSupported = false;
        byte[] featBuf = new byte[8];
        // Пробуем прочитать Feature Report 0x01 (если поддерживается)
        try
        {
            Array.Fill<byte>(featBuf, 0x01); // Report ID
            bool ok = HidD_GetFeature(h, featBuf, featBuf.Length);
            if (ok)
            {
                featureSupported = true;
                Console.WriteLine($"  ✓ Feature Report поддерживается");
                Console.WriteLine($"    Отчёт: [{BitConverter.ToString(featBuf)}]");
            }
            else
            {
                Console.WriteLine("  ✗ Feature Report не поддерживается (HidD_GetFeature вернул false)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ! Ошибка при чтении Feature Report: {ex.Message}");
        }

        // Получаем Hardware ID для анализа
        string? hwId = GetHardwareId(FindGamepad());
        if (!string.IsNullOrEmpty(hwId))
            Console.WriteLine($"  Hardware ID: {hwId}");

        Stop(h);
        Console.WriteLine("\n[2] Последовательный тест уровней силы\n");
        Console.WriteLine("Пожалуйста ДЕРЖИТЕ геймпад в руках и ОЦЕНИВАЙТЕ силу вибрации!");
        Console.WriteLine("(или положите на ровную поверхность чтобы слышать/видеть разницу)\n");

        // Этапы: стоп -> мин -> низкий -> средний -> высокий -> макс
        var stages = new (string label, byte[] setCmd, int waitMs)[]
        {
            ("СТОП  (0/0)          ", CmdStop,                         400),
            ("МИНИМУМ (1/1)         ", SetRumble(1,   1),             400),
            ("НИЗКИЙ   (64/64)      ", SetRumble(64,  64),            400),
            ("СРЕДНИЙ  (128/128)    ", SetRumble(128, 128),           400),
            ("ВЫСОКИЙ  (200/200)    ", SetRumble(200, 200),           400),
            ("МАКСИМУМ (255/255)    ", SetRumble(255, 255),           500),
            ("СТОП  (0/0)",                                            CmdStop, 500),
        };

        foreach (var (label, cmd, waitMs) in stages)
        {
            Send(h, cmd);
            Console.Write($"  {label,-24} ... ");
            Console.ForegroundColor = ConsoleColor.White;
            await Task.Delay(waitMs);
            Console.ResetColor();
            Console.WriteLine("готово");
            await Task.Delay(100);
        }

        // Теперь пробует поочерёдно: отправляем один уровень, ждём, сравниваем
        Console.WriteLine("\n[3] Детальный сравнительный тест\n");
        Console.WriteLine("Каждый уровень держится 0.8 сек. Оценивайте разницу визуально/на слух.\n");

        var levels = new (string label, byte s, byte b)[]
        {
            ("A: 0 (стоп)",          (byte)0,   (byte)0),
            ("B: 1  (около нуля)",  (byte)1,  (byte)1),
            ("C: 10  (почти незаметно)", (byte)10,  (byte)10),
            ("D: 50  (слабо)",      (byte)50,  (byte)50),
            ("E: 100 (ниже средн)",(byte)100, (byte)100),
            ("F: 150 (средне)",     (byte)150, (byte)150),
            ("G: 200 (сильно)",     (byte)200, (byte)200),
            ("H: 255 (максимум)",   (byte)255, (byte)255),
        };

        double[] timings = new double[levels.Length];
        for (int i = 0; i < levels.Length; i++)
        {
            var (lbl, s, b) = levels[i];
            Console.Write($"  {lbl,-28} ");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int j = 0; j < 5; j++)
            {
                Send(h, SetRumble(s, b));
                Send(h, CmdMotorOn);
                await Task.Delay(80);
                Send(h, CmdStop);
                await Task.Delay(80);
            }
            sw.Stop();
            timings[i] = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"{timings[i]:F0}мс (5 циклов)");
        }

        Stop(h);

        // Анализ
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("АНАЛИЗ РЕЗУЛЬТАТОВ:");
        Console.WriteLine(new string('-', 60));

        double minT = double.MaxValue, maxT = double.MinValue;
        int minIdx = 0, maxIdx = 0;
        for (int i = 0; i < timings.Length; i++)
        {
            if (timings[i] < minT) { minT = timings[i]; minIdx = i; }
            if (timings[i] > maxT) { maxT = timings[i]; maxIdx = i; }
        }

        // Если время отправки одинаково — значит контроллер всё равно шлёт максимум
        double ratio = (minT > 0) ? maxT / minT : 1.0;

        // Сравниваем первые и последние этапы (стоп vs макс)
        bool diffDetected = ratio > 1.05; // больше 5% разницы считаем заметной

        if (featureSupported)
        {
            Console.WriteLine("  Feature Report ПОДДЕРЖИВАЕТСЯ → возможны уровни силы!");
            Console.WriteLine("  Однако HID Output Report может ограничивать диапазон.");
        }
        else
        {
            Console.WriteLine("  Feature Report НЕ поддерживается → устройство работает в режиме ON/OFF.");
        }

        if (diffDetected)
        {
            Console.WriteLine($"\n  ⚠ Разница во времени отправки обнаружена (ratio={ratio:F2})");
            Console.WriteLine($"    Это может означать частичную поддержку уровней силы.");
        }
        else
        {
            Console.WriteLine($"\n  ✗ Разницы нет (ratio={ratio:F2}, близко к 1.0)");
            Console.WriteLine($"    Геймпад работает ТОЛЬКО в режиме ON/OFF");
            Console.WriteLine($"    Любое значение > 0 даёт максимальную вибрацию (255/255)");
        }

        Console.WriteLine("\nРЕКОМЕНДАЦИЯ:");
        Console.WriteLine("  Если уровни не работают — мост должен всегда слать 255/255 при любой вибрации.");
        Console.WriteLine("  Используйте Hotkey Ctrl+Shift+R для быстрого включения/выключения.");
        Console.WriteLine("=" + new string('=', 59));

        CloseHandle(h);
        return 0;
    }

    static void Send(IntPtr h, byte[] report)
    {
        HidD_SetOutputReport(h, report, report.Length);
    }

    static void Stop(IntPtr h)
    {
        HidD_SetOutputReport(h, CmdStop, CmdStop.Length);
    }
}
