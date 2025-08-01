﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Equativ.RoaringBitmaps;

public class RoaringBitmap : IEnumerable<int>, IEquatable<RoaringBitmap>
{
    private readonly RoaringArray _highLowContainer;

    private RoaringBitmap(RoaringArray input)
    {
        _highLowContainer = input;
    }

    public long Cardinality => _highLowContainer.Cardinality;

    public IEnumerator<int> GetEnumerator()
    {
        return ToArray().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    /// <summary>
    /// Convert the bitmap to an array of integers
    /// </summary>
    /// <returns>Array of integers</returns>
    public List<int> ToArray()
    {
        var list = new List<int>((int)Cardinality);
        _highLowContainer.EnumerateFill(list);
        return list;
    }
    
    public bool Equals(RoaringBitmap? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        return _highLowContainer.Equals(other._highLowContainer);
    }
    
    public override bool Equals(object? obj)
    {
        var ra = obj as RoaringBitmap;
        return ra != null && Equals(ra);
    }

    public override int GetHashCode()
    {
        return (13 ^ _highLowContainer.GetHashCode()) << 3;
    }

    /// <summary>
    /// Creates a new immutable RoaringBitmap from an existing list of integers
    /// </summary>
    /// <param name="values">List of integers</param>
    /// <returns>RoaringBitmap</returns>
    public static RoaringBitmap Create(params int[] values)
    {
        return Create(values.AsEnumerable());
    }

    /// <summary>
    /// Optimizes a RoaringBitmap to prepare e.g. for Serialization/Deserialization
    /// </summary>
    /// <returns>RoaringBitmap</returns>
    public RoaringBitmap Optimize()
    {
        return new RoaringBitmap(RoaringArray.Optimize(_highLowContainer));
    }

    /// <summary>
    /// Creates a new immutable RoaringBitmap from an existing list of integers
    /// </summary>
    /// <param name="values">List of integers</param>
    /// <returns>RoaringBitmap</returns>
    public static RoaringBitmap Create(IEnumerable<int> values)
    {
        var data = values as int[] ?? values.ToArray();
        if (data.Length == 0)
        {
            return new RoaringBitmap(new RoaringArray(0, new List<ushort>(), new List<Container>()));
        }

        Array.Sort(data);

        // In-place deduplication (two pointers technique)
        var uniqueCount = 1;
        for (var i = 1; i < data.Length; i++)
        {
            if (data[i] != data[uniqueCount - 1])
            {
                data[uniqueCount++] = data[i];
            }
        }

        var keys = new List<ushort>();
        var containers = new List<Container>();
        var index = 0;

        while (index < uniqueCount)
        {
            var hb = Utils.HighBits(data[index]);
            var start = index;
            index++;
            while (index < uniqueCount && Utils.HighBits(data[index]) == hb)
            {
                index++;
            }

            var count = index - start;
            var lows = new ushort[count];
            for (var j = 0; j < count; j++)
            {
                lows[j] = Utils.LowBits(data[start + j]);
            }

            keys.Add(hb);
            containers.Add(count > Container.MaxSize
                ? BitmapContainer.Create(lows)
                : ArrayContainer.Create(lows));
        }

        return new RoaringBitmap(new RoaringArray(keys.Count, keys, containers));
    }

    /// <summary>
    /// Bitwise Or operation of two RoaringBitmaps
    /// </summary>
    /// <param name="x">RoaringBitmap</param>
    /// <param name="y">RoaringBitmap</param>
    /// <returns>RoaringBitmap</returns>
    public static RoaringBitmap operator |(RoaringBitmap x, RoaringBitmap y)
    {
        return new RoaringBitmap(x._highLowContainer | y._highLowContainer);
    }

    /// <summary>
    /// Bitwise And operation of two RoaringBitmaps
    /// </summary>
    /// <param name="x">RoaringBitmap</param>
    /// <param name="y">RoaringBitmap</param>
    /// <returns>RoaringBitmap</returns>
    public static RoaringBitmap operator &(RoaringBitmap x, RoaringBitmap y)
    {
        return new RoaringBitmap(x._highLowContainer & y._highLowContainer);
    }

    /// <summary>
    /// Bitwise Not operation of a RoaringBitmap
    /// </summary>
    /// <param name="x">RoaringBitmap</param>
    /// <returns>RoaringBitmap</returns>
    public static RoaringBitmap operator ~(RoaringBitmap x)
    {
        return new RoaringBitmap(~x._highLowContainer);
    }

    /// <summary>
    /// Bitwise Xor operation of two RoaringBitmaps
    /// </summary>
    /// <param name="x">RoaringBitmap</param>
    /// <param name="y">RoaringBitmap</param>
    /// <returns>RoaringBitmap</returns>
    public static RoaringBitmap operator ^(RoaringBitmap x, RoaringBitmap y)
    {
        return new RoaringBitmap(x._highLowContainer ^ y._highLowContainer);
    }

    /// <summary>
    /// Bitwise AndNot operation of two RoaringBitmaps
    /// </summary>
    /// <param name="x">RoaringBitmap</param>
    /// <param name="y">RoaringBitmap</param>
    /// <returns>RoaringBitmap</returns>
    public static RoaringBitmap AndNot(RoaringBitmap x, RoaringBitmap y)
    {
        return new RoaringBitmap(RoaringArray.AndNot(x._highLowContainer, y._highLowContainer));
    }

    /// <summary>
    /// Serializes a RoaringBitmap into a stream using the 'official' RoaringBitmap file format
    /// </summary>
    /// <param name="roaringBitmap">RoaringBitmap</param>
    /// <param name="stream">Stream</param>
    public static void Serialize(RoaringBitmap roaringBitmap, Stream stream)
    {
        RoaringArray.Serialize(roaringBitmap._highLowContainer, stream);
    }

    /// <summary>
    /// Deserializes a RoaringBitmap from astream using the 'official' RoaringBitmap file format
    /// </summary>
    /// <param name="stream">Stream</param>
    public static RoaringBitmap Deserialize(Stream stream)
    {
        var ra = RoaringArray.Deserialize(stream);
        return new RoaringBitmap(ra);
    }
}