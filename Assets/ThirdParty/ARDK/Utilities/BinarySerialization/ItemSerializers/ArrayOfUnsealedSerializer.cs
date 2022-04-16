// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class ArrayOfUnsealedSerializer<T>:
    BaseItemSerializer<T[]>
  {
    public static readonly ArrayOfUnsealedSerializer<T> Instance =
      new ArrayOfUnsealedSerializer<T>();

    private const string _errorMessageUnsealedTypesOnly =
      "This serializer is for unsealed types only. Use the ArrayOfSealedSerializer instead.";
      
    static ArrayOfUnsealedSerializer()
    {
      if (typeof(T).IsSealed)
        throw new Exception(_errorMessageUnsealedTypesOnly);
    }

    private ArrayOfUnsealedSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, T[] array)
    {
      int length = array.Length;

      var arrayLengthLimiter = serializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);
      CompressedUInt32Serializer.Instance.Serialize(serializer, (UInt32)length);

      foreach (T item in array)
        serializer.Serialize(item);
    }

    protected override T[] DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt32 unsignedLength = CompressedUInt32Serializer.Instance.Deserialize(deserializer);
      if (unsignedLength == 0)
        return EmptyArray<T>.Instance;

      Int32 length = checked((Int32)unsignedLength);
      var arrayLengthLimiter = deserializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);

      T[] result = new T[length];
      for (int i = 0; i < length; i++)
      {
        T item = (T)deserializer.Deserialize();
        result[i] = item;
      }
      return result;
    }
  }
}
