# Test: check how many Xbox 360 controllers ViGEm created
Write-Host "=== ViGEm Multi-Pad Test ==="
Write-Host ""

Get-WmiObject Win32_PnPEntity | Where-Object { $_.Name -match "Xbox" } | ForEach-Object {
    Write-Host ("Device: " + $_.Name)
    Write-Host ("  Status : " + $_.Status)
    Write-Host ("  ID     : " + $_.PNPDeviceID)
    Write-Host ""
}

# Also check what XInput reports
$code = @'
using System;
using System.Runtime.InteropServices;
class P {
    [DllImport("xinput1_4.dll")] static extern uint XInputGetState(int uUserIndex, out GAMEPAD lpState);
    [StructLayout(LayoutKind.Sequential)] struct GAMEPAD {
        public uint dwPacketNumber;
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }
    static void Main() {
        int count = 0;
        for (int i = 0; i < 4; i++) {
            var r = XInputGetState(i, out var g);
            string s = r == 0 ? "CONNECTED" : "NOT FOUND";
            Console.WriteLine("Controller " + i + ": " + s);
            if (r == 0) count++;
        }
        Console.WriteLine("Total XInput controllers: " + count);
    }
}
'@

Add-Type -TypeDefinition $code -Language CSharpVersion4 -ReferencedAssemblies System.Runtime.InteropServices
[P]::Main()
