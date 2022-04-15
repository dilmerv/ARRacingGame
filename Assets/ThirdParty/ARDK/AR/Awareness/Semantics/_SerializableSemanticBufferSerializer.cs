// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Camera;

using Unity.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Semantics
{
  internal sealed class _SerializableSemanticBufferSerializer:
    _SerializableAwarenessBufferSerializer<_SerializableSemanticBuffer, UInt32>
  {
    internal static readonly _SerializableSemanticBufferSerializer _instance =
      new _SerializableSemanticBufferSerializer();

    private _SerializableSemanticBufferSerializer()
    {
    }

    protected override void DoSerialize
      (BinarySerializer serializer, _SerializableSemanticBuffer item)
    {
      base.DoSerialize(serializer, item);

      NativeArraySerializer<UInt32>.Instance.Serialize(serializer, item.Data);
      var uint32Serializer = CompressedUInt32Serializer.Instance;
      uint32Serializer.Serialize(serializer, item.ChannelCount);

      foreach (var name in item.ChannelNames)
      {
        StringSerializer.Instance.Serialize(serializer, name);
      }
    }

    protected override _SerializableSemanticBuffer _InternalDeserialize
    (
      BinaryDeserializer deserializer,
      uint width,
      uint height,
      bool isKeyFrame,
      Matrix4x4 view,
      CameraIntrinsics intrinsics
    )
    {
      var data = NativeArraySerializer<UInt32>.Instance.Deserialize(deserializer);
      var uint32Deserializer = CompressedUInt32Serializer.Instance;
      var channelCount = uint32Deserializer.Deserialize(deserializer);
      string[] channelNames = new string[channelCount];
      for (int i = 0; i < channelCount; i++)
      {
        channelNames[i] = StringSerializer.Instance.Deserialize(deserializer);
      }

      return
        new _SerializableSemanticBuffer
        (
          width,
          height,
          isKeyFrame,
          view,
          data,
          channelNames,
          intrinsics
        );
    }
  }
}
