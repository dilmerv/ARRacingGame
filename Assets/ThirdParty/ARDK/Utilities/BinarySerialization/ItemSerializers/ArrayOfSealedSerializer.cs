// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  // Serializer for types that are sealed.
  // This serializer does a single search for the item serializer, and reuses it for all items.
  // This only works for sealed types, as array of non-sealed types might actually have objects of
  // sub-classes, requiring new searches.
  public sealed class ArrayOfSealedSerializer<T>:
    BaseItemSerializer<T[]>
  {
    public static readonly ArrayOfSealedSerializer<T> Instance = new ArrayOfSealedSerializer<T>();

    static ArrayOfSealedSerializer()
    {
      if (!typeof(T).IsSealed)
        throw new InvalidOperationException("This serializer only supports sealed types.");
    }

    private ArrayOfSealedSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, T[] array)
    {
      int length = array.Length;

      var arrayLengthLimiter = serializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);
      CompressedUInt32Serializer.Instance.Serialize(serializer, (UInt32)length);

      if (length == 0)
        return;

      var itemSerializer = GlobalSerializer.GetItemSerializerOrThrow<T>();
      if (typeof(T).IsValueType)
      {
        // Value types can't be null.
        foreach (T item in array)
          itemSerializer.Serialize(serializer, item);
      }
      else
      {
        foreach (T item in array)
        {
          bool hasItem = item != null;
          BooleanSerializer.Instance.Serialize(serializer, hasItem);

          if (hasItem)
            itemSerializer.Serialize(serializer, item);
        }
      }
    }

    protected override T[] DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt32 unsignedLength = CompressedUInt32Serializer.Instance.Deserialize(deserializer);
      if (unsignedLength == 0)
        return EmptyArray<T>.Instance;

      Int32 length = checked((Int32)unsignedLength);
      var arrayLengthLimiter = deserializer.GetContext<ArrayLengthLimiter>();
      arrayLengthLimiter.ReserveOrThrow(length);

      var itemSerializer = GlobalSerializer.GetItemSerializerOrThrow<T>();

      T[] result = new T[length];

      // Value types can't be null, so we avoid such a check.
      if (typeof(T).IsValueType)
      {
        for (int i = 0; i < length; i++)
        {
          T item = itemSerializer.Deserialize(deserializer);
          result[i] = item;
        }
      }
      else
      {
        for (int i = 0; i < length; i++)
        {
          bool hasItem = BooleanSerializer.Instance.Deserialize(deserializer);
          if (hasItem)
          {
            T item = itemSerializer.Deserialize(deserializer);
            result[i] = item;
          }
        }
      }

      return result;
    }
  }
}
