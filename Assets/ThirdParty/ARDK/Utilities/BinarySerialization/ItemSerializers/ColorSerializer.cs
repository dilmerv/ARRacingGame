// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{

  public sealed class ColorSerializer:
    BaseItemSerializer<Color>
  {
    public static readonly ColorSerializer Instance = new ColorSerializer();

    private ColorSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Color item)
    {
      var floatSerializer = FloatSerializer.Instance;
      floatSerializer.Serialize(serializer, item.r);
      floatSerializer.Serialize(serializer, item.g);
      floatSerializer.Serialize(serializer, item.b);
      floatSerializer.Serialize(serializer, item.a);
    }
    protected override Color DoDeserialize(BinaryDeserializer deserializer)
    {
      var floatSerializer = FloatSerializer.Instance;
      float r = floatSerializer.Deserialize(deserializer);
      float g = floatSerializer.Deserialize(deserializer);
      float b = floatSerializer.Deserialize(deserializer);
      float a = floatSerializer.Deserialize(deserializer);
      return new Color(r, g, b, a);
    }
  }
}
