// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class GuidSerializer:
    BaseItemSerializer<Guid>
  {
    public static readonly GuidSerializer Instance = new GuidSerializer();

    private GuidSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Guid item)
    {
      var array = item.ToByteArray();
      ByteArraySerializer.Instance.Serialize(serializer, array);
    }
    protected override Guid DoDeserialize(BinaryDeserializer deserializer)
    {
      var array = ByteArraySerializer.Instance.Deserialize(deserializer);
      return new Guid(array);
    }
  }
}
