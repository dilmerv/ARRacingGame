// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class Int16Serializer:
    BaseItemSerializer<Int16>
  {
    public static readonly Int16Serializer Instance = new Int16Serializer();

    private Int16Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Int16 item)
    {
      UInt16Serializer.Instance.Serialize(serializer, (UInt16)item);
    }
    protected override Int16 DoDeserialize(BinaryDeserializer deserializer)
    {
      return (Int16)UInt16Serializer.Instance.Deserialize(deserializer);
    }
  }
}
