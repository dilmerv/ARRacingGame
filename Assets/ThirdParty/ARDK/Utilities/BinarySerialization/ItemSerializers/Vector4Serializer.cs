// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers {

  public sealed class Vector4Serializer:
    BaseItemSerializer<Vector4>
  {
    public static readonly Vector4Serializer Instance = new Vector4Serializer();

    private Vector4Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Vector4 item)
    {
      var floatSerializer = FloatSerializer.Instance;
      floatSerializer.Serialize(serializer, item.x);
      floatSerializer.Serialize(serializer, item.y);
      floatSerializer.Serialize(serializer, item.z);
      floatSerializer.Serialize(serializer, item.w);
    }
    protected override Vector4 DoDeserialize(BinaryDeserializer deserializer)
    {
      var floatSerializer = FloatSerializer.Instance;
      float x = floatSerializer.Deserialize(deserializer);
      float y = floatSerializer.Deserialize(deserializer);
      float z = floatSerializer.Deserialize(deserializer);
      float w = floatSerializer.Deserialize(deserializer);
      return new Vector4(x, y, z, w);
    }
  }
}
