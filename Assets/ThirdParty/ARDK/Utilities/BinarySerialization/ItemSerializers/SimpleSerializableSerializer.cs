// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  /// <summary>
  /// Serializes/deserializes objects of classes that have the [Serializable] attribute by
  /// serializing/deserializing all their fields. This doesn't work with types that implement
  /// ISerializable (those types aren't "simple serializables").
  /// </summary>
  public sealed class SimpleSerializableSerializer:
    IItemSerializer
  {
    private static readonly ConcurrentDictionary<Type, SimpleSerializableSerializer> _serializers =
      new ConcurrentDictionary<Type, SimpleSerializableSerializer>();

    internal static readonly Comparison<FieldInfo> _fieldComparison =
      (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture);

    private static readonly Func<Type, SimpleSerializableSerializer> _createSerializerFunc =
      (type) => new SimpleSerializableSerializer(type);

    public static SimpleSerializableSerializer ForType(Type type)
    {
      if (type == null)
        throw new ArgumentNullException(nameof(type));

      var result = _serializers.GetOrAdd(type, _createSerializerFunc);
      return result;
    }

    public static void RegisterSerializerForAllSimpleSerializablesOf(Assembly assembly)
    {
      if (assembly == null)
        throw new ArgumentNullException(nameof(assembly));

      foreach(var type in assembly.GetTypes())
      {
        if (!type.IsSerializable)
          continue;

        if (type.IsEnum)
          continue;

        if (type.IsAbstract)
          continue;

        // Types that implement ISerializable aren't "simple" serializable types.
        if (typeof(ISerializable).IsAssignableFrom(type))
          continue;

        // Nothing to do if there's a serializer for that type already registered.
        if (GlobalSerializer.TryGetItemSerializer(type) != null)
          continue;

        var serializer = ForType(type);
        GlobalSerializer.RegisterUntypedItemSerializer(serializer);
      }
    }

    private readonly Type _typeToSerialize;
    private readonly FieldInfo[] _fields;

    private SimpleSerializableSerializer(Type typeToSerialize)
    {
      if (typeToSerialize == null)
        throw new ArgumentNullException(nameof(typeToSerialize));

      if (typeToSerialize.IsEnum)
      {
        var message = "Type " + typeToSerialize.FullName + " is an enum. Use EnumSerializer instead.";
        throw new ArgumentException(message, nameof(typeToSerialize));
      }

      if (!typeToSerialize.IsSerializable)
      {
        string msg = "The type " + typeToSerialize.FullName + " is not [Serializable].";
        throw new InvalidOperationException(msg);
      }

      if (typeof(ISerializable).IsAssignableFrom(typeToSerialize))
      {
        string msg =
          "This serializer is for simple serializable types (that is, types that don't implement " +
          "ISerializable), but type " + typeToSerialize.FullName + " is not a simple " +
          "serializable type.";

        throw new InvalidOperationException(msg);
      }

      // By default we would want to get all the field hierarchy at once, but it actually does
      // something quite odd on iOS... it returns all the fields, but aside from the fields at
      // the latest level, all the fields from base will always return null or default when
      // we try to get their values. So, we get Declared only fields for each level of the hierarchy
      // and this works fine.
      var flags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.DeclaredOnly;

      var allFields = new List<FieldInfo>();

      var currentType = typeToSerialize;
      while (currentType != null)
      {
        var fieldsAtThisLevel = currentType.GetFields(flags);

        // Add all fields that aren't marked as NonSerialized.
        foreach(var field in fieldsAtThisLevel)
          if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
            allFields.Add(field);

        currentType = currentType.BaseType;
      }

      allFields.Sort(SimpleSerializableSerializer._fieldComparison);

      _typeToSerialize = typeToSerialize;
      _fields = allFields.ToArray();
    }

    public Type DataType
    {
      get
      {
        return _typeToSerialize;
      }
    }

    public void Serialize(BinarySerializer serializer, object item)
    {
      if (serializer == null)
        throw new ArgumentNullException(nameof(serializer));

      if (item == null)
        throw new ArgumentNullException(nameof(item));

      var values = FormatterServices.GetObjectData(item, _fields);
      ArraySerializer<object>.Instance.Serialize(serializer, values);
    }

    public object Deserialize(BinaryDeserializer deserializer)
    {
      if (deserializer == null)
        throw new ArgumentNullException(nameof(deserializer));

      var values = ArraySerializer<object>.Instance.Deserialize(deserializer);

      if (_fields.Length != values.Length)
      {
        var message =
          "The number of values to deserialize does not match the number of known fields for " +
          "type " + _typeToSerialize.FullName + ".\n  Fields in the type: " + _fields.Length +
          "\n  Values to deserialize: " + values.Length + ".";

        throw new InvalidOperationException(message);
      }

      object result = FormatterServices.GetSafeUninitializedObject(_typeToSerialize);
      FormatterServices.PopulateObjectMembers(result, _fields, values);
      return result;
    }
  }

  public static class SimpleSerializableSerializer<T>
  {
    public static readonly IItemSerializer<T> Instance =
      new _UntypedToTypedSerializerAdapter<T>(SimpleSerializableSerializer.ForType(typeof(T)));
  }
}
