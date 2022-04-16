// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class CompressedInt32Serializer:
    BaseItemSerializer<Int32>
  {
    public static readonly CompressedInt32Serializer Instance = new CompressedInt32Serializer();

    public static Int32 ReadCompressedInt32(Stream stream)
    {
      UInt32 uint32Value = CompressedUInt32Serializer.ReadCompressedUInt32(stream);

      bool isNegative = (uint32Value & 1) == 1;
      uint32Value >>= 1;

      if (isNegative)
        uint32Value = ~uint32Value;

      return (Int32)uint32Value;
    }


    /// <summary>
    /// Writes an Int32 value in "compressed" format.
    /// This uses the UInt32 compression logic, with an extra twist. -1 has all bytes set, meaning
    /// that if we just want to use the "compression" on it, it will occupy 5 bytes. So, instead, we
    /// shift the sign bit to the right and invert all the bits when a value is negative. In this
    /// way, the values 0, -1, 1, -2, 2, -3, 3 etc are actually written as 0, 1, 2, 3, 4, 5, 6,
    /// keeping just one byte instead of 4 (no compression) or 5 (bad compression for negatives).
    /// </summary>
    public static void WriteCompressedInt32(Stream stream, Int32 value)
    {
      UInt32 uint32Value = (UInt32)value;
      uint32Value <<= 1;

      if (value < 0)
        uint32Value = ~uint32Value;

      CompressedUInt32Serializer.WriteCompressedUInt32(stream, uint32Value);
    }

    private CompressedInt32Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Int32 item)
    {
      WriteCompressedInt32(serializer.Stream, item);
    }
    protected override Int32 DoDeserialize(BinaryDeserializer deserializer)
    {
      return ReadCompressedInt32(deserializer.Stream);
    }
  }
}
