using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Equativ.RoaringBitmaps.Benchmark;

[MemoryDiagnoser(false)]
public class ArrayContainerVectorOpsBenchmark
{
    private ArrayContainer[] _containers = Array.Empty<ArrayContainer>();

    [Params(1000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(42);
        _containers = new ArrayContainer[Size];
        for (int i = 0; i < Size; i++)
        {
            var start = rnd.Next(0, ushort.MaxValue - 200);
            _containers[i] = ArrayContainer.Create(Enumerable.Range(start, 100).Select(x => (ushort)x).ToArray());
        }
    }

    private static ulong[][] FreshBitmaps(int size)
    {
        var bitmaps = new ulong[size][];
        for (int i = 0; i < size; i++)
        {
            bitmaps[i] = new ulong[1024];
        }
        return bitmaps;
    }

    [Benchmark]
    public int OrScalar()
    {
        var bitmaps = FreshBitmaps(Size);
        int total = 0;
        for (int i = 0; i < _containers.Length; i++)
        {
            total += _containers[i].OrArray(bitmaps[i]);
        }
        return total;
    }

    [Benchmark]
    public int OrVector()
    {
        var bitmaps = FreshBitmaps(Size);
        int total = 0;
        for (int i = 0; i < _containers.Length; i++)
        {
            total += _containers[i].OrArrayVectorized(bitmaps[i]);
        }
        return total;
    }

    [Benchmark]
    public int XorScalar()
    {
        var bitmaps = FreshBitmaps(Size);
        int total = 0;
        for (int i = 0; i < _containers.Length; i++)
        {
            total += _containers[i].XorArray(bitmaps[i]);
        }
        return total;
    }

    [Benchmark]
    public int XorVector()
    {
        var bitmaps = FreshBitmaps(Size);
        int total = 0;
        for (int i = 0; i < _containers.Length; i++)
        {
            total += _containers[i].XorArrayVectorized(bitmaps[i]);
        }
        return total;
    }

    [Benchmark]
    public int AndNotScalar()
    {
        var bitmaps = FreshBitmaps(Size);
        int total = 0;
        for (int i = 0; i < _containers.Length; i++)
        {
            total += _containers[i].AndNotArray(bitmaps[i]);
        }
        return total;
    }

    [Benchmark]
    public int AndNotVector()
    {
        var bitmaps = FreshBitmaps(Size);
        int total = 0;
        for (int i = 0; i < _containers.Length; i++)
        {
            total += _containers[i].AndNotArrayVectorized(bitmaps[i]);
        }
        return total;
    }
}
