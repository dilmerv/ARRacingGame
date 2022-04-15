// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;
#if UNITY_EDITOR
using UnityEditor.Networking.PlayerConnection;
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Serialization;

using Object = UnityEngine.Object;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// TODO(bpeake): Make instantiatable type for testing.
  /// Helper class for handling the remote connection with the player/editor.
  internal static class _RemoteConnection
  {
    public enum ConnectionMethod
    {
      USB,
      Internet,
    }

    private struct BufferedMessage
    {
      public Guid Tag;
      public TransportType TransportType;
      public byte[] Data;
    }

    // Messages that were sent while disconnected still.
    private static readonly List<BufferedMessage> _bufferedMessages = new List<BufferedMessage>();

    private static _IRemoteConnectionCompat _remoteConnectionImpl;

    /// True if the remote connection is connected to another device.
    public static bool IsConnected
    {
      get
      {
        return _remoteConnectionImpl != null && _remoteConnectionImpl.IsConnected;
      }
    }

    /// True if the remote connection is ready to connect to another device.
    public static bool IsReady
    {
      get
      {
        return _remoteConnectionImpl != null && _remoteConnectionImpl.IsReady;
      }
    }

    public static bool IsEnabled
    {
      get
      {
#if UNITY_EDITOR
        var use = PlayerPrefs.GetInt(ARDKUseRemoteProperty, 0);
        return use == 1;
#else
        return true;
#endif
      }
#if UNITY_EDITOR
      set
      {
        if (!Application.isPlaying)
          PlayerPrefs.SetInt(ARDKUseRemoteProperty, value ? 1 : 0);
      }
#endif
    }

    private const string ARDKUseRemoteProperty = "ARDK_Use_Remote";

    /// The secret that was used to connect devices together.
    public static string Secret
    {
      get
      {
        if (_remoteConnectionImpl != null)
          return _remoteConnectionImpl.DeviceGroupIdentifier;

        return "";
      }
    }

    /// The ID associated with the connection.
    public static string ConnectionID
    {
      get
      {
        if (_remoteConnectionImpl != null)
          return _remoteConnectionImpl.LocalDeviceIdentifier;

        return "";
      }
    }

    /// The version that is being used.
    public static ConnectionMethod CurrentConnectionMethod
    {
      get
      {
        return _connectionMethod;
      }
    }

    private static ConnectionMethod _connectionMethod;

    /// <summary>
    /// Initializes the remote connection using the version of remote connection that corresponds
    /// with the given connection method.
    /// </summary>
    /// <param name="connectionMethod">The connection method to use.</param>
    /// <exception cref="ArgumentOutOfRangeException">If an unknown connection method is given.</exception>
    public static void InitIfNone(ConnectionMethod connectionMethod)
    {
      if (_remoteConnectionImpl != null)
      {
        ARLog._Debug("RemoteConnection was already initialized.");
        return;
      }

      Init(connectionMethod);
    }

    private static void Init(ConnectionMethod connectionMethod)
    {
      Screen.orientation = ScreenOrientation.Portrait;
      Screen.autorotateToPortrait = false;
      Screen.autorotateToLandscapeLeft = false;
      Screen.autorotateToLandscapeRight = false;
      Screen.autorotateToPortraitUpsideDown = false;
      Screen.sleepTimeout = SleepTimeout.NeverSleep;

      Application.runInBackground = true;

      Platform.Init();

      if (_remoteConnectionImpl != null)
        _remoteConnectionImpl.Dispose();

      _connectionMethod = connectionMethod;

      switch (connectionMethod)
      {
        case ConnectionMethod.USB:
          _remoteConnectionImpl = new _UsbRemoteConnection();

          // TODO(awang): Because of different message handling flows, this will be called in
          //   different places for different implementations. Unify them somehow?
          _EasyConnection.Initialize();
          break;

        case ConnectionMethod.Internet:
          _remoteConnectionImpl = new _InternetRemoteConnectionCompat();
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }

      _CallbackQueue.ApplicationWillQuit += OnApplicationWillQuit;

      _EasyConnection.Register<RemoteConnectionDestroyMessage>(Deinitialize);

#if !UNITY_EDITOR
      _RemoteDeviceARSessionConstructor.RegisterForInitMessage();
      _RemoteDeviceMultipeerNetworkingConstructor.RegisterForInitMessage();
      _RemoteDeviceARNetworkingConstructor.RegisterForInitMessage();
#endif
    }

    public static void Deinitialize()
    {
      _EasyConnection.Send(new RemoteConnectionDestroyMessage());
      Dispose();
    }

    private static void Deinitialize(RemoteConnectionDestroyMessage message)
    {
      // Dispose before raising error, in case Unity Editor is set to pause on errors
      Dispose();

#if UNITY_EDITOR
      ARLog._Error("Lost connection with the ARDK Remote Feed App.");
#endif
    }

    private static void Dispose()
    {
      var connection = _remoteConnectionImpl;
      if (connection == null)
        return;

      var handler = Deinitialized;
      if (handler != null)
        handler();

      _EasyConnection.Unregister<RemoteConnectionDestroyMessage>();

      _remoteConnectionImpl = null;
      connection.Dispose();

      _RemoteDeviceARSessionConstructor._Deinitialize();
      _RemoteDeviceMultipeerNetworkingConstructor._Deinitialize();
      _RemoteDeviceARNetworkingConstructor._Deinitialize();
    }

    private static void OnApplicationWillQuit()
    {
      _CallbackQueue.ApplicationWillQuit -= OnApplicationWillQuit;
      Deinitialize();
    }

    /// <summary>
    /// Connects the remote connection to a specific pin.
    /// </summary>
    public static void Connect(string pin)
    {
      if (_remoteConnectionImpl != null)
        _remoteConnectionImpl.Connect(pin);
    }

    /// <summary>
    /// Registers a callback to be called whenever data is sent over the specific id.
    /// </summary>
    /// <param name="id">The id of the data.</param>
    /// <param name="e">The event to fire when data of the specific id comes.</param>
    public static void Register(Guid id, Action<MessageEventArgs> e)
    {
      if (_remoteConnectionImpl != null)
        _remoteConnectionImpl.Register(id, e);
    }

    /// <summary>
    /// Unregisters a callback.
    /// </summary>
    /// <param name="id">The id to unregister from.</param>
    /// <param name="e">The callback to unregister.</param>
    public static void Unregister(Guid id, Action<MessageEventArgs> e)
    {
      if (_remoteConnectionImpl != null)
        _remoteConnectionImpl.Unregister(id, e);
    }

    /// <summary>
    /// Sends data over the remote connection.
    /// </summary>
    /// <param name="id">The id to send with the data.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="transportType">The protocol to send the data with.</param>
    public static void Send(Guid id, byte[] data, TransportType transportType = TransportType.ReliableUnordered)
    {
      bool readyAndConnected =
        _remoteConnectionImpl != null &&
        _remoteConnectionImpl.IsReady &&
        _remoteConnectionImpl.IsConnected;

      if (readyAndConnected)
      {
        if (_bufferedMessages.Count != 0)
          TryClearBuffer();

        _remoteConnectionImpl.Send(id, data, transportType);
      }
      else
      {
        var message =
          new BufferedMessage()
          {
            Tag = id,
            TransportType = transportType,
            Data = data,
          };

        _bufferedMessages.Add(message);

        if (_bufferedMessages.Count == 1)
          _CallbackQueue.QueueCallback(_tryClearBufferAction);
      }
    }

    /// <summary>
    /// Sends a message over remote connection.
    /// </summary>
    /// <param name="id">The id of the message.</param>
    /// <param name="value">The value to send.</param>
    /// <param name="transportType">The protocol to send the data with.</param>
    public static void Send<TValue>(Guid id, TValue value, TransportType transportType = TransportType.ReliableUnordered)
    {
      Send(id, value.SerializeToArray(), transportType);
    }

    // Store the delegate once so we don't keep allocating new Action objects per call.
    private static readonly Action _tryClearBufferAction = TryClearBuffer;
    private static void TryClearBuffer()
    {
      bool readyAndConnected =
        _remoteConnectionImpl != null &&
        _remoteConnectionImpl.IsReady &&
        _remoteConnectionImpl.IsConnected;

      if (!readyAndConnected)
      {
        _CallbackQueue.QueueCallback(TryClearBuffer);
        return;
      }

      var messages = _bufferedMessages.ToArray();
      _bufferedMessages.Clear();

      foreach (var bufferedMessage in messages)
        Send(bufferedMessage.Tag, bufferedMessage.Data, bufferedMessage.TransportType);
    }

    public static Action Deinitialized;
  }
}
