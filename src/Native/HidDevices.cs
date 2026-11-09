using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace GamepadBridge.Native;

/// <summary>Перечисление HID-устройств и открытие их по интерфейсу.</summary>
public static class HidDevices
{
    /// <summary>
    /// SP_DEVICE_INTERFACE_DETAIL_DATA_W в raw-буфере:
    ///   uint  dwSize;      // 4
    ///   wchar_t DevicePath[];
    /// На x64 sizeof(struct) == 8 (4 байта cbSize + padding до 1 wchar),
    /// на x86 — 6. Дальше путь от offset 8 на x64 / 8 на x86 (из-за выравнивания wchar_t).
    /// </summary>
    private static readonly int DetailCbsz = IntPtr.Size == 8 ? 8 : 6;
    private static readonly int DetailPathOffset = 8; // путь всегда по align-границе 2, но struct-align = 4 => 8 на x64; на x86: 6+2=8 тоже

    /// <summary>Возвращает все пути HID-интерфейсов (present-only).</summary>
    public static List<string> EnumeratePaths()
    {
        var result = new List<string>();
        Guid guid;
        Hid.HidD_GetHidGuid(out guid);

        IntPtr set = Hid.SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero,
            Hid.DIGCF_PRESENT | Hid.DIGCF_DEVICEINTERFACE);
        if (set == IntPtr.Zero || set == new IntPtr(-1))
            return result;

        try
        {
            var iface = new Hid.SP_DEVICE_INTERFACE_DATA
            {
                cbSize = (uint)Marshal.SizeOf<Hid.SP_DEVICE_INTERFACE_DATA>()
            };

            for (uint i = 0; ; i++)
            {
                if (!Hid.SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, out iface))
                    break;

                int size = DetailCbsz + 260 * 2;
                IntPtr buf = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.WriteInt32(buf, DetailCbsz);
                    if (Hid.SetupDiGetDeviceInterfaceDetail(set, ref iface, buf, (uint)size, out _, IntPtr.Zero))
                    {
                        string path = Marshal.PtrToStringUni(buf + DetailPathOffset);
                        if (!string.IsNullOrEmpty(path))
                            result.Add(path);
                    }
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
        }
        finally { Hid.SetupDiDestroyDeviceInfoList(set); }

        return result;
    }

    /// <summary>Открывает устройство (чтение+запись). null — если не удалось.</summary>
    public static SafeFileHandle TryOpen(string path, bool writable = true)
    {
        uint access = Hid.GENERIC_READ | (writable ? Hid.GENERIC_WRITE : 0);
        var h = Hid.CreateFile(path, access, Hid.FILE_SHARE_READ | Hid.FILE_SHARE_WRITE,
            IntPtr.Zero, Hid.OPEN_EXISTING, 0, IntPtr.Zero);
        if (h.IsInvalid) { h.Dispose(); return null; }
        return h;
    }

    /// <summary>VID/PID из пути экземпляра HID: HID\VID_xxxx&amp;PID_yyyy...</summary>
    public static bool TryParseVidPid(string hidPath, out ushort vid, out ushort pid)
    {
        vid = pid = 0;
        string upper = hidPath.ToUpperInvariant();
        int v = upper.IndexOf("VID_", StringComparison.Ordinal);
        int p = upper.IndexOf("PID_", StringComparison.Ordinal);
        if (v < 0 || p < 0) return false;
        try
        {
            vid = Convert.ToUInt16(upper.Substring(v + 4, 4), 16);
            pid = Convert.ToUInt16(upper.Substring(p + 4, 4), 16);
            return true;
        }
        catch { return false; }
    }
}
