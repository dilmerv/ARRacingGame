// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers {

  public sealed class Vector2Serializer:
    BaseItemSerializer<Vector2>
  {
    public static readonly Vector2Serializer Instance = new Vector2Serializer();

    private Vector2Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Vector2 item)
    {
      var floatSerializer = FloatSerializer.Instance;
      floatSerializer.Serialize(serializer, item.x);
      floatSerializer.Serialize(serializer, item.y);
    }
    protected override Vector2 DoDeserialize(BinaryDeserializer deserializer)
    {
      var floatSerializer = FloatSerializer.Instance;
      float x = floatSerializer.Deserialize(deserializer);
      float y = floatSerializer.Deserialize(deserializer);
      return new Vector2(x, y);
    }
  }
}
