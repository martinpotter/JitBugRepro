# .NET 10 JIT Bug Reproduction

Standalone reproduction of a JIT bug in .NET 10.0.201+ on macOS arm64.

## Bug Summary

The JIT's "postorder local assertion propagation for fieldwise block op indirs" 
optimization incorrectly handles field-by-field struct copies in readonly structs,
causing memory corruption.

**Affected:** .NET 10.0.201+ on macOS arm64
**Commit:** `bbe64d057704e103de3e0db1fbc93fbec0769a8e`

## Symptoms

- A 12-byte readonly struct with a copy constructor gets incorrectly optimized
- Field values read from the copied struct return garbage data
- Expected: `len=5759`
- Actual: `len=1839834740` (or similar garbage value)

## How to Reproduce

```bash
# Run Release build (bug manifests)
dotnet run -c Release

# Expected output:
# FAIL on round 1, iteration 1!
#   Expected: res/LLS:1.0.3/2006-05-10T18:34:41Z/159548?len=5759
#   Actual:   res/LLS:1.0.3/2006-05-10T18:34:41Z/159548?len=1839834740
```

## Workaround

```bash
# Disable the problematic optimization
DOTNET_JitEnablePostorderLocalAssertionProp=0 dotnet run -c Release
```

## Key Code Pattern

The bug requires:
1. A 12-byte `readonly struct` with 3 int fields
2. A copy constructor that copies field-by-field
3. `[MethodImpl(MethodImplOptions.AggressiveOptimization)]` on the constructors
4. The struct stored in a class with inheritance hierarchy

```csharp
public readonly struct Endpoint
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public Endpoint(Endpoint endpoint)
    {
        TotalCharacterOffset = endpoint.TotalCharacterOffset;
        Article = endpoint.Article;
        Offset = endpoint.Offset;
    }

    public int TotalCharacterOffset { get; }
    public int Article { get; }
    public int Offset { get; }
}
```

## Environment

- .NET SDK: 10.0.201
- Runtime: 10.0.5
- OS: macOS 15.x (arm64)
- Architecture: Apple Silicon (M1/M2/M3)
