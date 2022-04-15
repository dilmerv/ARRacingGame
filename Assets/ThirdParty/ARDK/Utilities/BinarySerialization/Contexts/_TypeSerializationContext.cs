// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

namespace Niantic.ARDK.Utilities.BinarySerialization.Contexts
{
  internal sealed class _TypeSerializationContext
  {
    private readonly Dictionary<Type, UInt32> _itemSerializers =
      new Dictionary<Type, UInt32>(_ReferenceComparer<Type>.Instance);

    internal void _SerializeType(BinarySerializer serializer, Type type, string serializationName)
    {
      UInt32 id;
      if (_itemSerializers.TryGetValue(type, out id))
      {
        CompressedUInt32Serializer.Instance.Serialize(serializer, id);
        return;
      }

      // Magic numbers: 0 = null. 1 = sending type info for the first time. 2+ =  existing types.
      _itemSerializers.Add(type, (UInt32)(_itemSerializers.Count + 2));
      CompressedUInt32Serializer.Instance.Serialize(serializer, 1);
      StringSerializer.Instance.Serialize(serializer, serializationName);
    }
  }
}
