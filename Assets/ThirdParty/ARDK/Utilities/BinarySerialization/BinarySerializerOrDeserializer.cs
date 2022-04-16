// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.Utilities.BinarySerialization.Contexts;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

namespace Niantic.ARDK.Utilities.BinarySerialization
{
  /// <summary>
  /// Base class for both binary serializers and binary deserializers.
  /// This class is responsible for holding the Stream as well as the "serialization contexts"
  /// that might be needed during serialization.
  /// </summary>
  public abstract class BinarySerializerOrDeserializer:
    IDisposable
  {
    private static readonly object _runningStreamsLock = new object();
    private static readonly HashSet<Stream> _runningStreams =
      new HashSet<Stream>(_ReferenceComparer<Stream>.Instance);

    private readonly Dictionary<Type, ISerializationContext> _contexts =
      new Dictionary<Type, ISerializationContext>(_ReferenceComparer<Type>.Instance);

    const string _duplicatedSerializerErrorMessage =
      "There's another BinarySerializer or BinaryDeserializer for this stream.\n" +
      "Use it instead or dispose it before creating a new one.";

    internal BinarySerializerOrDeserializer(Stream stream)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof(stream));

      lock(_runningStreamsLock)
        if (!_runningStreams.Add(stream))
          throw new InvalidOperationException(_duplicatedSerializerErrorMessage);

      Stream = stream;
    }

    /// <summary>
    /// Releases resources used by this serializer or deserializer.
    /// This does *not* dispose the stream.
    /// </summary>
    public virtual void Dispose()
    {
      var stream = Stream;
      if (stream == null)
        return;

      Stream = null;

      lock(_runningStreamsLock)
        _runningStreams.Remove(stream);
    }

    /// <summary>
    /// Gets the stream used to serialize or deserialize data from this serializer/deserializer.
    /// </summary>
    public Stream Stream { get; private set; }

    /// <summary>
    /// Gets a context of type T for this serialization. Item serializers might want to keep some
    /// context per serialization, like arrays use a maximum length limit, which is controlled by
    ///  the class ArrayLengthLimiter (which is a context class).
    /// </summary>
    public T GetContext<T>()
    where
      T: ISerializationContext, new()
    {
      ISerializationContext context;
      _contexts.TryGetValue(typeof(T), out context);

      if (context == null)
      {
        context = new T();
        _contexts.Add(typeof(T), context);
      }

      return (T)context;
    }
  }
}
