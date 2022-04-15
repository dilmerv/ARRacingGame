// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class ByteSerializer:
    BaseItemSerializer<byte>
  {
    public static readonly ByteSerializer Instance = new ByteSerializer();

    private ByteSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, byte item)
    {
      serializer.Stream.WriteByte(item);
    }
    protected override byte DoDeserialize(BinaryDeserializer deserializer)
    {
      return deserializer.Stream.ReadByteOrThrow();
    }
  }
}
