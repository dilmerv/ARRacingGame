// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class BooleanArraySerializer:
    BaseItemSerializer<bool[]>
  {
    public static readonly BooleanArraySerializer Instance = new BooleanArraySerializer();

    private BooleanArraySerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, bool[] item)
    {
      var length = item.Length;

      var arrayLengthLimiter = serializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow((length+7) / 8);
      CompressedUInt32Serializer.Instance.Serialize(serializer, (UInt32)length);

      var stream = serializer.Stream;
      byte b = 0;
      int shift = 7;
      for (int i = 0; i < length; i++)
      {
        if (item[i])
          b |= (byte)(1 << shift);

        if (shift != 0)
          shift--;
        else
        {
          shift = 7;
          stream.WriteByte(b);
          b = 0;
        }
      }

      if (shift != 7)
        stream.WriteByte(b);
    }
    protected override bool[] DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt32 unsignedLength = CompressedUInt32Serializer.Instance.Deserialize(deserializer);
      if (unsignedLength == 0)
        return EmptyArray<bool>.Instance;

      Int32 length = checked((Int32)unsignedLength);
      var arrayLengthLimiter = deserializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow((length+7) / 8);

      var stream = deserializer.Stream;
      bool[] result = new bool[length];
      for (int i = 0; i < length; i += 8)
      {
        byte b = stream.ReadByteOrThrow();

        _SetValue(result, b >> 7, i);
        _SetValue(result, b >> 6, i + 1);
        _SetValue(result, b >> 5, i + 2);
        _SetValue(result, b >> 4, i + 3);
        _SetValue(result, b >> 3, i + 4);
        _SetValue(result, b >> 2, i + 5);
        _SetValue(result, b >> 1, i + 6);
        _SetValue(result, b, i + 7);
      }

      return result;
    }

    private static void _SetValue(bool[] array, int value, int index)
    {
      // We don't check if index is bigger than the length of the array because the
      // values for the last byte (if length is not multiple of 8) should be 0 for non-existing
      // indexes. And, if it is not (which is an error) .NET will throw.
      if ((value & 1) == 1)
        array[index] = true;
    }
  }
}
