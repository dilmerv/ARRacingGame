// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class CompressedUInt32Serializer:
    BaseItemSerializer<UInt32>
  {
    public static readonly CompressedUInt32Serializer Instance = new CompressedUInt32Serializer();

    public static UInt32 ReadCompressedUInt32(Stream stream)
    {
      UInt32 result = 0;
      int shiftAmount = 0;
      while(true) {
        UInt32 read = stream.ReadByteOrThrow();

        UInt32 readValue = read & ((1 << 7) - 1);
        UInt32 shiftedValue = readValue << shiftAmount;

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
    /// Writes an UInt32 value in "compressed" format.
    /// Assuming most values are small, we can possibly write a single byte instead of 4 if the
    /// value is smaller than 127. To do the "compression", we write 7 bits of the value at a time,
    /// and use the last bit to tell if there's more data or not.
    /// Unfortunately, in the worst case, we might end-up writing 5 bytes instead of 4.
    /// </summary>
    public static void WriteCompressedUInt32(Stream stream, UInt32 value)
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

    private CompressedUInt32Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, UInt32 item)
    {
      WriteCompressedUInt32(serializer.Stream, item);
    }
    protected override UInt32 DoDeserialize(BinaryDeserializer deserializer)
    {
      return ReadCompressedUInt32(deserializer.Stream);
    }
  }
}
