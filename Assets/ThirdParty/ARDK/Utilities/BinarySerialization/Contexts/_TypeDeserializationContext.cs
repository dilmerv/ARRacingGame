// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

namespace Niantic.ARDK.Utilities.BinarySerialization.Contexts
{
  internal sealed class _TypeDeserializationContext
  {
    private readonly List<IItemSerializer> _itemDeserializers = new List<IItemSerializer>();

    internal IItemSerializer _DeserializeTypeAndGetItemDeserializer(
      BinaryDeserializer deserializer,
      UInt32 id)
    {
      // 0 is magic value for null, 1 is magic value for new type. 2+ means already known type.
      if (id == 1)
        return _LoadNewType(deserializer);

      Int32 index = checked((Int32)id) - 2;
      if (index < 0 || index >= _itemDeserializers.Count)
        throw new IOException("Stream contained invalid type index.");

      return _itemDeserializers[index];
    }

    private IItemSerializer _LoadNewType(BinaryDeserializer deserializer)
    {
      string typeName = StringSerializer.Instance.Deserialize(deserializer);

      IItemSerializer result;
      GlobalSerializer._itemSerializersByTypeName.TryGetValue(typeName, out result);

      if (result == null)
      {
        string message =
          "Couldn't find an item deserializer for type " + typeName +
          ", which is in the serialization data.";

        throw new IOException(message);
      }

      _itemDeserializers.Add(result);
      return result;
    }
  }
}
