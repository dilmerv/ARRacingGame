// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
using T = Niantic.ARDK.AR.Configuration._NativeARWorldTrackingConfiguration;
#else
using T = Niantic.ARDK.AR.Configuration._SerializableARWorldTrackingConfiguration;
#endif
using System;

using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

namespace Niantic.ARDK.AR.Configuration
{
  internal sealed class _ARWorldTrackingConfigurationSerializer:
    BaseItemSerializer<T>
  {
    internal static readonly _ARWorldTrackingConfigurationSerializer _instance =
      new _ARWorldTrackingConfigurationSerializer();

    private _ARWorldTrackingConfigurationSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, T item)
    {
      // Base
      BooleanSerializer.Instance.Serialize(serializer, item.IsAutoFocusEnabled);
      BooleanSerializer.Instance.Serialize(serializer, item.IsSharedExperienceEnabled);

      // World Tracking
      BooleanSerializer.Instance.Serialize(serializer, item.IsLightEstimationEnabled);
      EnumSerializer.ForType<WorldAlignment>().Serialize(serializer, item.WorldAlignment);
      EnumSerializer.ForType<PlaneDetection>().Serialize(serializer, item.PlaneDetection);

      // Depth
      BooleanSerializer.Instance.Serialize(serializer, item.IsDepthEnabled);
      CompressedUInt32Serializer.Instance.Serialize(serializer, item.DepthTargetFrameRate);

      // Semantics
      BooleanSerializer.Instance.Serialize(serializer, item.IsSemanticSegmentationEnabled);
      CompressedUInt32Serializer.Instance.Serialize(serializer, item.SemanticTargetFrameRate);

      // Meshing
      BooleanSerializer.Instance.Serialize(serializer, item.IsMeshingEnabled);
      CompressedUInt32Serializer.Instance.Serialize(serializer, item.MeshingTargetFrameRate);
      FloatSerializer.Instance.Serialize(serializer, item.MeshingTargetBlockSize);

      // Mapping
      EnumSerializer.ForType<MappingRole>().Serialize(serializer, item.MappingRole);
      serializer.Serialize(item.MapLayerIdentifier);
    }

    protected override T DoDeserialize(BinaryDeserializer deserializer)
    {
      var result = new T();

      // Base
      result.IsAutoFocusEnabled = BooleanSerializer.Instance.Deserialize(deserializer);
      result.IsSharedExperienceEnabled = BooleanSerializer.Instance.Deserialize(deserializer);

      // World Tracking
      result.IsLightEstimationEnabled = BooleanSerializer.Instance.Deserialize(deserializer);
      result.WorldAlignment = EnumSerializer.ForType<WorldAlignment>().Deserialize(deserializer);
      result.PlaneDetection = EnumSerializer.ForType<PlaneDetection>().Deserialize(deserializer);

      // Depth
      result.IsDepthEnabled = BooleanSerializer.Instance.Deserialize(deserializer);
      result.DepthTargetFrameRate = CompressedUInt32Serializer.Instance.Deserialize(deserializer);

      // Semantics
      result.IsSemanticSegmentationEnabled = BooleanSerializer.Instance.Deserialize(deserializer);
      result.SemanticTargetFrameRate = CompressedUInt32Serializer.Instance.Deserialize(deserializer);

      // Meshing
      result.IsMeshingEnabled = BooleanSerializer.Instance.Deserialize(deserializer);
      result.MeshingTargetFrameRate = CompressedUInt32Serializer.Instance.Deserialize(deserializer);
      result.MeshingTargetBlockSize = FloatSerializer.Instance.Deserialize(deserializer);

      // Mapping
      result.MappingRole = EnumSerializer.ForType<MappingRole>().Deserialize(deserializer);
      result.MapLayerIdentifier = (MapLayerIdentifier) deserializer.Deserialize();

      return result;
    }
  }
}
