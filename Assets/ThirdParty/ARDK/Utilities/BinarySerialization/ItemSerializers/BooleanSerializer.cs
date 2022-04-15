// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class BooleanSerializer:
    BaseItemSerializer<bool>
  {
    public static readonly BooleanSerializer Instance = new BooleanSerializer();

    private BooleanSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, bool item)
    {
      byte b = item ? (byte)1 : (byte)0;
      serializer.Stream.WriteByte(b);
    }
    protected override bool DoDeserialize(BinaryDeserializer deserializer)
    {
      byte b = deserializer.Stream.ReadByteOrThrow();

      switch(b)
      {
        case 0: return false;
        case 1: return true;
        default: throw new IOException("Invalid boolean value in stream.");
      }
    }
  }
}
