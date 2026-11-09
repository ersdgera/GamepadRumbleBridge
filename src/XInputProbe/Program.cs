using System;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("xinput1_4.dll")]
    static extern uint XInputGetState(int uUserIndex, out XINPUT_STATE lpState);
    [DllImport("xinput1_4.dll")]
    static extern uint XInputSetState(int uUserIndex, ref XINPUT_VIBRATION pVibration);

    [StructLayout(LayoutKind.Sequential)]
    struct XINPUT_GAMEPAD { public ushort wButtons; public byte bLeftTrigger; public byte bRightTrigger; public short sThumbLX; public short sThumbLY; public short sThumbRX; public short sThumbRY; }
    [StructLayout(LayoutKind.Sequential)]
    struct XINPUT_STATE { public uint dwPacketNumber; public XINPUT_GAMEPAD Gamepad; }
    [StructLayout(LayoutKind.Sequential)]
    struct XINPUT_VIBRATION { public ushort wLeftMotorSpeed; public ushort wRightMotorSpeed; }

    static void Main()
    {
        Console.WriteLine("=== Rumble Routing Test ===");
        
        // Count visible controllers
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            uint r = XInputGetState(i, out var _);
            if (r == 0) count++;
        }
        Console.WriteLine($"Visible XInput controllers: {count}");

        // Test: send rumble to each controller individually and report
        for (int i = 0; i < 4; i++)
        {
            uint r = XInputGetState(i, out var state);
            if (r != 0) continue;
            
            Console.WriteLine($"\n[Test {i}] Sending rumble to controller #{i}...");
            XINPUT_VIBRATION vib = new XINPUT_VIBRATION { wLeftMotorSpeed = 65535, wRightMotorSpeed = 65535 };
            XInputSetState(i, ref vib);
            Console.WriteLine($"  Sent max rumble to XInput [{i}]");
            Console.WriteLine($"  Check: does ONLY this gamepad vibrate? (if yes -> per-controller works!)");
        }

        Console.WriteLine("\nNow test the bridge pipe:");
        Console.WriteLine("  '0 255 255' should vibrate only slot 0");
        Console.WriteLine("  '1 255 255' should vibrate only slot 1");
    }
}
