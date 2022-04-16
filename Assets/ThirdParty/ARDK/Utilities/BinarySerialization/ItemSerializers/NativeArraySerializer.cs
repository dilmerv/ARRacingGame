// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.Collections;

using Unity.Collections;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class NativeArraySerializer<T>:
    BaseItemSerializer<NativeArray<T>>
  where T: struct
  {
    public static readonly NativeArraySerializer<T> Instance = new NativeArraySerializer<T>();

    private NativeArraySerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, NativeArray<T> item)
    {
      int length = item.Length;

      var arrayLengthLimiter = serializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);
      CompressedUInt32Serializer.Instance.Serialize(serializer, (UInt32)length);

      foreach (T t in item)
        serializer.Serialize(t);
    }
    protected override NativeArray<T> DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt32 unsignedLength = CompressedUInt32Serializer.Instance.Deserialize(deserializer);
      if (unsignedLength == 0)
        return new NativeArray<T>();

      Int32 length = checked((Int32)unsignedLength);
      var arrayLengthLimiter = deserializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);

      var result = new NativeArray<T>((int)unsignedLength, Allocator.Persistent);

      for (int i = 0; i < length; i++)
      {
        T item = (T)deserializer.Deserialize();
        result[i] = item;
      }

      return result;
    }
  }
}
