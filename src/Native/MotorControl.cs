using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace GamepadBridge.Native;

/// <summary>Управление вибрацией физического геймпада VID_0079&amp;PID_0006.</summary>
public sealed class MotorControl
{
    private static readonly byte[] StopCmd = { 0x00, 0xf3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

    /// <summary>
    /// Отправляет 2 отчёта вибрации (протокол геймпадов VID 0x0079 PID 0x0006).
    /// </summary>
    public static bool SendRumble(SafeFileHandle handle, byte smallMotor, byte bigMotor)
    {
        if (handle == null || handle.IsInvalid) return false;

        if (smallMotor == 0 && bigMotor == 0)
        {
            return Hid.HidD_SetOutputReport(handle, StopCmd, StopCmd.Length);
        }

        byte[] buf1 = { 0x00, 0x51, 0x00, smallMotor, 0x00, bigMotor, 0x00, 0x00 };
        byte[] buf2 = { 0x00, 0xfa, 0xfe, 0x00, 0x00, 0x00, 0x00, 0x00 };

        bool ok1 = Hid.HidD_SetOutputReport(handle, buf1, buf1.Length);
        bool ok2 = Hid.HidD_SetOutputReport(handle, buf2, buf2.Length);
        return ok1 && ok2;
    }

    public static bool Stop(SafeFileHandle handle)
    {
        if (handle == null || handle.IsInvalid) return false;
        return Hid.HidD_SetOutputReport(handle, StopCmd, StopCmd.Length);
    }
}
