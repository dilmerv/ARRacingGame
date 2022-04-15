// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  /// <summary>
  /// Class used to implement the untyped IItemSerializer and the typed IItemSerializer&lt;T&gt;
  /// the right way. This class should be used only as a base-class to implement a new serializer
  /// but any method that requires a serializer should ask only for the interfaces, be it the
  /// typed or the untyped version.
  /// </summary>
  public abstract class BaseItemSerializer<T>:
    IItemSerializer<T>, IItemSerializer
  {
    /// <summary>
    /// Implements the IItemSerializer&lt;T&gt;.Serialize() method.
    /// </summary>
    public void Serialize(BinarySerializer serializer, T item)
    {
      if (serializer == null)
        throw new ArgumentNullException(nameof(serializer));

      if (item == null)
        throw new ArgumentNullException(nameof(item));

      DoSerialize(serializer, item);
    }

    private const string _errorMessageGotNull =
      "The ItemSerializer.Deserialize() returned null. This shouldn't happen.";

    /// <summary>
    /// Implements the IItemSerializer&lt;T&gt;.Deserialize() method.
    /// </summary>
    public T Deserialize(BinaryDeserializer deserializer)
    {
      if (deserializer == null)
        throw new ArgumentNullException(nameof(deserializer));

      T result = DoDeserialize(deserializer);
      if (result == null)
        throw new InvalidOperationException(_errorMessageGotNull);

      return result;
    }

    /// <summary>
    /// Method that sub-classes need to override to provide the actual serialization for item.
    /// There's no need to check if either serializer or item are null, as this is done by the
    /// Serialize() method.
    /// </summary>
    protected abstract void DoSerialize(BinarySerializer serializer, T item);

    /// <summary>
    /// Method that sub-classes need to override to provide the actual deserialization logic.
    /// There's no need to check if either deserializer is nuyll as this is done by the
    /// Deserialize() method.
    /// </summary>
    protected abstract T DoDeserialize(BinaryDeserializer deserializer);

    Type IItemSerializer.DataType
    {
      get
      {
        return typeof(T);
      }
    }
    void IItemSerializer.Serialize(BinarySerializer serializer, object item)
    {
      Serialize(serializer, (T)item);
    }
    object IItemSerializer.Deserialize(BinaryDeserializer deserializer)
    {
      return Deserialize(deserializer);
    }
  }
}
