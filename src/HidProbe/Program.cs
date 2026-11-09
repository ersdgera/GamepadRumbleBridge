using System.Runtime.InteropServices;
using GamepadBridge.Native;

// HID Probe: снимает VID/PID, usage, размеры отчётов и печатает сырые входные отчёты
const ushort TargetVid = 0x0079;
const ushort TargetPid = 0x0006;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var paths = HidDevices.EnumeratePaths();
var matches = new List<(string path, Hid.HIDD_ATTRIBUTES attr)>();

foreach (var p in paths)
{
    using var h = HidDevices.TryOpen(p, writable: false);
    if (h == null) continue;
    if (!Hid.HidD_GetAttributes(h, out var attr)) continue;
    if (attr.VendorID == TargetVid && attr.ProductID == TargetPid)
        matches.Add((p, attr));
}

Console.WriteLine($"найдено устройств VID_0079&PID_0006: {matches.Count}");
if (matches.Count == 0)
{
    Console.WriteLine("Устройство не найдено.");
    return;
}

foreach (var (path, attr) in matches)
{
    Console.WriteLine();
    Console.WriteLine($"Device path: {path}");
    Console.WriteLine($"VID:PID = {attr.VendorID:X4}:{attr.ProductID:X4}  rev=0x{attr.VersionNumber:X4}");

    using var handle = HidDevices.TryOpen(path, writable: true);
    if (handle == null) { Console.WriteLine("Не удалось открыть (занято/нет доступа)."); continue; }

    if (!Hid.HidD_GetPreparsedData(handle, out var preparsed))
    {
        Console.WriteLine("HidD_GetPreparsedData FAILED " + Marshal.GetLastWin32Error());
        continue;
    }

    int rc = Hid.HidP_GetCaps(preparsed, out var caps);
    Console.WriteLine($"HidP_GetCaps rc=0x{rc:X8}  usage=0x{caps.Usage:X4}/page=0x{caps.UsagePage:X4}");
    Console.WriteLine($"  input={caps.InputReportByteLength} output={caps.OutputReportByteLength} feature={caps.FeatureReportByteLength}");
    Console.WriteLine($"  counts: inVal={caps.NumberInputValueCaps} inBtn={caps.NumberInputButtonCaps} " +
                      $"outVal={caps.NumberOutputValueCaps} outBtn={caps.NumberOutputButtonCaps} " +
                      $"featVal={caps.NumberFeatureValueCaps} featBtn={caps.NumberFeatureButtonCaps}");

    Console.WriteLine();
    Console.WriteLine("Жду нажатия кнопок / движения стиков... (Ctrl+C — выход)");
    var buf = new byte[Math.Max(1, (int)caps.InputReportByteLength)];
    using var dec = new HidGamepadDecoder(handle, path);
    while (true)
    {
        if (!Hid.ReadFile(handle, buf, (uint)buf.Length, out uint read, IntPtr.Zero) || read == 0)
        {
            Thread.Sleep(5);
            continue;
        }
        var sb = new System.Text.StringBuilder(buf.Length * 3);
        for (int i = 0; i < read; i++) sb.Append(buf[i].ToString("x2")).Append(' ');
        var axes = dec.ReadAxes(buf);
        var buttons = dec.ReadButtons(buf);
        int hat = dec.ReadHat(buf);
        string axisStr = axes.Count == 0 ? "-" : string.Join(",", axes.Select(kv => $"0x{kv.Key:X4}:{kv.Value}"));
        string btnStr = buttons.Count == 0 ? "-" : string.Join(",", buttons);
        Console.WriteLine($"{sb.ToString().TrimEnd()} | axes=[{axisStr}] btns=[{btnStr}] hat={hat}");
    }
}
