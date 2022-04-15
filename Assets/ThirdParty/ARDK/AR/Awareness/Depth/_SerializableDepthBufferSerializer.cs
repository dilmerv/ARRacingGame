// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;
using Niantic.ARDK.AR.Camera;

using Unity.Collections;
using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Depth
{
  internal sealed class _SerializableDepthBufferSerializer:
    _SerializableAwarenessBufferSerializer<_SerializableDepthBuffer, float>
  {
    private const bool USE_JPEG_COMPRESSION = true;

    // 0-100. Better use higher quality as depth buffer resolution is low and
    // artifacts from compression affects more.
    private const int JPEG_COMPRESSION_QUALITY = 95;
    internal static readonly _SerializableDepthBufferSerializer _instance =
      new _SerializableDepthBufferSerializer();

    private _SerializableDepthBufferSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, _SerializableDepthBuffer item)
    {
      base.DoSerialize(serializer, item);

      var compressedData = _VideoStreamHelper._CompressForDepthBuffer
      (
        (int)item.Width,
        (int)item.Height,
        item.Data,
        USE_JPEG_COMPRESSION,
        JPEG_COMPRESSION_QUALITY
      );
      ByteArraySerializer.Instance.Serialize(serializer, compressedData);

      var floatSerializer = FloatSerializer.Instance;
      floatSerializer.Serialize(serializer, item.NearDistance);
      floatSerializer.Serialize(serializer, item.FarDistance);
    }

    protected override _SerializableDepthBuffer _InternalDeserialize
    (
      BinaryDeserializer deserializer,
      uint width,
      uint height,
      bool isKeyFrame,
      Matrix4x4 view,
      CameraIntrinsics intrinsics
    )
    {
      var compressedData = ByteArraySerializer.Instance.Deserialize(deserializer);
      var data = _VideoStreamHelper._DecompressForDepthBuffer(compressedData, USE_JPEG_COMPRESSION);

      var floatSerializer = FloatSerializer.Instance;
      float nearDistance = floatSerializer.Deserialize(deserializer);
      float farDistance = floatSerializer.Deserialize(deserializer);

      return
        new _SerializableDepthBuffer
        (
          width,
          height,
          isKeyFrame,
          view,
          data,
          nearDistance,
          farDistance,
          intrinsics
        );
    }
  }
}
