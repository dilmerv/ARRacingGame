// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class ResolutionSerializer:
    BaseItemSerializer<Resolution>
  {
    public static readonly ResolutionSerializer Instance = new ResolutionSerializer();

    private ResolutionSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, Resolution item)
    {
      var int32Serializer = CompressedInt32Serializer.Instance;
      int32Serializer.Serialize(serializer, item.width);
      int32Serializer.Serialize(serializer, item.height);
    }
    protected override Resolution DoDeserialize(BinaryDeserializer deserializer)
    {
      var int32Serializer = CompressedInt32Serializer.Instance;
      Int32 width = int32Serializer.Deserialize(deserializer);
      Int32 height = int32Serializer.Deserialize(deserializer);

      return new Resolution { width = width, height = height };
    }
  }
}
