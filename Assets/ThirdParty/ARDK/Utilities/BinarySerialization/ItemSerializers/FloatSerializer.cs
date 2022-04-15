// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class FloatSerializer:
    BaseItemSerializer<float>
  {
    public static readonly FloatSerializer Instance = new FloatSerializer();

    private FloatSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, float item)
    {
      unsafe
      {
        void* address = &item;
        UInt32* uint32Address = (UInt32*)address;
        UInt32 value = *uint32Address;

        var stream = serializer.Stream;
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
      }
    }
    protected override float DoDeserialize(BinaryDeserializer deserializer)
    {
      var stream = deserializer.Stream;
      UInt32 byte1 = stream.ReadByteOrThrow();
      UInt32 byte2 = stream.ReadByteOrThrow();
      UInt32 byte3 = stream.ReadByteOrThrow();
      UInt32 byte4 = stream.ReadByteOrThrow();

      UInt32 uint32Value = 
        byte1 << 24 |
        byte2 << 16 |
        byte3 << 8 |
        byte4;

      unsafe
      {
        void* address = &uint32Value;
        float* floatAddress = (float*)address;
        return *floatAddress;
      }
    }
  }
}
