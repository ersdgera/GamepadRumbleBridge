using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

class T
{
    [DllImport("xinput1_4.dll")]
    static extern uint XInputGetState(int i, out S s);
    [DllImport("xinput1_4.dll")]
    static extern uint XInputSetState(int i, ref V v);
    [StructLayout(LayoutKind.Sequential)]
    struct S { public uint pkt; public G g; }
    [StructLayout(LayoutKind.Sequential)]
    struct G { public ushort b; public byte l,r,x,y,x2,y2; }
    [StructLayout(LayoutKind.Sequential)]
    struct V { public ushort left; public ushort right; }
    
    static void Main()
    {
        Console.WriteLine("Waiting 5s...");
        Thread.Sleep(5000);
        
        var st = new S();
        Console.WriteLine("\n--- State before ---");
        for (int i = 0; i < 4; i++)
        {
            uint r = XInputGetState(i, out st);
            Console.WriteLine($"X[{i}]: code={r} pkt={st.pkt}");
        }
        
        Console.WriteLine("\n--- Pad 0 rumble (left motor) ---");
        var vib = new V { left = 65535 };
        XInputSetState(0, ref vib);
        Thread.Sleep(1000);
        vib.left = 0;
        XInputSetState(0, ref vib);
        Thread.Sleep(300);
        
        Console.WriteLine("\n--- Pad 1 rumble (right motor) ---");
        vib.right = 65535;
        XInputSetState(1, ref vib);
        Thread.Sleep(1000);
        vib.right = 0;
        XInputSetState(1, ref vib);
        Thread.Sleep(300);
        
        Console.WriteLine("\n--- Final state ---");
        for (int i = 0; i < 4; i++)
        {
            uint r = XInputGetState(i, out st);
            Console.WriteLine($"X[i]: code={r} pkt={st.pkt}");
        }
        
        // Check bridge logs
        try
        {
            var logPath = @"D:\MAN\bridge_rumble_test.txt";
            if (File.Exists(logPath))
            {
                var lines = File.ReadAllLines(logPath);
                var feedback = Array.FindAll(lines, l => l.Contains("[DBG]") && l.Contains("Feedback"));
                if (feedback.Length > 0)
                {
                    Console.WriteLine("\n=== Bridge feedback events ===");
                    foreach (var l in feedback) Console.WriteLine(l);
                }
                else
                {
                    Console.WriteLine("\nNo non-zero feedback events in bridge logs");
                }
            }
            else
            {
                Console.WriteLine($"\nLog file not found: {logPath}");
            }
        }
        catch { Console.WriteLine("Cannot read bridge logs"); }
        
        Console.WriteLine("\nDone!");
    }
}
