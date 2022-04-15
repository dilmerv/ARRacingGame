// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class Matrix4x4Serializer:
    BaseItemSerializer<Matrix4x4>
  {
    public static readonly Matrix4x4Serializer Instance = new Matrix4x4Serializer();

    private Matrix4x4Serializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Matrix4x4 item)
    {
      var vector4Serializer = Vector4Serializer.Instance;
      vector4Serializer.Serialize(serializer, item.GetColumn(0));
      vector4Serializer.Serialize(serializer, item.GetColumn(1));
      vector4Serializer.Serialize(serializer, item.GetColumn(2));
      vector4Serializer.Serialize(serializer, item.GetColumn(3));
    }
    protected override Matrix4x4 DoDeserialize(BinaryDeserializer deserializer)
    {
      var vector4Serializer = Vector4Serializer.Instance;
      var column0 = vector4Serializer.Deserialize(deserializer);
      var column1 = vector4Serializer.Deserialize(deserializer);
      var column2 = vector4Serializer.Deserialize(deserializer);
      var column3 = vector4Serializer.Deserialize(deserializer);
      return new Matrix4x4(column0, column1, column2, column3);
    }
  }
}
