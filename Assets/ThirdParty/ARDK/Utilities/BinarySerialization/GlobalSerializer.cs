// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Image;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;
using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;

using SpawnMessage = Niantic.ARDK.Networking.HLAPI.Object.Unity.NetworkSpawner.SpawnMessage;

namespace Niantic.ARDK.Utilities.BinarySerialization
{
  /// <summary>
  /// This static class manages all individual "item serializers" for types. This way, we have
  /// a single point in our application to serialize data, any data, in a binary format.
  /// This global serializer allows item-serializers to be mapped to types that are otherwise
  /// non-serializable (that is, types don't need to have the [Serializable] or implement any
  /// interface to be able to be serialized by using this class/framework).
  /// </summary>
  public static class GlobalSerializer
  {
    // We use locks just on write, as we update two ConcurrentDictionaries together.
    // On reads, the ConcurrentDictionary allows for lock-free access.
    private static readonly object _itemSerializersLock = new object();

    internal static readonly
      ConcurrentDictionary<string, IItemSerializer> _itemSerializersByTypeName =
        new ConcurrentDictionary<string, IItemSerializer>();

    internal struct _SerializerInfo
    {
      internal _SerializerInfo(IItemSerializer serializer, string serializationName)
      {
        _serializer = serializer;
        _serializationName = serializationName;
      }

      internal readonly IItemSerializer _serializer;
      internal readonly string _serializationName;
    }

    internal static readonly ConcurrentDictionary<Type, _SerializerInfo> _itemSerializers =
      new ConcurrentDictionary<Type, _SerializerInfo>(_ReferenceComparer<Type>.Instance);

    /// <summary>
    /// Registers all the "default" serializers supported by this framework.
    /// </summary>
    static GlobalSerializer()
    {
      ARLog._Debug("Initializing static GlobalSerializer.");

      RegisterItemSerializer(BooleanSerializer.Instance, "2", false);
      RegisterItemSerializer(BooleanArraySerializer.Instance, "3", false, false);
      RegisterItemSerializer(ByteSerializer.Instance, "b", false);
      RegisterItemSerializer(ByteArraySerializer.Instance, "b+", false, false);
      RegisterItemSerializer(SByteSerializer.Instance, "-b", false);
      RegisterItemSerializer(ArrayOfUnsealedSerializer<object>.Instance, "o+", false);
      RegisterItemSerializer(CompressedInt64Serializer.Instance, "i64");
      RegisterItemSerializer(CompressedUInt64Serializer.Instance, "ui64");
      RegisterItemSerializer(CompressedInt32Serializer.Instance, "i32");
      RegisterItemSerializer(CompressedUInt32Serializer.Instance, "ui32");
      RegisterItemSerializer(Int16Serializer.Instance, "i16");
      RegisterItemSerializer(UInt16Serializer.Instance, "ui16");
      RegisterItemSerializer(IntPtrSerializer.Instance, "iptr");
      RegisterItemSerializer(FloatSerializer.Instance, "f");
      RegisterItemSerializer(DoubleSerializer.Instance, "d");
      RegisterItemSerializer(GuidSerializer.Instance, "g");
      RegisterItemSerializer(ResolutionSerializer.Instance, "r");
      RegisterItemSerializer(Vector2Serializer.Instance, "v2");
      RegisterItemSerializer(Vector3Serializer.Instance, "v3");
      RegisterItemSerializer(Vector4Serializer.Instance, "v4");
      RegisterItemSerializer(QuaternionSerializer.Instance, "q");
      RegisterItemSerializer(Matrix4x4Serializer.Instance, "m4x4");
      RegisterItemSerializer(CameraIntrinsicsSerializer.Instance, "ci");
      RegisterItemSerializer(StringSerializer.Instance, "s");
      RegisterItemSerializer(ColorSerializer.Instance, "c");
      RegisterItemSerializer(NetworkIdSerializer.Instance, "NId");
      RegisterItemSerializer(NativeArraySerializer<float>.Instance, "naf");
      RegisterItemSerializer(NativeArraySerializer<Int16>.Instance, "nai16");

      RegisterItemSerializer(_SerializableImagePlaneSerializer._instance);
      RegisterItemSerializer(_SerializableImageBufferSerializer._instance);
      RegisterItemSerializer(_SerializableDepthBufferSerializer._instance);
      RegisterItemSerializer(_SerializableSemanticBufferSerializer._instance);

      // We use the same serializer for Native and Serializable configurations, in a way that the
      // application doesn't know if it is native or not. But, to make a Serializable one be
      // deserialized as a native one, we need to register them with the same name, that's why
      // we explicitly specify a name that doesn't include either Native or Serializable.
      RegisterItemSerializer
      (
        _ARWorldTrackingConfigurationSerializer._instance,
        "Niantic.ARDK.AR.Configuration.WorldConfiguration"
      );

      // TODO: Write specific serializer for performance.
      var spawnMessageSerializer = SimpleSerializableSerializer<SpawnMessage>.Instance;
      RegisterItemSerializer(spawnMessageSerializer);


      var networkIdAndDataSerializer =
        SimpleSerializableSerializer<ARDK.Networking.HLAPI.HlapiSession._NetworkIdAndData>.Instance;

      RegisterItemSerializer(networkIdAndDataSerializer);


      var networkGroupDataSerializer =
        SimpleSerializableSerializer<ARDK.Networking.HLAPI.NetworkGroup._NetworkGroupData>.Instance;

      RegisterItemSerializer(networkGroupDataSerializer);


      var packedTransformSerializer =
        SimpleSerializableSerializer<UnreliableBroadcastTransformPacker.PackedTransform>.Instance;

      RegisterItemSerializer(packedTransformSerializer);


      var roleSerializer = EnumSerializer.ForType<Role>();
      RegisterItemSerializer(roleSerializer);

      // We only need to explicitly register those types that we serialize as arrays.
      // Unfortunately, we can't use reflection on iOS to find the array serializers for
      // those. All the other non-arrays for enums and [Serializable] are registered
      // automatically.
      RegisterItemSerializer(SimpleSerializableSerializer<_SerializableARMap>.Instance);
      RegisterItemSerializer(SimpleSerializableSerializer<_SerializableARBaseAnchor>.Instance);
      RegisterItemSerializer(SimpleSerializableSerializer<_SerializableARImageAnchor>.Instance);
      RegisterItemSerializer(SimpleSerializableSerializer<_SerializableARPlaneAnchor>.Instance);

      RegisterItemSerializer(ArraySerializer<IARAnchor>.Instance, null, false, false);

      RegisterItemSerializer
      (
        SimpleSerializableSerializer<ReadOnlyCollection<IARAnchor>>.Instance,
        null,
        false,
        false
      );

      RegisterItemSerializer(ArraySerializer<_SerializableARAnchor>.Instance, null, false, false);

      RegisterItemSerializer
      (
        SimpleSerializableSerializer<ReadOnlyCollection<_SerializableARAnchor>>.Instance,
        null,
        false,
        false
      );

      RegisterItemSerializer(ArraySerializer<IARMap>.Instance, null, false, false);

      RegisterItemSerializer
      (
        SimpleSerializableSerializer<ReadOnlyCollection<IARMap>>.Instance,
        null,
        false,
        false
      );

      // Register all "default serializers" for enums and [Serializable] types.
      var assembly = Assembly.GetExecutingAssembly();
      EnumSerializer.RegisterSerializerForAllEnumsOf(assembly);
      SimpleSerializableSerializer.RegisterSerializerForAllSimpleSerializablesOf(assembly);

      ARLog._Debug("Finished initialization of static GlobalSerializer.");
    }

    /// <summary>
    /// Registers the given item-serializer, optionally allowing to use a different "serialization
    /// name" and to tell if the default array serializer should be used for this type (by default,
    /// the arrays are always registered for any given type).
    /// </summary>
    /// <remarks>
    /// This method is *not* thread-safe and is supposed to be called only during the initialization
    /// of the application, before any serialization or deserialization takes place.
    /// </remarks>
    public static void RegisterItemSerializer<T>
    (
      IItemSerializer<T> itemSerializer,
      string serializationName = null,
      bool registerDefaultArraySerializerOfT = true,
      bool registerDefaultReadOnlyCollectionOfT = true
    )
    {
      if (itemSerializer == null)
        throw new ArgumentNullException(nameof(itemSerializer));

      ARLog._Debug("Registering ItemSerializer<" + typeof(T).FullName + ">.");

      var untypedItemSerializer = itemSerializer as IItemSerializer;
      if (untypedItemSerializer == null)
      {
        string msg = "All item serializers should also implement the non-generic IItemSerializer.";
        throw new ArgumentException(msg, nameof(itemSerializer));
      }

      if (untypedItemSerializer.DataType != typeof(T))
      {
        string msg =
          "The generic argument of the type-serializer must match the type returned by the " +
          "DataType property.";

        throw new ArgumentException(msg, nameof(itemSerializer));
      }

      RegisterUntypedItemSerializer(untypedItemSerializer, serializationName);

      if (registerDefaultArraySerializerOfT || registerDefaultReadOnlyCollectionOfT)
      {
        if (serializationName == null)
          serializationName = typeof(T).FullName;

        // Originally, the call was like this:
        // RegisterItemSerializer<T[]>(ArraySerializer<T>.Instance, serializationName + "+", false);
        // But it was crashing on iOS when trying to run any RegisterItemSerializer.
        // I thought generic methods didn't work with IL2Cpp, but the truth is that the recursivity
        // didn't, even though I am not using the exact same type. Not sure what happens there,
        // but it is clearly a bug of IL2Cpp and only happens at run-time.
        // So, now we invoke the RegisterUntypedItemSerializer and everything works.

        if (registerDefaultArraySerializerOfT)
        {
          var untypedArraySerializer = (IItemSerializer)ArraySerializer<T>.Instance;
          RegisterUntypedItemSerializer(untypedArraySerializer, serializationName + "+");
        }

        if (registerDefaultReadOnlyCollectionOfT)
        {
          var untypedReadOnlyCollectionSerializer =
            (IItemSerializer)SimpleSerializableSerializer<ReadOnlyCollection<T>>.Instance;

          string name = "ROC<" + serializationName + ">";
          RegisterUntypedItemSerializer(untypedReadOnlyCollectionSerializer, name);
        }
      }
    }

    public static void RegisterUntypedItemSerializer
    (
      IItemSerializer untypedItemSerializer,
      string serializationName = null
    )
    {
      if (untypedItemSerializer == null)
        throw new ArgumentNullException(nameof(untypedItemSerializer));

      var dataType = untypedItemSerializer.DataType;
      if (dataType == null)
        throw new ArgumentException("Serializer DataType is null.", nameof(untypedItemSerializer));

      ARLog._Debug("Registering untyped item-serializer for " + dataType.FullName + ".");

      if (dataType.IsAbstract)
        throw new ArgumentException(dataType.FullName + " is abstract.", "dataType");

      if (serializationName == null)
        serializationName = dataType.FullName;

      var info = new _SerializerInfo(untypedItemSerializer, serializationName);
      lock(_itemSerializersLock)
      {
        if (_itemSerializers.ContainsKey(dataType))
        {
          string msg =
            "An item serializer for type " + dataType.FullName + " has already been registered.";

          throw new InvalidOperationException(msg);
        }

        if (_itemSerializersByTypeName.ContainsKey(serializationName))
        {
          string msg =
            "There's an item serializer already registered for a different type that is also " +
            "named: " + serializationName + ".";

          throw new InvalidOperationException(msg);
        }

        _itemSerializers.TryAdd(dataType, info);
        _itemSerializersByTypeName.TryAdd(serializationName, untypedItemSerializer);
      }
    }

    /// <summary>
    /// Gets the item serializer for the given type, or returns null if a serializer for that type
    /// is not found.
    /// </summary>
    public static IItemSerializer TryGetItemSerializer(Type dataType)
    {
      if (dataType == null)
        throw new ArgumentNullException(nameof(dataType));

      _SerializerInfo info;
      if (!_itemSerializers.TryGetValue(dataType, out info))
      {
        ARLog._Debug("Item serializer for " + dataType.FullName + " not found.");
        return null;
      }

      return info._serializer;
    }

    /// <summary>
    /// Gets the item serializer for the generic type T, or returns null if a serializer for that
    /// type is not found.
    /// This method is particularly useful if users don't have direct access to the item-serializer
    /// for the type they plan to serialize, and such type is a struct. By first getting the item
    /// serializer, and using it to serialize the values, serializing the type-information is
    /// skipped, and there's no boxing.
    /// Note that if the type is class/reference type, it might still be preferable to use the
    /// Serialize() method if it is possible the value is null, as item serializers can't deal with
    /// null values.
    /// </summary>
    public static IItemSerializer<T> TryGetItemSerializer<T>()
    {
      _SerializerInfo info;
      if (!_itemSerializers.TryGetValue(typeof(T), out info))
        return null;

      var typedResult = info._serializer as IItemSerializer<T>;
      if (typedResult == null)
      {
        typedResult = new _UntypedToTypedSerializerAdapter<T>(info._serializer);

        // When we get a typed-serializer from an untyped one, we actually replace the existing
        // registration, so now we don't need to reinstantiate an adapter every time.
        info = new _SerializerInfo((IItemSerializer)typedResult, info._serializationName);
        _itemSerializers[typeof(T)] = info;

        ARLog._Debug("Replaced untyped serializer for " + typeof(T).FullName + " by a typed one.");
      }

      return typedResult;
    }

    /// <summary>
    /// Gets the untyped item-serializer for the given run-time type, or throws an
    /// InvalidOperationException if there's no serializer for the given type.
    /// </summary>
    public static IItemSerializer GetItemSerializerOrThrow(Type type)
    {
      var result = TryGetItemSerializer(type);

      if (result == null)
      {
        string msg = "Couldn't get item serializer for " + type.FullName + ".";
        throw new InvalidOperationException(msg);
      }

      return result;
    }

    /// <summary>
    /// Gets the item-serializer for the generic type T, or throws an InvalidOperationException if
    /// there's no serializer for such a type.
    /// </summary>
    public static IItemSerializer<T> GetItemSerializerOrThrow<T>()
    {
      var result = TryGetItemSerializer<T>();

      if (result == null)
      {
        string msg = "Couldn't get item serializer for " + typeof(T).FullName + ".";
        throw new InvalidOperationException(msg);
      }

      return result;
    }

    /// <summary>
    /// Serializes the given object (including null) into the given stream, or throws if a
    /// serializer for the given type of any of its members is not found.
    /// </summary>
    /// <param name="stream">Required. The stream where the data will be serialized.</param>
    /// <param name="item">
    /// Can be null. The item to serialize. null value is a valid item to serialize.
    /// </param>
    public static void Serialize(Stream stream, object item)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof(stream));

      using(var binarySerializer = new BinarySerializer(stream))
        binarySerializer.Serialize(item);
    }

    /// <summary>
    /// Deserializes an object from the given stream.
    /// This method can very easily throw if the stream is corrupted, references an unregistered
    /// type or similar.
    /// </summary>
    public static object Deserialize(Stream stream)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof(stream));

      using(var binaryDeserializer = new BinaryDeserializer(stream))
        return binaryDeserializer.Deserialize();
    }
  }
}