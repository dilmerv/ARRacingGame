// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  internal sealed class _UntypedToTypedSerializerAdapter<T>:
    BaseItemSerializer<T>
  {
    private readonly IItemSerializer _untypedSerializer;
    internal _UntypedToTypedSerializerAdapter(IItemSerializer untypedSerializer)
    {
      _untypedSerializer = untypedSerializer;
    }

    protected override void DoSerialize(BinarySerializer serializer, T item)
    {
      _untypedSerializer.Serialize(serializer, item);
    }
    protected override T DoDeserialize(BinaryDeserializer deserializer)
    {
      object result = _untypedSerializer.Deserialize(deserializer);
      return (T)result;
    }
  }
}
