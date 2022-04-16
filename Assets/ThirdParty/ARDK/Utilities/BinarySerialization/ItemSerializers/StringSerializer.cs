// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Text;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.BinarySerialization.Contexts;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class StringSerializer:
    BaseItemSerializer<string>
  {
    public static readonly StringSerializer Instance = new StringSerializer();

    private StringSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, string item)
    {
      int length = item.Length;

      if (length == 0)
      {
        CompressedUInt32Serializer.Instance.Serialize(serializer, 0);
        return;
      }

      var encoder = Encoding.UTF8;
      var bytes = encoder.GetBytes(item);
      length = bytes.Length;

      var arrayLengthLimiter = serializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);

      CompressedUInt32Serializer.Instance.Serialize(serializer, (UInt32)length);
      serializer.Stream.Write(bytes, 0, bytes.Length);
    }

    protected override string DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt32 unsignedLength = CompressedUInt32Serializer.Instance.Deserialize(deserializer);

      // A length 0 means an empty string. Null is not supported by item serializers, being the
      // responsibility of the global serializer (or the array serializer or the like).
      if (unsignedLength == 0)
        return "";

      Int32 length = checked((Int32)unsignedLength);
      var arrayLengthLimiter = deserializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);

      byte[] bytes = new byte[length];
      deserializer.Stream.ReadOrThrow(bytes, 0, length);

      var encoder = Encoding.UTF8;
      var result = encoder.GetString(bytes);
      return result;
    }
  }
}
