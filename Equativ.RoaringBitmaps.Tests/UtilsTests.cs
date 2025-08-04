using Xunit;

namespace Equativ.RoaringBitmaps.Tests;

public class UtilsTests
{
    [Fact]
    public void IntersectArrays_TwoSets()
    {
        ushort[] set1 = {1, 2, 3};
        ushort[] set2 = {2, 3, 4};
        ushort[] buffer = new ushort[3];

        int count = Utils.IntersectArrays(buffer, set1, set2);

        Assert.Equal(2, count);
        Assert.Equal((ushort)2, buffer[0]);
        Assert.Equal((ushort)3, buffer[1]);
    }

    [Fact]
    public void IntersectArrays_MultipleSets()
    {
        ushort[] set1 = {1, 2, 3, 4, 5};
        ushort[] set2 = {2, 4, 6};
        ushort[] set3 = {0, 2, 4, 8};
        ushort[] buffer = new ushort[5];

        int count = Utils.IntersectArrays(buffer, set1, set2, set3);

        Assert.Equal(2, count);
        Assert.Equal((ushort)2, buffer[0]);
        Assert.Equal((ushort)4, buffer[1]);
    }
}
