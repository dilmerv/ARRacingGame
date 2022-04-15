// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// A static wrapper class for listening for messages from the editor to create a parallel
  /// ARNetworking running on device
  internal static class _RemoteDeviceARNetworkingConstructor
  {
    public static _RemoteDeviceARNetworkingHandler Handler
    {
      get
      {
        return _handler;
      }
    }

    private static _RemoteDeviceARNetworkingHandler _handler;

    private static IDisposable _executor;
    private static ARNetworkingInitMessage _initMessage;

    private static bool _shouldTryToConstruct;

    public static void RegisterForInitMessage()
    {
      _RemoteDeviceARSessionConstructor.HandlerInitialized += OnOtherHandlerInitialized;
      _RemoteDeviceMultipeerNetworkingConstructor.HandlerInitialized += OnOtherHandlerInitialized;

      // Listen for an init...
      _executor = _EasyConnection.Register<ARNetworkingInitMessage>
      (
        initMessage =>
        {
          _initMessage = initMessage;
          _shouldTryToConstruct = true;
          TryToConstruct();
        }
      );
    }

    internal static void _Deinitialize()
    {
      _RemoteDeviceARSessionConstructor.HandlerInitialized -= OnOtherHandlerInitialized;
      _RemoteDeviceMultipeerNetworkingConstructor.HandlerInitialized -= OnOtherHandlerInitialized;

      _initMessage = null;
      _shouldTryToConstruct = false;

      if (_handler != null)
      {
        _handler.Dispose();
        _handler = null;
      }

      if (_executor != null)
      {
        _executor.Dispose();
        _executor = null;
      }
    }

    private static void TryToConstruct()
    {
      if (Handler != null)
      {
        ARLog._Error("A _RemoteDeviceARNetworking instance already exists.");
        return;
      }

      var arSessionDataSender = _RemoteDeviceARSessionConstructor.Handler;
      if (arSessionDataSender == null)
        return;

      if (arSessionDataSender.InnerARSession.StageIdentifier != _initMessage.StageIdentifier)
      {
        var msg = "ARNetworking's stage identifier must match previously constructed ARSession's.";
        ARLog._Error(msg);
        return;
      }

      // Need to set it false here so that a construction of a networking doesn't trigger
      // another call of this method before this call has finished.
      _shouldTryToConstruct = false;

      _RemoteDeviceARNetworkingHandler deviceArNetworkingHandler;
      if (_initMessage.ConstructFromExistingNetworking)
      {
        _RemoteDeviceMultipeerNetworkingHandler networkingHandler;
        _RemoteDeviceMultipeerNetworkingConstructor.CurrentHandlers.TryGetValue
        (
          _initMessage.StageIdentifier,
          out networkingHandler
        );

        if (networkingHandler == null)
        {
          _shouldTryToConstruct = true;
          return;
        }

        deviceArNetworkingHandler =
          new _RemoteDeviceARNetworkingHandler
          (
            arSessionDataSender.InnerARSession,
            networkingHandler.InnerNetworking
          );
      }
      else
      {
        var networking =
          _RemoteDeviceMultipeerNetworkingConstructor.Construct
          (
            _initMessage.ServerConfiguration,
            _initMessage.StageIdentifier
          );

        deviceArNetworkingHandler =
          new _RemoteDeviceARNetworkingHandler
          (
            arSessionDataSender.InnerARSession,
            networking.InnerNetworking
          );
      }

      _handler = deviceArNetworkingHandler;
      deviceArNetworkingHandler.InnerARNetworking.Deinitialized += (_) =>
      {
        _handler.Dispose();
        _handler = null;
      };
    }

    private static void OnOtherHandlerInitialized()
    {
      if (_shouldTryToConstruct)
        TryToConstruct();
    }
  }
}
