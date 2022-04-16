// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers {

  public sealed class Vector3Serializer:
    BaseItemSerializer<Vector3>
  {
    public static readonly Vector3Serializer Instance = new Vector3Serializer();

    private Vector3Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Vector3 item)
    {
      var floatSerializer = FloatSerializer.Instance;
      floatSerializer.Serialize(serializer, item.x);
      floatSerializer.Serialize(serializer, item.y);
      floatSerializer.Serialize(serializer, item.z);
    }
    protected override Vector3 DoDeserialize(BinaryDeserializer deserializer)
    {
      var floatSerializer = FloatSerializer.Instance;
      float x = floatSerializer.Deserialize(deserializer);
      float y = floatSerializer.Deserialize(deserializer);
      float z = floatSerializer.Deserialize(deserializer);
      return new Vector3(x, y, z);
    }
  }
}
