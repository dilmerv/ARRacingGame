// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class EnumSerializer:
    IItemSerializer
  {
    private static readonly ConcurrentDictionary<Type, IItemSerializer> _enumSerializers =
      new ConcurrentDictionary<Type, IItemSerializer>();

    private static readonly Func<Type, IItemSerializer> _createUntypedSerializerFunc =
      (enumType) =>
      {
        if (!enumType.IsEnum)
          throw new ArgumentException(enumType.FullName + " is not an enum type.", nameof(enumType));

        var result = new EnumSerializer(enumType);
        return result;
      };

    public static IItemSerializer ForType(Type enumType)
    {
      if (enumType == null)
        throw new ArgumentNullException(nameof(enumType));

      return _enumSerializers.GetOrAdd(enumType, _createUntypedSerializerFunc);
    }

    // Unfortunately, because of the generic type to untyped conversion, we can't use a concurrent
    // dictionary here.
    private static readonly object _typedSerializersLock = new object();
    private static readonly Dictionary<Type, IItemSerializer> _typedSerializers =
      new Dictionary<Type, IItemSerializer>();

    public static IItemSerializer<T> ForType<T>()
    {
      lock(_typedSerializersLock)
      {
        IItemSerializer uncastSerializer;
        if (_typedSerializers.TryGetValue(typeof(T), out uncastSerializer))
          return (IItemSerializer<T>)uncastSerializer;

        var untypedSerializer = ForType(typeof(T));
        var typedSerializer = new _UntypedToTypedSerializerAdapter<T>(untypedSerializer);
        _typedSerializers.Add(typeof(T), typedSerializer);
        return typedSerializer;
      }
    }

    /// <summary>
    /// Registers an EnumSerializer instance for each enum type found in the given assembly.
    /// </summary>
    public static void RegisterSerializerForAllEnumsOf(Assembly assembly)
    {
      if (assembly == null)
        throw new ArgumentNullException(nameof(assembly));

      foreach(var type in assembly.GetTypes())
      {
        if (type.IsEnum)
        {
          var existingSerializer = GlobalSerializer.TryGetItemSerializer(type);
          if (existingSerializer == null)
          {
            var serializer = ForType(type);
            GlobalSerializer.RegisterUntypedItemSerializer(serializer);
          }
        }
      }
    }

    private readonly Type _enumType;
    private readonly Type _underlyingType;
    private readonly IItemSerializer _underlyingSerializer;
    private EnumSerializer(Type enumType)
    {
      _enumType = enumType;
      _underlyingType = Enum.GetUnderlyingType(_enumType);
      _underlyingSerializer = GlobalSerializer.GetItemSerializerOrThrow(_underlyingType);
    }

    public Type DataType
    {
      get
      {
        return _enumType;
      }
    }

    public void Serialize(BinarySerializer serializer, object item)
    {
      var underlyingValue = Convert.ChangeType(item, _underlyingType);
      _underlyingSerializer.Serialize(serializer, underlyingValue);
    }
    public object Deserialize(BinaryDeserializer deserializer)
    {
      var underlyingValue = _underlyingSerializer.Deserialize(deserializer);
      var result = Enum.ToObject(_enumType, underlyingValue);
      return result;
    }
  }
}
