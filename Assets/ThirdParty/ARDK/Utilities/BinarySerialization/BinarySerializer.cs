// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Utilities.BinarySerialization
{
  /// <summary>
  /// Class used to serialize objects in binary-format.
  /// </summary>
  public sealed class BinarySerializer:
    BinarySerializerOrDeserializer
  {
    private readonly _TypeSerializationContext _context = new _TypeSerializationContext();

    /// <summary>
    /// Creates a new binary-serializer, which will serialize data to the given stream.
    /// </summary>
    public BinarySerializer(Stream stream):
      base(stream)
    {
      ARLog._Debug("Creating a BinarySerializer.");
    }

    /// <summary>
    /// Releases the resources of this serializer and flushes the stream.
    /// Disposing of the stream is not done, on purpose, as we might want to use
    /// different serializers to send each message over a NetworkStream or similar.
    /// </summary>
    public override void Dispose()
    {
      var stream = Stream;
      base.Dispose();

      if (stream != null)
        stream.Flush();

      ARLog._Debug("Disposed of a BinarySerializer.");
    }

    /// <summary>
    /// Serializes the given item (including null) to the Stream this BinarySerializer is bound to.
    /// If an item-serializer for the given type is not found, an exception is thrown.
    /// </summary>
    public void Serialize(object item)
    {
      if (item == null)
      {
        CompressedUInt32Serializer.Instance.Serialize(this, 0);
        return;
      }

      var itemType = item.GetType();

      GlobalSerializer._SerializerInfo info;
      if (!GlobalSerializer._itemSerializers.TryGetValue(itemType, out info))
      {
        string message = "There's no item serializer for type " + itemType.FullName + ".";
        throw new InvalidOperationException(message);
      }

      _context._SerializeType(this, itemType, info._serializationName);
      info._serializer.Serialize(this, item);
    }
  }
}
