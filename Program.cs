// Minimal reproduction of dotnet/runtime JIT bug on macOS arm64 with .NET 10
// The bug is in postorder local assertion propagation for fieldwise block ops
//
// Run: dotnet run -c Release
// Fix: DOTNET_JitEnablePostorderLocalAssertionProp=0 dotnet run -c Release

using System.Runtime.CompilerServices;

// 12-byte readonly struct with copy constructor (field-by-field copy)
public readonly struct Endpoint
{
    public Endpoint(Endpoint e)
    {
        A = e.A;
        B = e.B;
        C = e.C;
    }

    public Endpoint(int a, int b, int c) { A = a; B = b; C = c; }

    public int A { get; }
    public int B { get; }
    public int C { get; }
}

// Class with AggressiveOptimization constructor - triggers the bug
public sealed class Range
{
    private readonly Endpoint m_start;
    private readonly Endpoint m_end;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public Range(Endpoint start, Endpoint end)
    {
        m_start = new Endpoint(start);  // Copy via copy constructor
        m_end = new Endpoint(end);      // Copy via copy constructor
    }

    public int Length => m_end.A - m_start.A;
}

public class Program
{
    public static int Main()
    {
        Console.WriteLine($".NET {Environment.Version}, {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        
        const int expected = 5759;
        
        for (int i = 0; i < 10000; i++)
        {
            var r = new Range(new Endpoint(100, 0, 0), new Endpoint(100 + expected, 0, 0));
            
            if (r.Length != expected)
            {
                Console.WriteLine($"FAIL at iteration {i}: Length={r.Length} (expected {expected}), hex=0x{r.Length:X}");
                Console.WriteLine("Workaround: DOTNET_JitEnablePostorderLocalAssertionProp=0");
                return 1;
            }
        }
        
        Console.WriteLine($"PASS: 10000 iterations, Length always = {expected}");
        return 0;
    }
}
