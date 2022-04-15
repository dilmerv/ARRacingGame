// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Linq;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

namespace Niantic.ARDK.Utilities.Marker
{
  /// Default IMetadataSerializer implementation for serializing and deserializing
  /// MarkerMetadata as
  ///   1. string SessionIdentifier
  ///   2. MarkerSource Source
  ///   3. byte[] Data (base)
  public class BasicMetadataSerializer:
    IMetadataSerializer
  {
    private static readonly BasicMetadataSerializer _instance = new BasicMetadataSerializer();

    public static byte[] StaticSerialize(MarkerMetadata stationaryMetadata)
    {
      return _instance.Serialize(stationaryMetadata);
    }

    public byte[] Serialize(MarkerMetadata metadata)
    {
      using (var stream = new MemoryStream(1024))
      {
        using (var binarySerializer = new BinarySerializer(stream))
        {
          StringSerializer.Instance.Serialize(binarySerializer, metadata.SessionIdentifier);
          CompressedInt32Serializer.Instance.Serialize(binarySerializer, (int)metadata.Source);
          CompressedInt32Serializer.Instance.Serialize(binarySerializer, metadata.Data.Length);
        }

        var markerData = stream.ToArray();
        return markerData.Concat(metadata.Data).ToArray();
      }
    }

    public static MarkerMetadata StaticDeserialize(byte[] data)
    {
      return _instance.Deserialize(data);
    }

    public MarkerMetadata Deserialize(byte[] data)
    {
      using (var stream = new MemoryStream(data))
      {
        using (var binaryDeserializer = new BinaryDeserializer(stream))
        {
          var sessionIdentifier = StringSerializer.Instance.Deserialize(binaryDeserializer);
          var source = CompressedInt32Serializer.Instance.Deserialize(binaryDeserializer);

          var remainingDataLen =
            CompressedInt32Serializer.Instance.Deserialize(binaryDeserializer);

          var buffer = new byte[remainingDataLen];
          stream.ReadOrThrow(buffer, 0, remainingDataLen);

          var markerSource = (MarkerMetadata.MarkerSource)source;
          var metadata = new MarkerMetadata(sessionIdentifier, markerSource, buffer);
          return metadata;
        }
      }
    }
  }
}
