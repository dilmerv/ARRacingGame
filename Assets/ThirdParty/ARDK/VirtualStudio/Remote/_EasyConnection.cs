// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.IO;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// Helper class to use RemoteConnection without having to worry about Guid IDs for registration
  /// or to send messages.
  /// Ideally in the future the entire RemoteConnection API will be simplified to be used exactly
  /// like this class.
  /// </summary>
  /// <remarks>
  /// An _EasyConnection registration only pairs with an _EasyConnection Send().
  /// So, either use _EasyConnection to register and send a type, or use the RemoteConnection
  /// instead.
  /// </remarks>
  internal static class _EasyConnection
  {
    // To avoid using a slow DynamicInvoke on an Action<T>, we use this interface + helper class.
    private interface _IMessageExecutor
    {
      void Execute(object message);
    }

    private sealed class _MessageExecutor<T>:
      _IMessageExecutor
    {
      private readonly Action<T> _action;

      internal _MessageExecutor(Action<T> action)
      {
        _action = action;
      }

      public void Execute(object message)
      {
        T typedMessage = (T)message;
        _action(typedMessage);
      }
    }

    private sealed class _Disposer:
      IDisposable
    {
      private readonly Type _type;
      private readonly _IMessageExecutor _executor;

      internal _Disposer(Type type, _IMessageExecutor executor)
      {
        _type = type;
        _executor = executor;
      }

      // In .NET 5 there is a TryRemove that gets both key and value.
      // For now, we are actually using TryGetValue and then TryRemove with a lock.
      private static readonly object _executorsRemoveLock = new object();

      // Only removes the existing registration if it is the same that was registered.
      public void Dispose()
      {
        // In .NET5 we would just do:
        // _executors.TryRemove(KeyValuePair.Create(_type, _executor), out _);

        lock (_executorsRemoveLock)
        {
          _IMessageExecutor existingExecutor;
          _executors.TryGetValue(_type, out existingExecutor);

          if (_executor == existingExecutor)
            _executors.TryRemove(_type, out _);
        }
      }
    }

    private static readonly Guid _easyConnectionMessageId =
      new Guid("93343898-fcc6-4f77-b5c3-74361c672e77");

    private static readonly ConcurrentDictionary<Type, _IMessageExecutor> _executors =
      new ConcurrentDictionary<Type, _IMessageExecutor>(_ReferenceComparer<Type>.Instance);

    // As this class depends on RemoteConnection, we must call Initialize() after RemoteConnection
    // initialization was done.
    public static void Initialize()
    {
      _RemoteConnection.Register(_easyConnectionMessageId, _MessageReceived);
    }

    private static void _MessageReceived(MessageEventArgs args)
    {
      if (args == null)
        throw new ArgumentNullException(nameof(args));

      var data = args.data;
      if (data == null)
        throw new ArgumentException("args.data is null.", nameof(args));

      object message = data.DeserializeFromArray<object>();
      if (message == null)
        throw new ArgumentException("args had a null message. That shouldn't happen.", nameof(args));

      _InvokeExecutor(message);
    }

    private static void _InvokeExecutor(object message)
    {
      var type = message.GetType();

      _IMessageExecutor executor;
      _executors.TryGetValue(type, out executor);

      if (executor != null)
        executor.Execute(message);
      else
        ARLog._Warn("No executor found for message of type: " + type.FullName + ".");
    }

    // Returns an object that, when disposed, unregisters the actual registration.
    public static IDisposable Register<T>(Action<T> messageExecutor)
    {
      if (!typeof(T).IsSealed)
        throw new InvalidOperationException(typeof(T).FullName + " must be a sealed type.");

      if (messageExecutor == null)
        throw new ArgumentNullException(nameof(messageExecutor));

      var messageExecutorHelper = new _MessageExecutor<T>(messageExecutor);
      if (_executors.TryAdd(typeof(T), messageExecutorHelper))
        return new _Disposer(typeof(T), messageExecutorHelper);

      string message =
        "There's another message executor already registered for:" + typeof(T).FullName;

      throw new ArgumentException(message, nameof(messageExecutor));
    }

    // Unregisters any handler for type T. Usually it is preferred to store the IDisposable
    // object returned from Register and invoke it instead (in that case you only unregister
    // the handler if it is your handler).
    // Returns whether there was a registration for type T (which was unregistered) or not.
    public static bool Unregister<T>()
    {
      return _executors.TryRemove(typeof(T), out _);
    }

    public static void Send(object message, TransportType transportType = TransportType.ReliableUnordered)
    {
      if (message == null)
        throw new ArgumentNullException(nameof(message));

      var type = message.GetType();
      if (!type.IsSealed)
      {
        string errorMessage =
          "message must be from a sealed type, but type " + type.FullName + " is not sealed.";

        throw new ArgumentException(errorMessage, nameof(message));
      }

      var bytes = message.SerializeToArray();
      _RemoteConnection.Send(_easyConnectionMessageId, bytes, transportType);
    }

    internal static class _TestingShim
    {
#pragma warning disable 0162
      internal static void _InvokeMessageReceived(MessageEventArgs eventArgs)
      {
        _MessageReceived(eventArgs);
      }
#pragma warning restore 0162
    }
  }
}
