// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class CompressedUInt64Serializer:
    BaseItemSerializer<UInt64>
  {
    public static readonly CompressedUInt64Serializer Instance = new CompressedUInt64Serializer();

    public static UInt64 ReadCompressedUInt64(Stream stream)
    {
      UInt64 result = 0;
      int shiftAmount = 0;
      while(true) {
        UInt64 read = stream.ReadByteOrThrow();

        UInt64 readValue = read & ((1 << 7) - 1);
        UInt64 shiftedValue = readValue << shiftAmount;

        // Even in checked scopes, bit shifting doesn't throw. So, we check for an overflow by
        // making a shifted value be shifted in the opposite direction and comparing to the
        // unshifted value. If it is not the same value anymore, that means we had an overflow.
        if ((shiftedValue >> shiftAmount) != readValue)
          throw new IOException("Overflow when reading compressed int.");

        result |= shiftedValue;

        if ((read >> 7) == 0)
          return result;

        shiftAmount += 7;
      }
    }

    /// <summary>
    /// Writes an UInt64 value in "compressed" format.
    /// Assuming most values are small, we can possibly write a single byte instead of 8 if the
    /// value is smaller than 127. To do the "compression", we write 7 bits of the value at a time,
    /// and use the last bit to tell if there's more data or not.
    /// Unfortunately, in the worst case, we might end-up writing 10 bytes instead of 8.
    /// </summary>
    public static void WriteCompressedUInt64(Stream stream, UInt64 value)
    {
      while(true)
      {
        byte b = (byte)value;
        value >>= 7;

        if (value > 0)
          b |= 1 << 7;

        stream.WriteByte(b);

        if (value == 0)
          break;
      }
    }

    private CompressedUInt64Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, UInt64 item)
    {
      WriteCompressedUInt64(serializer.Stream, item);
    }
    protected override UInt64 DoDeserialize(BinaryDeserializer deserializer)
    {
      return ReadCompressedUInt64(deserializer.Stream);
    }
  }
}
