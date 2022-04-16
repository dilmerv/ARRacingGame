// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// A static wrapper class for listening for messages from the editor to create a parallel
  /// _RemoteDeviceMultipeerNetworking running on device
  /// </summary>
  internal static class _RemoteDeviceMultipeerNetworkingConstructor
  {
    public static Action HandlerInitialized;

    public static _ReadOnlyDictionary<Guid, _RemoteDeviceMultipeerNetworkingHandler> CurrentHandlers
    {
      get;
      private set;
    }

    private static readonly
      ConcurrentDictionary<Guid, _RemoteDeviceMultipeerNetworkingHandler> _handlers =
        new ConcurrentDictionary<Guid, _RemoteDeviceMultipeerNetworkingHandler>();

    private static IDisposable _executor;

    static _RemoteDeviceMultipeerNetworkingConstructor()
    {
      CurrentHandlers = new _ReadOnlyDictionary<Guid, _RemoteDeviceMultipeerNetworkingHandler>(_handlers);
    }

    public static void RegisterForInitMessage()
    {
      _executor = _EasyConnection.Register<NetworkingInitMessage>(Construct);
    }

    internal static void _Deinitialize()
    {
      var handlers = new List<_RemoteDeviceMultipeerNetworkingHandler>(_handlers.Values);
      foreach (var handler in handlers)
        handler.Dispose();

      _handlers.Clear();

      var executor = _executor;
      if (executor != null)
      {
        _executor = null;
        executor.Dispose();
      }
    }

    private static void Construct(NetworkingInitMessage message)
    {
      Construct(message.Configuration, message.StageIdentifier);
    }

    public static _RemoteDeviceMultipeerNetworkingHandler Construct
    (
      ServerConfiguration serverConfiguration,
      Guid stageIdentifier
    )
    {
      var handler =
        new _RemoteDeviceMultipeerNetworkingHandler
        (
          serverConfiguration,
          stageIdentifier
        );

      if (!_handlers.TryAdd(handler.InnerNetworking.StageIdentifier, handler))
        throw new InvalidOperationException("Tried to create a networking with a StageIdentifier already in use.");

      handler.InnerNetworking.Deinitialized +=
        (ignored) =>
        {
          _handlers.TryRemove(stageIdentifier, out _);
          handler.Dispose();
        };

      HandlerInitialized?.Invoke();
      return handler;
    }
  }
}
