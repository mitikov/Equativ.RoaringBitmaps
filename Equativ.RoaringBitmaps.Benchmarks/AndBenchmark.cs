using BenchmarkDotNet.Attributes;
using Equativ.RoaringBitmaps.Datasets;

namespace Equativ.RoaringBitmaps.Benchmarks;

[MemoryDiagnoser(false)]
public class AndBenchmark
{
    private RoaringBitmap _bitmap1 = null!;
    private RoaringBitmap _bitmap2 = null!;
    private RoaringBitmap _bitmap3 = null!;

    [Params(
        Paths.Census1881,
        Paths.Census1881Srt,
        Paths.CensusIncome,
        Paths.Dimension003,
        Paths.Dimension008,
        Paths.Dimension033,
        Paths.UsCensus2000,
        Paths.WeatherSept85,
        Paths.WeatherSept85Srt,
        Paths.WikileaksNoQuotes,
        Paths.WikileaksNoQuotesSrt)]
    public string FileName { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        using var provider = new ZipRealDataProvider(FileName);
        var bitmaps = provider.ToArray();
        _bitmap1 = bitmaps[0];
        _bitmap2 = bitmaps[1];
        _bitmap3 = bitmaps[2];
    }

    [Benchmark]
    public long And()
    {
        return RoaringBitmap.And(_bitmap1, _bitmap2, _bitmap3).Cardinality;
    }
}
