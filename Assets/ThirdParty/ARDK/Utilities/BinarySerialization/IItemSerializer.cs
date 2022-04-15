// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

namespace Niantic.ARDK.Utilities.BinarySerialization
{
  /// <summary>
  /// Represents an "untyped" serializer for an item of a specific type, which can be registered
  /// in the GlobalSerializer class.
  /// </summary>
  public interface IItemSerializer
  {
    /// <summary>
    /// Gets the data-type this item serializer is capable of serializing and deserializing.
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// Serializes the given object into the stream of the given "generic purpose" serializer.
    /// To avoid any confusion, this item-serializer only knows how to serialize a single type,
    /// while the serializer given as argument is the one invoking this item serializer, but
    /// might be used if to serialize the current item, items of different types need to be
    /// serialized.
    /// Example: To serialize a Vector3, we also need to serialize 3 floats, the the
    /// BinarySerializer can help the Vector3Serializer to do this.
    /// </summary>
    void Serialize(BinarySerializer serializer, object item);

    /// <summary>
    /// Deserializes an item. The given BinarySerializer is the one calling this item deserializer
    /// and can be used if the current item-serializer needs to deserialize data of other types
    /// before being able to provide the final item.
    /// </summary>
    object Deserialize(BinaryDeserializer deserializer);
  }

  /// <summary>
  /// Represents a typed serializer for items of the generic type T.
  /// All implementations of this interface should also implement the non-generic (untyped)
  /// interface. This interface doesn't depend on the other one directly so users of this interface
  /// will not see the untyped methods (avoiding possible calls to Serialize() giving objects of
  /// the wrong type).
  /// </summary>
  public interface IItemSerializer<T>
  {
    /// <summary>
    /// Serializes the given object into the stream of the given "generic purpose" serializer.
    /// To avoid any confusion, this item-serializer only knows how to serialize a single type,
    /// while the serializer given as argument is the one invoking this item serializer, but
    /// might be used if to serialize the current item, items of different types need to be
    /// serialized.
    /// Example: To serialize a Vector3, we also need to serialize 3 floats, the the
    /// BinarySerializer can help the Vector3Serializer to do this.
    /// </summary>
    void Serialize(BinarySerializer serializer, T item);

     /// <summary>
    /// Deserializes an item. The given BinarySerializer is the one calling this item deserializer
    /// and can be used if the current item-serializer needs to deserialize data of other types
    /// before being able to provide the final item.
    /// </summary>
   T Deserialize(BinaryDeserializer deserializer);
  }
}
