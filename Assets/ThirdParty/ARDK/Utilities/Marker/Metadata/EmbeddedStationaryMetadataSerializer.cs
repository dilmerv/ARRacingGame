// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

using UnityEngine;

namespace Niantic.ARDK.Utilities.Marker
{
  // TODO: This entire class seems unnecessary.
  
  /// Default IMetadataSerializer implementation for serializing and deserializing
  /// StationaryMarkerMetadata as
  ///   1. string SessionIdentifier (base)
  ///   2. MarkerSource Source (base)
  ///   3. Vector3 RealWorldTransform.Position
  ///   4. Quaternion RealWorldTransform.Rotation
  ///   5. Vector3[4] DetectionPointPositions
  ///   6. byte[] Data (base)
  /// @note StationaryMarkerMetadata that is serialized using this class can be deserialized using the
  ///   BasicMetadataSerializer.
  public class EmbeddedStationaryMetadataSerializer:
    IMetadataSerializer
  {
    private static readonly EmbeddedStationaryMetadataSerializer _instance =
      new EmbeddedStationaryMetadataSerializer();

    public static byte[] StaticSerialize(StationaryMarkerMetadata stationaryMetadata)
    {
      return _instance.Serialize(stationaryMetadata);
    }

    private void WriteVector3(Vector3 inVector, byte[] buffer, int startIndex)
    {
      BitConverter.GetBytes(inVector.x).CopyTo(buffer, startIndex);
      BitConverter.GetBytes(inVector.y).CopyTo(buffer, startIndex + 4);
      BitConverter.GetBytes(inVector.z).CopyTo(buffer, startIndex + 8);
    }

    public byte[] Serialize(MarkerMetadata metadata)
    {
      var stationaryMetadata = metadata as StationaryMarkerMetadata;

      if (stationaryMetadata == null)
      {
        Debug.LogError
        (
          "EmbeddedStationaryMetadataSerializer can only be used to serialize " +
          "StationaryMarkerMetadata objects"
        );

        return null;
      }

      using (var memoryStream = new MemoryStream())
      {
        using (var binarySerializer = new BinarySerializer(memoryStream))
        {
          var realWorldTransform = stationaryMetadata.RealWorldTransform;
          var detectionPoints = stationaryMetadata.DetectionPointPositions;

          var vector3Serializer = Vector3Serializer.Instance;

          vector3Serializer.Serialize(binarySerializer, realWorldTransform.ToPosition());

          vector3Serializer.Serialize
            (binarySerializer, realWorldTransform.ToRotation().eulerAngles);

          vector3Serializer.Serialize(binarySerializer, detectionPoints[0]);
          vector3Serializer.Serialize(binarySerializer, detectionPoints[1]);
          vector3Serializer.Serialize(binarySerializer, detectionPoints[2]);
          vector3Serializer.Serialize(binarySerializer, detectionPoints[3]);
        }

        var basicData = BasicMetadataSerializer.StaticSerialize(metadata);
        var stationaryData = memoryStream.ToArray();

        // TODO: We can possibly just serialize basicData first and avoid this copy.
        // Not changing this login in this MR.
        var allData = new byte[basicData.Length + stationaryData.Length];
        basicData.CopyTo(allData, 0);
        stationaryData.CopyTo(allData, basicData.Length);
        return allData;
      }
    }

    public static StationaryMarkerMetadata StaticDeserialize(byte[] data)
    {
      return (StationaryMarkerMetadata)_instance.Deserialize(data);
    }

    public MarkerMetadata Deserialize(byte[] data)
    {
      using (var memoryStream = new MemoryStream(data))
      {
        using (var binaryDeserializer = new BinaryDeserializer(memoryStream))
        {
          var sessionIdentifier = StringSerializer.Instance.Deserialize(binaryDeserializer);
          
          // read the Source but don't need it
          CompressedInt32Serializer.Instance.Deserialize(binaryDeserializer);

          var userDataLen = CompressedInt32Serializer.Instance.Deserialize(binaryDeserializer);

          var userData = new byte[userDataLen];
          memoryStream.ReadOrThrow(userData, 0, userDataLen);

          var vector3Deserializer = Vector3Serializer.Instance;
          var worldPosition = vector3Deserializer.Deserialize(binaryDeserializer);
          var worldRotationEuler = vector3Deserializer.Deserialize(binaryDeserializer);
          var worldRotation = Quaternion.Euler(worldRotationEuler);

          var objectPoints = new Vector3[4];
          objectPoints[0] = vector3Deserializer.Deserialize(binaryDeserializer);
          objectPoints[1] = vector3Deserializer.Deserialize(binaryDeserializer);
          objectPoints[2] = vector3Deserializer.Deserialize(binaryDeserializer);
          objectPoints[3] = vector3Deserializer.Deserialize(binaryDeserializer);

          var stationaryMetadata = new StationaryMarkerMetadata
          (
            sessionIdentifier,
            userData,
            Matrix4x4.TRS(worldPosition, worldRotation, Vector3.one),
            objectPoints
          );

          return stationaryMetadata;
        }
      }
    }
  }
}
