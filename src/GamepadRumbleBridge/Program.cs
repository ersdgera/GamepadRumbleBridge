using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Xbox360EventArgs = Nefarius.ViGEm.Client.Targets.Xbox360.Xbox360FeedbackReceivedEventArgs;

namespace GamepadRumbleBridge;

public partial class MainForm : Form
{
    const ushort VirtVid = 0x045E;
    const ushort VirtPid = 0x028E;
    const string PipeName = "GamepadRumblePipe";
    const int MaxPads = 4;

    static readonly byte[] CmdStop = { 0x00, 0xf3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    static readonly byte[] VibeFull = { 0x00, 0x51, 0x00, 0xff, 0x00, 0xff, 0x00, 0x00 };

    volatile bool _rumbleOn = true;
    CancellationTokenSource _cts;
    readonly List<GamepadEntry> _pads = new();
    NotifyIcon _tray;
    ContextMenuStrip _menu;
    int _hotkeyId = 1000;
    ViGEmClient _vigemClient;

    struct GamepadEntry
    {
        public IXbox360Controller Controller;
        public IntPtr HidHandle;
        public string DevicePath;
        public bool Connected;
        public int RumbleScale; // 0-100 percent, default 100
    }

    public MainForm()
    {
        _vigemClient = new ViGEmClient();
        Console.WriteLine("[TAG1] Constructor called");
        for (int i = 0; i < MaxPads; i++) _pads.Add(new GamepadEntry());
        InitializeTray();
        SetStatus("Ждём геймпады...");
    }

    protected override void OnLoad(EventArgs e)
    {
        this.Visible = false;
        this.ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        base.OnLoad(e);
        RegisterHotKey(this.Handle, _hotkeyId, MOD_CONTROL | MOD_SHIFT, VK_R);
        _cts = new CancellationTokenSource();
        
        Thread hidThread = new Thread(() => HidMonitor(_cts.Token));
        hidThread.IsBackground = true;
        hidThread.Start();
        
        Thread pipeThread = new Thread(() => PipeServer(_cts.Token));
        pipeThread.IsBackground = true;
        pipeThread.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Console.WriteLine($"[TAG2] OnFormClosing Reason={e.CloseReason}");
        Cleanup();
        base.OnFormClosing(e);
    }

    void HidMonitor(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var currentPaths = new HashSet<string>(FindAllGamepadPaths());
                Console.WriteLine($"[HID] Scan: found {currentPaths.Count} paths [TAG3]");
                foreach (var p in currentPaths) Console.WriteLine($"[HID]   path={p}");

                // Collect currently-connected and pending paths to avoid race conditions
                var claimedPaths = new HashSet<string>();
                lock (_pads)
                {
                    foreach (var p in _pads)
                        if (p.Connected) claimedPaths.Add(p.DevicePath);
                }

                var toRemove = new List<int>();
                var newAssignments = new Dictionary<int, string>();

                lock (_pads)
                {
                    // Detect disconnections
                    for (int i = _pads.Count - 1; i >= 0; i--)
                    {
                        if (!_pads[i].Connected) continue;
                        if (currentPaths.Contains(_pads[i].DevicePath)) continue;
                        toRemove.Add(i);
                    }

                    // Find new unclaimed paths for empty slots
                    foreach (var path in currentPaths)
                    {
                        if (claimedPaths.Contains(path)) continue;
                        for (int slot = 0; slot < MaxPads; slot++)
                        {
                            if (_pads[slot].Connected) continue;
                            if (newAssignments.ContainsKey(slot)) continue;
                            newAssignments[slot] = path;
                            break;
                        }
                    }
                }

                foreach (int i in toRemove)
                {
                    try { Invoke(() => DisconnectPad(i)); } catch { }
                }

                foreach (var kv in newAssignments)
                {
                    try { Invoke(() => ConnectPad(kv.Key, kv.Value)); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HID] ERROR: {ex.Message} [TAG4]");
            }
            Thread.Sleep(3000);
        }
    }

    void ConnectPad(int slot, string path)
    {
        if (slot >= _pads.Count || _pads[slot].Connected) return;
        Console.WriteLine($"[UI] ConnectPad slot={slot} [TAG5]");
        IntPtr h = OpenDevice(path);
        Console.WriteLine($"[UI]   OpenDevice={(h==(IntPtr)(-1)?"FAIL":"OK")} [TAG6]");
        if (h == IntPtr.Zero || h == (IntPtr)(-1)) return;
        try
        {
            Console.WriteLine($"[UI]   Creating ViGEm ctrl for slot {slot} [TAG7]");
            var ctrl = _vigemClient.CreateXbox360Controller(VirtVid, VirtPid);
            ctrl.FeedbackReceived += OnXboxFeedbackReceived;
            var prop = ctrl.GetType().GetProperty("UserIndex", BindingFlags.Public | BindingFlags.Instance);
            prop?.SetValue(ctrl, slot);
            ctrl.Connect();
            Console.WriteLine($"[UI]   Connected! UI={ctrl.UserIndex} [TAG8]");
            lock (_pads)
            {
                _pads[slot] = new GamepadEntry { Controller = ctrl, HidHandle = h, DevicePath = path, Connected = true, RumbleScale = 100 };
            }
            SetStatus($"Геймпад #{slot + 1} подключён ({CountConnected()}/{MaxPads})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UI] ERROR: {ex.Message} [TAG9]");
            CloseHandle(h);
        }
    }

    void DisconnectPad(int idx)
    {
        if (idx < 0 || idx >= _pads.Count) return;
        Console.WriteLine($"[UI] DisconnectPad idx={idx} [TAG10]");
        StopPad(idx);
        lock (_pads)
        {
            var entry = _pads[idx];
            if (entry.HidHandle != IntPtr.Zero && entry.HidHandle != (IntPtr)(-1)) CloseHandle(entry.HidHandle);
            if (entry.Controller != null)
            {
                try { entry.Controller.Disconnect(); } catch { }
                entry.Controller.FeedbackReceived -= OnXboxFeedbackReceived;
            }
_pads[idx] = new GamepadEntry { RumbleScale = 100 };
        }
        SetStatus($"Геймпад #{idx + 1} отключён ({CountConnected()}/{MaxPads})");
    }

    void OnXboxFeedbackReceived(object sender, Xbox360EventArgs e)
    {
        Console.WriteLine($"[DBG] Feedback small={(byte)e.SmallMotor} large={(byte)e.LargeMotor} [TAG11]");
        int idx = -1;
        for (int i = 0; i < _pads.Count; i++)
        {
            if (_pads[i].Connected && _pads[i].Controller == sender) { idx = i; break; }
        }
        Console.WriteLine($"[DBG]   slot={idx} [TAG12]");
        if (idx >= 0) SendHidRumbleToPad(idx, e.SmallMotor, e.LargeMotor);
    }

    void SendHidRumbleToPad(int padIdx, byte small, byte big)
    {
        if (!_rumbleOn) return;
        lock (_pads)
        {
            if (padIdx < 0 || padIdx >= _pads.Count) return;
            var pad = _pads[padIdx];
            if (!pad.Connected || pad.HidHandle == IntPtr.Zero || pad.HidHandle == (IntPtr)(-1)) return;
            // Применяем коэффициент масштабирования для каждого геймпада (0-100%)
            int scale = Math.Max(0, Math.Min(100, pad.RumbleScale));
            byte s = (byte)((small * scale) / 100);
            byte b = (byte)((big * scale) / 100);
            Console.WriteLine($"[DBG] Sending rumble pad={padIdx} s={s} b={b} scale={scale}% [TAG16]");
            if (s == 0 && b == 0)
                HidD_SetOutputReport(pad.HidHandle, CmdStop, CmdStop.Length);
            else
                HidD_SetOutputReport(pad.HidHandle, VibeFull, VibeFull.Length);
        }
    }

    void StopPad(int idx)
    {
        lock (_pads)
        {
            if (idx < 0 || idx >= _pads.Count) return;
            var pad = _pads[idx];
            if (!pad.Connected || pad.HidHandle == IntPtr.Zero || pad.HidHandle == (IntPtr)(-1)) return;
            HidD_SetOutputReport(pad.HidHandle, CmdStop, CmdStop.Length);
        }
    }

    static List<string> FindAllGamepadPaths()
    {
        var result = new List<string>();
        Guid guid = GetHidGuid();
        IntPtr hSet = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (hSet == IntPtr.Zero || hSet == (IntPtr)(-1)) return result;
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
                    if (!SetupDiGetDeviceInterfaceDetail(hSet, ref iface, ptr, needed, out _, IntPtr.Zero)) { idx++; continue; }
                    char[] chars = new char[(int)needed / 2];
                    Marshal.Copy(ptr, chars, 0, chars.Length);
                    string full = new string(chars).TrimEnd('\0');
                    if (full.IndexOf("vid_0079", StringComparison.OrdinalIgnoreCase) >= 0
                        && full.IndexOf("pid_0006", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string path = new string(chars, 2, Math.Max(0, chars.Length - 2)).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(path) && path.StartsWith("\\")) result.Add(path);
                    }
                }
                finally { Marshal.FreeHGlobal(ptr); }
                idx++;
            }
        }
        finally { SetupDiDestroyDeviceInfoList(hSet); }
        return result;
    }

    void PipeServer(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, MaxPads, PipeTransmissionMode.Message, PipeOptions.None, 4096, 4096);
                pipe.WaitForConnection();
                byte[] buf = new byte[512];
                while (!token.IsCancellationRequested && pipe.IsConnected)
                {
                    int read = pipe.Read(buf, 0, buf.Length);
                    if (read == 0) break;
                    string cmd = Encoding.UTF8.GetString(buf, 0, read).Trim().ToLowerInvariant();
                    ProcessCommand(cmd);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Console.WriteLine($"[PIPE] ERROR: {ex.Message} [TAG13]");
                Thread.Sleep(500);
            }
        }
    }

    void ProcessCommand(string cmd)
    {
        string[] tokens = cmd.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        int padIdx = -1;
        string rest = cmd;
        if (tokens.Length >= 3 && byte.TryParse(tokens[0], out byte idxByte) && idxByte < (byte)MaxPads)
        {
            padIdx = idxByte;
            rest = string.Join(" ", tokens.Skip(1));
        }
        switch (rest)
        {
            case "stop": case "off": case "0 0": case "0":
                if (padIdx >= 0) StopPad(padIdx); else StopHidRumbleAll(); break;
            case "start": case "on": case "1 1": case "1":
                if (padIdx >= 0) SendHidRumbleToPad(padIdx, 255, 255); else SendHidRumbleAll(255, 255); break;
            default:
            {
                string[] parts = rest.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && byte.TryParse(parts[0], out byte s) && byte.TryParse(parts[1], out byte b))
                {
                    byte val = (s > 0 || b > 0) ? (byte)255 : (byte)0;
                    if (padIdx >= 0) SendHidRumbleToPad(padIdx, val, val);
                    else SendHidRumbleAll(val, val);
                }
                break;
            }
        }
    }

    void SendHidRumbleAll(byte small, byte big) { lock (_pads) for (int i = 0; i < _pads.Count; i++) SendHidRumbleToPad(i, small, big); }
    void StopHidRumbleAll() { lock (_pads) for (int i = 0; i < _pads.Count; i++) { var pad = _pads[i]; if (!pad.Connected || pad.HidHandle == IntPtr.Zero || pad.HidHandle == (IntPtr)(-1)) continue; HidD_SetOutputReport(pad.HidHandle, CmdStop, CmdStop.Length); } }
    int CountConnected() { lock (_pads) return _pads.Count(p => p.Connected); }

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    const uint MOD_CONTROL = 0x0002; const uint MOD_SHIFT = 0x0004; const uint VK_R = 0x52;

    protected override void WndProc(ref Message m)
    {
        const int WM_HOTKEY = 0x0312;
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _hotkeyId) ToggleRumble();
        base.WndProc(ref m);
    }

    void ToggleRumble()
    {
        _rumbleOn = !_rumbleOn;
        Console.WriteLine($"[DBG] Rumble toggled -> {_rumbleOn} [TAG17]");
        if (_rumbleOn) SendHidRumbleAll(255, 255); else StopHidRumbleAll();
        SetStatus(_rumbleOn ? "Вибрация ВКЛ" : "Вибрация ВЫКЛ");
    }

    void InitializeTray()
    {
        _tray = new NotifyIcon { Text = "Gamepad Bridge", Icon = SystemIcons.Application, Visible = true };
        _menu = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("Toggle Vibration (Ctrl+Shift+R)");
        toggleItem.Click += (s, e) => ToggleRumble();
        _menu.Items.Add(toggleItem);
        var statusItem = new ToolStripMenuItem("Status: Loading...") { Name = "statusItem" };
        _menu.Items.Add(statusItem);
        _menu.Items.Add(new ToolStripSeparator());
        var settingsItem = new ToolStripMenuItem("Pad Settings...");
        settingsItem.Click += (s, e) => ShowPadSettings();
        _menu.Items.Add(settingsItem);
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApp();
        _menu.Items.Add(exitItem);
        _tray.ContextMenuStrip = _menu;
        _tray.DoubleClick += (s, e) => ToggleRumble();
    }

    void ShowPadSettings()
    {
        try
        {
            var pads = new List<PadConnectionInfo>();
            lock (_pads)
            {
                for (int i = 0; i < MaxPads; i++)
                {
                    var entry = _pads[i];
                    pads.Add(new PadConnectionInfo
                    {
                        Index = i,
                        Connected = entry.Connected,
                        RumbleScale = entry.RumbleScale
                    });
                }
            }
            var form = new PadSettingsForm(pads, (idx, scale) =>
            {
                lock (_pads)
                {
                    if (idx >= 0 && idx < _pads.Count)
                        _pads[idx] = new GamepadEntry
                        {
                            Controller = _pads[idx].Controller,
                            HidHandle = _pads[idx].HidHandle,
                            DevicePath = _pads[idx].DevicePath,
                            Connected = _pads[idx].Connected,
                            RumbleScale = scale
                        };
                }
                SetStatus($"Геймпад #{idx + 1} масштаб вибро: {scale}%");
            });
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UI] ERROR ShowPadSettings: {ex.Message}");
        }
    }

    void SetStatus(string text)
    {
        InvokeIfRequired(() =>
        {
            if (_tray != null) _tray.Text = $"Gamepad Bridge [({(CountConnected() > 0 ? "OK" : "---")})] x{CountConnected()} {text}";
            var item = _menu?.Items.Find("statusItem", false)[0] as ToolStripMenuItem;
            if (item != null) item.Text = $"Status: x{CountConnected()} - {text}";
        });
    }

    void ExitApp() { Cleanup(); Application.Exit(); }
    
    void Cleanup()
    {
        Console.WriteLine("[DBG] Cleanup started [TAG14]");
        UnregisterHotKey(this.Handle, _hotkeyId);
        StopHidRumbleAll();
        lock (_pads)
        {
            foreach (var pad in _pads)
            {
                if (pad.Controller != null)
                {
                    try { pad.Controller.Disconnect(); } catch { }
                    pad.Controller.FeedbackReceived -= OnXboxFeedbackReceived;
                }
                if (pad.HidHandle != IntPtr.Zero && pad.HidHandle != (IntPtr)(-1)) CloseHandle(pad.HidHandle);
            }
            _pads.Clear();
        }
        _vigemClient?.Dispose();
        _vigemClient = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        _tray?.Dispose();
        _menu?.Dispose();
        Console.WriteLine("[DBG] Cleanup completed [TAG15]");
    }
    
    static void InvokeIfRequired(Action action)
    {
        if (action == null) return;
        try
        {
            var ctx = SynchronizationContext.Current;
            if (ctx != null) ctx.Post(_ => { try { action(); } catch { } }, null);
            else action();
        }
        catch { }
    }

    const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, OPEN_EXISTING = 3, DIGCF_PRESENT = 2, DIGCF_DEVICEINTERFACE = 0x10;
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateFile(string name, uint acc, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
    [DllImport("hid.dll")] static extern bool HidD_SetOutputReport(IntPtr h, byte[] buf, int len);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern IntPtr SetupDiGetClassDevs(ref Guid c, IntPtr e, IntPtr p, uint f);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, uint i, out SP_IFACE iface);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_IFACE iface, IntPtr d, uint sz, out uint needed, IntPtr di);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
    [StructLayout(LayoutKind.Sequential)] struct SP_IFACE { public uint cbSize; public Guid ClassGuid; public uint Flags; public IntPtr Reserved; }
    static IntPtr OpenDevice(string path) => CreateFile(path, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
    static Guid GetHidGuid() { Guid g; HidD_GetHidGuid(out g); return g; }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
