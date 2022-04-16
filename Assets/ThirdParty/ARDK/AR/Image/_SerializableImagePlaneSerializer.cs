// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

using Unity.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.Image
{
  internal sealed class _SerializableImagePlaneSerializer:
    BaseItemSerializer<_SerializableImagePlane>
  {
    internal static readonly _SerializableImagePlaneSerializer _instance =
      new _SerializableImagePlaneSerializer();

    private _SerializableImagePlaneSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, _SerializableImagePlane item)
    {
      NativeArraySerializer<byte>.Instance.Serialize(serializer, item.Data);

      var int32Serializer = CompressedInt32Serializer.Instance;
      int32Serializer.Serialize(serializer, item.PixelWidth);
      int32Serializer.Serialize(serializer, item.PixelHeight);
      int32Serializer.Serialize(serializer, item.BytesPerRow);
      int32Serializer.Serialize(serializer, item.BytesPerPixel);
    }

    protected override _SerializableImagePlane DoDeserialize(BinaryDeserializer deserializer)
    {
      var data = NativeArraySerializer<byte>.Instance.Deserialize(deserializer);

      var int32Deserializer = CompressedInt32Serializer.Instance;
      int pixelWidth = int32Deserializer.Deserialize(deserializer);
      int pixelHeight = int32Deserializer.Deserialize(deserializer);
      int bytesPerRow = int32Deserializer.Deserialize(deserializer);
      int bytesPerPixel = int32Deserializer.Deserialize(deserializer);

      var result =
        new _SerializableImagePlane(data, pixelWidth, pixelHeight, bytesPerRow, bytesPerPixel);

      return result;
    }
  }
}
