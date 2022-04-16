// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

#if UNITY_EDITOR
using UnityEditor.Networking.PlayerConnection;
using UnityEditor;
#endif

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// Remote connection implementation that goes over USB using the unity profiler utils.
  /// </summary>
  internal sealed class _UsbRemoteConnection: _IRemoteConnectionCompat
  {
    private RemoteMessageHandler _handler;

    /// <inheritdoc />
    public bool IsConnected
    {
      get
      {
#if UNITY_EDITOR
        return EditorConnection.instance.ConnectedPlayers.Count > 0;
#else
        return PlayerConnection.instance.isConnected;
#endif
      }
    }

    /// <inheritdoc />
    public bool IsReady
    {
      get
      {
        return true;
      }
    }

    /// <inheritdoc />
    public string DeviceGroupIdentifier
    {
      get
      {
        return "Unity Player Connection";
      }
    }

    /// <inheritdoc />
    public string LocalDeviceIdentifier
    {
      get
      {
        return SystemInfo.deviceUniqueIdentifier;
      }
    }

    public _UsbRemoteConnection()
    {
      EnsureCreation();
    }

    /// <inheritdoc />
    public void Connect(string groupName)
    {
#if UNITY_EDITOR
      EditorConnection.instance.Initialize();
#endif
    }

    /// <inheritdoc />
    public void Register(Guid tag, Action<MessageEventArgs> e)
    {
      _handler.Register(tag, e);
    }

    /// <inheritdoc />
    public void Unregister(Guid tag, Action<MessageEventArgs> e)
    {
      _handler.Unregister(tag, e);
    }

    /// <inheritdoc />
    public void Send(Guid tag, byte[] data, TransportType transportType)
    {
      _handler.Send(tag, data);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
#if UNITY_EDITOR
      EditorConnection.instance.DisconnectAll();
#else
      PlayerConnection.instance.DisconnectAll();
#endif
    }

    private void EnsureCreation()
    {
      if (_handler != null)
        return;

      var go = new GameObject();
      _handler = go.AddComponent<RemoteMessageHandler>();
    }

    public void Dispose()
    {
      Disconnect();
    }
  }

  /// <summary>
  /// Helper monobehaviour to ensure registeration happens on a unity object.
  /// </summary>
  internal class RemoteMessageHandler: MonoBehaviour
  {
    private static readonly Guid GlobalMessageGuid =
      new Guid("235da759-52e0-4542-adb5-226e1b994328");

    private readonly Dictionary<Guid, HashSet<Action<MessageEventArgs>>> _commandLookup =
      new Dictionary<Guid, HashSet<Action<MessageEventArgs>>>();

    private void Awake()
    {
#if UNITY_EDITOR
      EditorConnection.instance.Register(GlobalMessageGuid, Callback);
#else
      PlayerConnection.instance.Register(GlobalMessageGuid, Callback);
#endif

      hideFlags = HideFlags.HideInHierarchy;
      DontDestroyOnLoad(gameObject);
    }

    public void Register(Guid messageGuid, Action<MessageEventArgs> callback)
    {
      _commandLookup.GetOrInsertNew(messageGuid).Add(callback);
    }

    public void Unregister(Guid messageGuid, Action<MessageEventArgs> callback)
    {
      _commandLookup.GetOrInsertNew(messageGuid).Remove(callback);
    }

    public void Send(Guid id, byte[] data)
    {
      var dataStruct = new Data()
      {
        ID = id.ToByteArray(), Contents = data,
      };
#if UNITY_EDITOR
      EditorConnection.instance.Send(GlobalMessageGuid, dataStruct.SerializeToArray());
#else
      PlayerConnection.instance.Send(GlobalMessageGuid, dataStruct.SerializeToArray());
#endif
    }

    private void Callback(MessageEventArgs e)
    {
      var data = e.data.DeserializeFromArray<Data>();
      var guid = new Guid(data.ID);
      var eventArgs = new MessageEventArgs()
      {
        data = data.Contents, playerId = e.playerId,
      };

      var commands = _commandLookup.GetOrDefault(guid);

      if (commands != null)
      {
        foreach (var command in commands.ToArray())
          command(eventArgs);
      }
      else
      {
        ARLog._WarnFormat("Message {0} has no handlers.", false, guid);
      }
    }

    /// <summary>
    /// Structure to allow forwarding the message under a global id.
    /// </summary>
    [Serializable]
    public class Data
    {
      public byte[] ID;
      public byte[] Contents;
    }
  }
}
