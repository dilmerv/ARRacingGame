// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class IntPtrSerializer:
    BaseItemSerializer<IntPtr>
  {
    public static readonly IntPtrSerializer Instance = new IntPtrSerializer();

    private IntPtrSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, IntPtr item)
    {
      Int64 value = (Int64)item;
      var stream = serializer.Stream;
      stream.WriteByte((byte)(value >> 56));
      stream.WriteByte((byte)(value >> 48));
      stream.WriteByte((byte)(value >> 40));
      stream.WriteByte((byte)(value >> 32));
      stream.WriteByte((byte)(value >> 24));
      stream.WriteByte((byte)(value >> 16));
      stream.WriteByte((byte)(value >> 8));
      stream.WriteByte((byte)value);
    }
    protected override IntPtr DoDeserialize(BinaryDeserializer deserializer)
    {
      var stream = deserializer.Stream;
      UInt64 byte1 = stream.ReadByteOrThrow();
      UInt64 byte2 = stream.ReadByteOrThrow();
      UInt64 byte3 = stream.ReadByteOrThrow();
      UInt64 byte4 = stream.ReadByteOrThrow();
      UInt64 byte5 = stream.ReadByteOrThrow();
      UInt64 byte6 = stream.ReadByteOrThrow();
      UInt64 byte7 = stream.ReadByteOrThrow();
      UInt64 byte8 = stream.ReadByteOrThrow();

      UInt64 result = 
        byte1 << 56 |
        byte2 << 48 |
        byte3 << 40 |
        byte4 << 32 |
        byte5 << 24 |
        byte6 << 16 |
        byte7 << 8 |
        byte8;

      return (IntPtr)result;
    }
  }
}
