﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Equativ.RoaringBitmaps;

internal class BitmapContainer : Container, IEquatable<BitmapContainer>
{
    private const int BitmapLength = 1024;
    public static readonly BitmapContainer One;
    private readonly ulong[] _bitmap;
    private readonly int _cardinality;

    static BitmapContainer()
    {
        var data = GC.AllocateUninitializedArray<ulong>(BitmapLength);
        for (var i = 0; i < BitmapLength; i++)
        {
            data[i] = ulong.MaxValue;
        }
        One = new BitmapContainer(1 << 16, data);
    }

    private BitmapContainer(int cardinality)
    {
        _bitmap = new ulong[BitmapLength];
        _cardinality = cardinality;
    }

    private BitmapContainer(int cardinality, ulong[] data)
    {
        _bitmap = data;
        _cardinality = cardinality;
    }

    private BitmapContainer(int cardinality, ushort[] values, bool negated) : this(negated ? MaxCapacity - cardinality : cardinality)
    {
        if (negated)
        {
            for (var i = 0; i < BitmapLength; i++)
            {
                _bitmap[i] = ulong.MaxValue;
            }
            for (var i = 0; i < cardinality; i++)
            {
                var v = values[i];
                _bitmap[v >> 6] &= ~(1UL << v);
            }
        }
        else
        {
            for (var i = 0; i < cardinality; i++)
            {
                var v = values[i];
                _bitmap[v >> 6] |= 1UL << v;
            }
        }
    }

    protected internal override int Cardinality => _cardinality;

    public override int ArraySizeInBytes => MaxCapacity / 8;

    public bool Equals(BitmapContainer? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (_cardinality != other._cardinality)
        {
            return false;
        }
        for (var i = 0; i < BitmapLength; i++)
        {
            if (_bitmap[i] != other._bitmap[i])
            {
                return false;
            }
        }
        return true;
    }

    internal static BitmapContainer Create(ushort[] values)
    {
        return new BitmapContainer(values.Length, values, false);
    }

    internal static BitmapContainer Create(int cardinality, ushort[] values)
    {
        return new BitmapContainer(cardinality, values, false);
    }

    internal static BitmapContainer Create(int cardinality, ushort[] values, bool negated)
    {
        return new BitmapContainer(cardinality, values, negated);
    }
    internal static BitmapContainer CreateXor(ushort[] first, int firstCardinality, ushort[] second, int secondCardinality)
    {
        var data = new ulong[BitmapLength];
        for (var i = 0; i < firstCardinality; i++)
        {
            var v = first[i];
            data[v >> 6] ^= 1UL << v;
        }

        for (var i = 0; i < secondCardinality; i++)
        {
            var v = second[i];
            data[v >> 6] ^= 1UL << v;
        }
        var cardinality = Utils.Popcnt(data);
        return new BitmapContainer(cardinality, data);
    }

    /// <summary>
    /// Java version has an optimized version of this, but it's using bitcount internally which should make it slower in .NET
    /// </summary>
    public static Container operator &(BitmapContainer x, BitmapContainer y)
    {
        var data = Clone(x._bitmap);
        var bc = new BitmapContainer(AndInternal(data, y._bitmap), data);
        return bc._cardinality <= MaxSize ? ArrayContainer.Create(bc) : bc;
    }

    private static ulong[] Clone(ulong[] data)
    {
        var result = GC.AllocateUninitializedArray<ulong>(BitmapLength);
        Buffer.BlockCopy(data, 0, result, 0, BitmapLength * sizeof(ulong));
        return result;
    }

    public static ArrayContainer operator &(BitmapContainer x, ArrayContainer y)
    {
        return y & x;
    }

    public static BitmapContainer operator |(BitmapContainer x, BitmapContainer y)
    {
        var data = Clone(x._bitmap);
        return new BitmapContainer(OrInternal(data, y._bitmap), data);
    }

    public static BitmapContainer operator |(BitmapContainer x, ArrayContainer y)
    {
        var data = Clone(x._bitmap);
        return new BitmapContainer(x._cardinality + y.OrArray(data), data);
    }

    public static Container operator ~(BitmapContainer x)
    {
        var data = Clone(x._bitmap);
        var bc = new BitmapContainer(NotInternal(data), data);
        return bc._cardinality <= MaxSize ? ArrayContainer.Create(bc) : bc;
    }

    /// <summary>
    ///     Java version has an optimized version of this, but it's using bitcount internally which should make it slower in
    ///     .NET
    /// </summary>
    public static Container operator ^(BitmapContainer x, BitmapContainer y)
    {
        var data = Clone(x._bitmap);
        var bc = new BitmapContainer(XorInternal(data, y._bitmap), data);
        return bc._cardinality <= MaxSize ? ArrayContainer.Create(bc) : bc;
    }

    public static Container operator ^(BitmapContainer x, ArrayContainer y)
    {
        var data = Clone(x._bitmap);
        var bc = new BitmapContainer(x._cardinality + y.XorArray(data), data);
        return bc._cardinality <= MaxSize ? ArrayContainer.Create(bc) : bc;
    }

    public static Container AndNot(BitmapContainer x, BitmapContainer y)
    {
        var data = Clone(x._bitmap);
        var bc = new BitmapContainer(AndNotInternal(data, y._bitmap), data);
        return bc._cardinality <= MaxSize ? ArrayContainer.Create(bc) : bc;
    }

    public static Container AndNot(BitmapContainer x, ArrayContainer y)
    {
        var data = Clone(x._bitmap);
        var bc = new BitmapContainer(x._cardinality + y.AndNotArray(data), data);
        return bc._cardinality <= MaxSize ? ArrayContainer.Create(bc) : bc;
    }

    private static int XorInternal(ulong[] first, ulong[] second)
    {
        for (var k = 0; k < BitmapLength; k++)
        {
            first[k] ^= second[k];
        }
        var c = Utils.Popcnt(first);
        return c;
    }

    private static int AndNotInternal(ulong[] first, ulong[] second)
    {
        for (var k = 0; k < first.Length; k++)
        {
            first[k] &= ~second[k];
        }
        var c = Utils.Popcnt(first);
        return c;
    }

    private static int NotInternal(ulong[] data)
    {
        for (var k = 0; k < BitmapLength; k++)
        {
            data[k] = ~data[k];
        }
        var c = Utils.Popcnt(data);
        return c;
    }

    private static int OrInternal(ulong[] first, ulong[] second)
    {
        for (var k = 0; k < BitmapLength; k++)
        {
            first[k] |= second[k];
        }
        var c = Utils.Popcnt(first);
        return c;
    }

    private static int AndInternal(ulong[] first, ulong[] second)
    {
        for (var k = 0; k < BitmapLength; k++)
        {
            first[k] &= second[k];
        }
        var c = Utils.Popcnt(first);
        return c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ushort x)
    {
        return Contains(_bitmap, x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Contains(ulong[] bitmap, ushort x)
    {
        return (bitmap[x >> 6] & (1UL << x)) != 0;
    }

    protected override bool EqualsInternal(Container other)
    {
        var bc = other as BitmapContainer;
        return bc != null && Equals(bc);
    }

    public override void EnumerateFill(List<int> list, int key)
    {
        for (var k = 0; k < BitmapLength; k++)
        {
            var bitset = _bitmap[k];
            var shiftedK = k << 6;
            while (bitset != 0)
            {
                var t = bitset & (~bitset + 1);
                var result = (ushort) (shiftedK + BitOperations.PopCount(t - 1));
                list.Add(key | result);
                bitset ^= t;
            }
        }
    }

    internal int FillArray(ushort[] data)
    {
        var pos = 0;
        for (var k = 0; k < BitmapLength; k++)
        {
            var bitset = _bitmap[k];
            var shiftedK = k << 6;
            while (bitset != 0)
            {
                var t = bitset & (~bitset + 1);
                data[pos++] = (ushort) (shiftedK + BitOperations.PopCount(t - 1));
                bitset ^= t;
            }
        }
        return _cardinality;
    }

    public override bool Equals(object? obj)
    {
        var bc = obj as BitmapContainer;
        return bc != null && Equals(bc);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var code = 17;
            code = code * 23 + _cardinality;
            for (var i = 0; i < BitmapLength; i++)
            {
                code = code * 23 + _bitmap[i].GetHashCode();
            }
            return code;
        }
    }

    public static void Serialize(BitmapContainer bc, BinaryWriter binaryWriter)
    {
        for (var i = 0; i < BitmapLength; i++)
        {
            binaryWriter.Write(bc._bitmap[i]);
        }
    }

    public static BitmapContainer Deserialize(BinaryReader binaryReader, int cardinality)
    {
        var data = GC.AllocateUninitializedArray<ulong>(BitmapLength);
        for (var i = 0; i < BitmapLength; i++)
        {
            data[i] = binaryReader.ReadUInt64();
        }
        return new BitmapContainer(cardinality, data);
    }
}