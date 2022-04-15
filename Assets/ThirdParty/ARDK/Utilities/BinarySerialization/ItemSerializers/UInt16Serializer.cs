// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{

  public sealed class UInt16Serializer:
    BaseItemSerializer<UInt16>
  {
    public static readonly UInt16Serializer Instance = new UInt16Serializer();

    private UInt16Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, UInt16 item)
    {
      unchecked
      {
        var stream = serializer.Stream;
        stream.WriteByte((byte)(item >> 8));
        stream.WriteByte((byte)item);
      }
    }
    protected override UInt16 DoDeserialize(BinaryDeserializer deserializer)
    {
      var stream = deserializer.Stream;
      UInt16 byte1 = stream.ReadByteOrThrow();
      UInt16 byte2 = stream.ReadByteOrThrow();

      return (UInt16)((byte1 << 8) | byte2);
    }
  }
}
