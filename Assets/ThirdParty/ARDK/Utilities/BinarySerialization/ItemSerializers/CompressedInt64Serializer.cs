// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class CompressedInt64Serializer:
    BaseItemSerializer<Int64>
  {
    public static readonly CompressedInt64Serializer Instance = new CompressedInt64Serializer();

    public static Int64 ReadCompressedInt64(Stream stream)
    {
      UInt64 uint64Value = CompressedUInt64Serializer.ReadCompressedUInt64(stream);

      bool isNegative = (uint64Value & 1) == 1;
      uint64Value >>= 1;

      if (isNegative)
        uint64Value = ~uint64Value;

      return (Int64)uint64Value;
    }

    /// <summary>
    /// Writes an Int64 value in "compressed" format.
    /// This uses the UInt64 compression logic, with an extra twist. -1 has all bytes set, meaning
    /// that if we just want to use the "compression" on it, it will occupy 10 bytes. So, instead,
    /// we shift the sign bit to the right and invert all the bits when a value is negative. In this
    /// way, the values 0, -1, 1, -2, 2, -3, 3 etc are actually written as 0, 1, 2, 3, 4, 5, 6,
    /// keeping just one byte instead of 4 (no compression) or 10 (bad compression for negatives).
    /// </summary>
    public static void WriteCompressedInt64(Stream stream, Int64 value)
    {
      UInt64 uint64Value = (UInt64)value;
      uint64Value <<= 1;

      if (value < 0)
        uint64Value = ~uint64Value;

      CompressedUInt64Serializer.WriteCompressedUInt64(stream, uint64Value);
    }

    private CompressedInt64Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Int64 item)
    {
      WriteCompressedInt64(serializer.Stream, item);
    }
    protected override Int64 DoDeserialize(BinaryDeserializer deserializer)
    {
      return ReadCompressedInt64(deserializer.Stream);
    }
  }
}
