// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class ByteArraySerializer:
    BaseItemSerializer<byte[]>
  {
    public static readonly ByteArraySerializer Instance = new ByteArraySerializer();

    private ByteArraySerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, byte[] item)
    {
      int length = item.Length;

      var arrayLengthLimiter = serializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);

      CompressedUInt32Serializer.Instance.Serialize(serializer, (UInt32)length);
      serializer.Stream.Write(item, 0, item.Length);
    }

    protected override byte[] DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt32 unsignedLength = CompressedUInt32Serializer.Instance.Deserialize(deserializer);
      if (unsignedLength == 0)
        return EmptyArray<byte>.Instance;

      Int32 length = checked((Int32)unsignedLength);
      var arrayLengthLimiter = deserializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);

      byte[] result = new byte[length];
      deserializer.Stream.ReadOrThrow(result, 0, length);
      return result;
    }
  }
}
