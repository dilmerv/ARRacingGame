// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// A static wrapper class for listening for messages from the editor to create a parallel
  /// ARSession running on device.
  internal static class _RemoteDeviceARSessionConstructor
  {
    public static Action HandlerInitialized;

    public static _RemoteDeviceARSessionHandler Handler
    {
      get
      {
        return _handler;
      }
    }

    private static _RemoteDeviceARSessionHandler _handler;
    private static IDisposable _executor;

    public static void RegisterForInitMessage()
    {
      _executor = _EasyConnection.Register<ARSessionInitMessage>(Construct);
    }

    internal static void _Deinitialize()
    {
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

    private static void Construct(ARSessionInitMessage message)
    {
      Construct
      (
        message.StageIdentifier,
        message.ImageCompressionQuality,
        message.TargetImageFramerate,
        message.TargetBufferFramerate
      );
    }

    public static _RemoteDeviceARSessionHandler Construct
    (
      Guid stageIdentifier,
      int compressionQuality,
      int imageFramerate,
      int awarenessFramerate
    )
    {
      if (Handler != null)
      {
        ARLog._Error("A _RemoteARSessionMessageHandler instance already exists.");
        return null;
      }

      ARLog._DebugFormat
      (
        "Constructing remote ARSession with compressionQuality: {0}, imageFramerate: {1}, awarenessFramerate: {2}",
        false,
        compressionQuality,
        imageFramerate,
        awarenessFramerate
      );

      var session =
        new _RemoteDeviceARSessionHandler
        (
          stageIdentifier,
          compressionQuality,
          imageFramerate,
          awarenessFramerate
        );

      _handler = session;
      session.InnerARSession.Deinitialized += (_) =>
      {
        _handler.Dispose();
        _handler = null;
      };

      HandlerInitialized?.Invoke();
      return session;
    }
  }
}
