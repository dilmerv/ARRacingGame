// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

using Unity.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  internal abstract class _SerializableAwarenessBufferSerializer<TBuffer, T>:
    BaseItemSerializer<TBuffer>
    where TBuffer: _SerializableAwarenessBufferBase<T>
    where T: struct
  {
    protected override void DoSerialize
      (BinarySerializer serializer, TBuffer item)
    {
      var uint32Serializer = CompressedUInt32Serializer.Instance;

      uint32Serializer.Serialize(serializer, item.Width);
      uint32Serializer.Serialize(serializer, item.Height);
      BooleanSerializer.Instance.Serialize(serializer, item.IsKeyframe);
      Matrix4x4Serializer.Instance.Serialize(serializer, item.ViewMatrix);
      CameraIntrinsicsSerializer.Instance.Serialize(serializer, item.Intrinsics);
    }

    protected override TBuffer DoDeserialize
      (BinaryDeserializer deserializer)
    {
      var uint32Serializer = CompressedUInt32Serializer.Instance;

      uint width = uint32Serializer.Deserialize(deserializer);
      uint height = uint32Serializer.Deserialize(deserializer);
      var isKeyFrame = BooleanSerializer.Instance.Deserialize(deserializer);
      var viewMatrix = Matrix4x4Serializer.Instance.Deserialize(deserializer);
      var intrinsics = CameraIntrinsicsSerializer.Instance.Deserialize(deserializer);

      return
        _InternalDeserialize
        (
          deserializer,
          width,
          height,
          isKeyFrame,
          viewMatrix,
          intrinsics
        );
    }

    protected abstract TBuffer _InternalDeserialize
    (
      BinaryDeserializer deserializer,
      uint width,
      uint height,
      bool isKeyFrame,
      Matrix4x4 view,
      CameraIntrinsics intrinsics
    );
  }
}
