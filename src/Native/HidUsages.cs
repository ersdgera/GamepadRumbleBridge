namespace GamepadBridge.Native;

/// <summary>Type/usage-пары HID (UsagePage:Usage).</summary>
public struct HidUsage
{
    public ushort Page;
    public ushort Usage;
    public HidUsage(ushort p, ushort u) { Page = p; Usage = u; }
    public override string ToString() => $"{Page:X2}:{Usage:X4}";
}

/// <summary>Используемые страницы из HID Usage Tables.</summary>
public static class UsagePages
{
    public const ushort GenericDesktop = 0x01;
    public const ushort Simulation = 0x02;
    public const ushort Button = 0x09;
    public const ushort Consumer = 0x0C;
}

/// <summary>Generic Desktop usages.</summary>
public static class UsageDesk
{
    public const ushort Pointer = 0x01;
    public const ushort Mouse = 0x02;
    public const ushort Joystick = 0x04;
    public const ushort Gamepad = 0x05;
    public const ushort X = 0x30;
    public const ushort Y = 0x31;
    public const ushort Z = 0x32;
    public const ushort Rx = 0x33;
    public const ushort Ry = 0x34;
    public const ushort Rz = 0x35;
    public const ushort Slider = 0x36;
    public const ushort Dial = 0x37;
    public const ushort Hat = 0x39;
}

/// <summary>Запись value-cap (ось/значений).</summary>
public struct ValueCapInfo
{
    public ushort Link;
    public ushort Page;
    public ushort Usage;
    public ushort UsageMin;
    public ushort UsageMax;
    public int LogMin;
    public int LogMax;
    public int PhysMin;
    public int PhysMax;
    public byte ReportSize;
    public bool IsArray;
}

/// <summary>Запись button-cap (битовые кнопки).</summary>
public struct ButtonCapInfo
{
    public ushort Link;
    public ushort Page;
    public ushort UsageMin;
    public ushort UsageMax;
    public byte ReportSize;
}
