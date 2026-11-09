using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace GamepadBridge.Native;

/// <summary>
/// Надёжный декодер для Generic USB Gamepad (VID_0079&amp;PID_0006).
/// </summary>
public sealed class HidGamepadDecoder : IDisposable
{
    public Hid.HIDP_CAPS Caps;
    public string Path;
    public ushort Vid, Pid;

    IntPtr _preparsed;
    bool _disposed;

    public HidGamepadDecoder(SafeFileHandle handle, string path)
    {
        Path = path;
        if (!Hid.HidD_GetAttributes(handle, out var attr))
            throw new InvalidOperationException("HidD_GetAttributes failed");
        Vid = attr.VendorID; Pid = attr.ProductID;

        if (!Hid.HidD_GetPreparsedData(handle, out _preparsed))
            throw new InvalidOperationException("HidD_GetPreparsedData failed");

        Hid.HidP_GetCaps(_preparsed, out Caps);
    }

    public Dictionary<ushort, int> ReadAxes(byte[] report)
    {
        var axes = new Dictionary<ushort, int>();
        if (report.Length < 5) return axes;

        int offset = report[0] == 0 && report.Length >= 6 ? 1 : 0;

        axes[UsageDesk.X] = report[offset + 0];
        axes[UsageDesk.Y] = report[offset + 1];
        axes[UsageDesk.Z] = report[offset + 2];
        axes[UsageDesk.Rz] = report[offset + 3];

        // System fallback
        try
        {
            uint val;
            if (Hid.HidP_GetUsageValue((int)Hid.ReportType.Input, UsagePages.GenericDesktop, 0, UsageDesk.X, out val, report, (ushort)report.Length, _preparsed) == Hid.HIDP_STATUS_SUCCESS)
                axes[UsageDesk.X] = (int)val;
            if (Hid.HidP_GetUsageValue((int)Hid.ReportType.Input, UsagePages.GenericDesktop, 0, UsageDesk.Y, out val, report, (ushort)report.Length, _preparsed) == Hid.HIDP_STATUS_SUCCESS)
                axes[UsageDesk.Y] = (int)val;
        }
        catch { }

        return axes;
    }

    public int ReadHat(byte[] report)
    {
        if (report.Length < 6) return -1;
        int offset = report[0] == 0 && report.Length >= 6 ? 1 : 0;
        byte b = report[offset + 4];
        int hat = b & 0x0F;
        return hat >= 0 && hat <= 7 ? hat + 1 : -1;
    }

    public HashSet<ushort> ReadButtons(byte[] report)
    {
        var pressed = new HashSet<ushort>();
        if (report.Length < 6) return pressed;
        int offset = report[0] == 0 && report.Length >= 6 ? 1 : 0;

        byte b5 = report[offset + 4];
        byte b6 = report[offset + 5];

        for (int i = 0; i < 8; i++)
        {
            if ((b5 & (1 << i)) != 0)
                pressed.Add((ushort)(i + 1));
        }

        for (int i = 0; i < 4; i++)
        {
            if ((b6 & (1 << i)) != 0)
                pressed.Add((ushort)(i + 9));
        }

        return pressed;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_preparsed != IntPtr.Zero)
        {
            Hid.HidD_FreePreparsedData(_preparsed);
            _preparsed = IntPtr.Zero;
        }
    }
}
