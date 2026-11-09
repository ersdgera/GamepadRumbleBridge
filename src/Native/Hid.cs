using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace GamepadBridge.Native;

/// <summary>
/// P/Invoke-обёртки над HID-стопкой Windows (hid.dll / setupapi / cfgmgr32).
/// </summary>
public static class Hid
{
    // GUID_DEVINTERFACE_HID {4D1E55B2-F160-11CF-883B-444553540000}
    public static readonly Guid HidInterfaceGuid = new("4D1E55B2-F160-11CF-883B-444553540000");
    public static readonly Guid HidClassGuid = new("745A17A0-74D3-11D0-B6FE-00A0C90F57DA");

    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint OPEN_EXISTING = 3;
    public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

    public const int HIDP_STATUS_SUCCESS = unchecked((int)0x00110000);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool HidD_GetAttributes(SafeFileHandle handle, out HIDD_ATTRIBUTES attributes);

    [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    /// <summary>Логическое значение usage'а из входного/выходного отчёта.</summary>
    [DllImport("hid.dll", SetLastError = true)]
    public static extern int HidP_GetUsageValue(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        out uint usageValue,
        byte[] reportBuffer,
        ushort reportBufferLength,
        IntPtr preparsedData);

    /// <summary>Максимальная длина списка нажатых usage'ов (для button-массивов).</summary>
    [DllImport("hid.dll", SetLastError = true)]
    public static extern uint HidP_MaxUsageListLength(
        int reportType, ushort usagePage, IntPtr preparsedData);

    /// <summary>Список «истинных» usage'ов в отчёте (кнопки 0x09 и т.п.).</summary>
    [DllImport("hid.dll", SetLastError = true)]
    public static extern int HidP_GetUsages(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        uint[] usageList,
        ref uint usageListLength,
        int valueFlagsReserved,
        byte[] reportBuffer,
        ushort reportBufferLength,
        IntPtr preparsedData);

    [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool HidD_GetProductString(SafeFileHandle handle, IntPtr buffer, int bufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool HidD_SetOutputReport(SafeFileHandle handle, byte[] reportBuffer, int reportBufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool HidD_SetFeature(SafeFileHandle handle, byte[] reportBuffer, int reportBufferLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReadFile(
        SafeFileHandle hFile,
        byte[] buffer,
        uint numberOfBytesToRead,
        out uint numberOfBytesRead,
        IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    // ---- SetupAPI ----

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid,
        uint memberIndex, out SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    public const uint DIGCF_PRESENT = 0x00000002;
    public const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDD_ATTRIBUTES
    {
        public int Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    /// <summary>HIDP_VALUE_CAPS — описание оси/значения в отчёте.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_VALUE_CAPS
    {
        public ushort ReportType;
        public ushort HasNull;
        public byte Reserved;
        public ushort ReportSize;
        public ushort NotUsed;
        public uint ValueCaption;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public ushort[] LinkCollection;
        public ushort UsagePage;
        public ushort Usage;
        public ushort UsageMin;
        public ushort UsageMax;
        public ushort StringMin;
        public ushort StringMax;
        public ushort Minimum;
        public ushort Maximum;
        public ushort PhysicalMin;
        public ushort PhysicalMax;
    }

    /// <summary>HIDP_BUTTON_CAPS — описание кнопки/битового поля.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_BUTTON_CAPS
    {
        public ushort ReportType;
        public ushort IsRange;
        public ushort ReportSize;
        public ushort NotUsed;
        public uint ValueCaption;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public ushort[] LinkCollection;
        public ushort UsagePage;
        public ushort Usage;
        public ushort UsageMin;
        public ushort UsageMax;
        public ushort StringMin;
        public ushort StringMax;
        public ushort DesignatorMin;
        public ushort DesignatorMax;
        public ushort DataIndexMin;
        public ushort DataIndexMax;
    }

    public enum ReportType
    {
        Input = 1,
        Output = 2,
        Feature = 3,
    }
}
