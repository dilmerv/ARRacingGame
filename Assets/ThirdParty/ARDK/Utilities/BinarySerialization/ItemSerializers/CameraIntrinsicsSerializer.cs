// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Camera;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class CameraIntrinsicsSerializer:
    BaseItemSerializer<CameraIntrinsics>
  {
    public static readonly CameraIntrinsicsSerializer Instance = new CameraIntrinsicsSerializer();

    private CameraIntrinsicsSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, CameraIntrinsics item)
    {
      Vector2Serializer.Instance.Serialize(serializer, item.FocalLength);
      Vector2Serializer.Instance.Serialize(serializer, item.PrincipalPoint);
    }
    protected override CameraIntrinsics DoDeserialize(BinaryDeserializer deserializer)
    {
      var focalLength = Vector2Serializer.Instance.Deserialize(deserializer);
      var principalPoint = Vector2Serializer.Instance.Deserialize(deserializer);
      return new CameraIntrinsics(focalLength.x, focalLength.y, principalPoint.x, principalPoint.y);
    }
  }
}
