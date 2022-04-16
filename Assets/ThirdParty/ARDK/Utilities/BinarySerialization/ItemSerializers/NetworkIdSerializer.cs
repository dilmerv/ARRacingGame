// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking.HLAPI.Routing;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class NetworkIdSerializer:
    BaseItemSerializer<NetworkId>
  {
    public static readonly NetworkIdSerializer Instance = new NetworkIdSerializer();

    private NetworkIdSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, NetworkId item)
    {
      CompressedUInt64Serializer.Instance.Serialize(serializer, item.RawId);
    }
    protected override NetworkId DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt64 rawId = CompressedUInt64Serializer.Instance.Deserialize(deserializer);
      return new NetworkId(rawId);
    }
  }
}
