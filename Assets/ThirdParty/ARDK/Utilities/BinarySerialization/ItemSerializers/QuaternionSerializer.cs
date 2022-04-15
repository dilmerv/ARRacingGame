// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class QuaternionSerializer:
    BaseItemSerializer<Quaternion>
  {
    public static readonly QuaternionSerializer Instance = new QuaternionSerializer();

    private QuaternionSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Quaternion item)
    {
      var floatSerializer = FloatSerializer.Instance;
      floatSerializer.Serialize(serializer, item.x);
      floatSerializer.Serialize(serializer, item.y);
      floatSerializer.Serialize(serializer, item.z);
      floatSerializer.Serialize(serializer, item.w);
    }
    protected override Quaternion DoDeserialize(BinaryDeserializer deserializer)
    {
      var floatSerializer = FloatSerializer.Instance;
      float x = floatSerializer.Deserialize(deserializer);
      float y = floatSerializer.Deserialize(deserializer);
      float z = floatSerializer.Deserialize(deserializer);
      float w = floatSerializer.Deserialize(deserializer);
      return new Quaternion(x, y, z, w);
    }
  }
}
