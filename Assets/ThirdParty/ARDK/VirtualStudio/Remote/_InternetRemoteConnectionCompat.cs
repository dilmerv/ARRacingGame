// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Extensions;

using UnityEngine.Networking.PlayerConnection;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// Compat layer for <see cref="_InternetRemoteConnection"/>.
  internal sealed class _InternetRemoteConnectionCompat: _IRemoteConnectionCompat
  {
    private readonly Dictionary<Guid, Action<MessageEventArgs>> _callbackLookup =
      new Dictionary<Guid, Action<MessageEventArgs>>();

    private readonly _InternetRemoteConnection _internetRemoteConnection;

    /// <inheritdoc />
    public bool IsReady
    {
      get
      {
        if (_internetRemoteConnection.Status.State != _RemoteConnectionState.Connected)
        {
          return false;
        }

        var localHandle = _internetRemoteConnection.LocalDeviceInfo.localHandle;
        return (_internetRemoteConnection.GetRoleOfDevice(localHandle) != _RemoteDeviceRole.None);
      }
    }

    /// <inheritdoc />
    public bool IsConnected
    {
      get
      {
        if (!IsReady)
        {
          return false;
        }

        var ourRole =
          _internetRemoteConnection.GetRoleOfDevice
            (_internetRemoteConnection.LocalDeviceInfo.localHandle);

        _RemoteDeviceRole destinationRole;
        switch (ourRole)
        {
          case _RemoteDeviceRole.Application:
            destinationRole = _RemoteDeviceRole.Remote;
            break;

          case _RemoteDeviceRole.Remote:
            destinationRole = _RemoteDeviceRole.Application;
            break;

          default:
            return false;
        }

        var destination = _internetRemoteConnection.GetDeviceWithRole(destinationRole);
        return destination != null;
      }
    }

    /// <inheritdoc />
    public string DeviceGroupIdentifier
    {
      get
      {
        return _internetRemoteConnection.DeviceGroupPin;
      }
    }

    /// <inheritdoc />
    public string LocalDeviceIdentifier
    {
      get
      {
        return _internetRemoteConnection.LocalDeviceInfo.localHandle.Identifier.ToString();
      }
    }

    public _InternetRemoteConnectionCompat()
    {
      _internetRemoteConnection = new _InternetRemoteConnection();
      _internetRemoteConnection.ReceivedMessage += (sender, tag, data) =>
      {
        var callbackList = _callbackLookup.GetOrInsert(tag, () => args => {});
        callbackList
        (
          new MessageEventArgs()
          {
            data = data, playerId = tag.GetHashCode(),
          }
        );
      };
    }

    /// <inheritdoc />
    public void Connect(string groupName)
    {
      _internetRemoteConnection.Connect(groupName);
    }

    /// <inheritdoc />
    public void Register(Guid tag, Action<MessageEventArgs> e)
    {
      var callbackList = _callbackLookup.GetOrInsert(tag, () => args => {});

      var newCallbackList = callbackList + e;
      _callbackLookup[tag] = newCallbackList;

      _internetRemoteConnection.RegisterMessage(tag);
    }

    /// <inheritdoc />
    public void Unregister(Guid tag, Action<MessageEventArgs> e)
    {
      var callbackList = _callbackLookup.GetOrInsert(tag, () => args => {});

      var newCallbackList = callbackList - e;
      _callbackLookup[tag] = newCallbackList;
    }

    /// <inheritdoc />
    public void Send(Guid tag, byte[] data, TransportType transportType)
    {
      var ourRole =
        _internetRemoteConnection.GetRoleOfDevice
          (_internetRemoteConnection.LocalDeviceInfo.localHandle);

      _RemoteDeviceRole destinationRole;
      switch (ourRole)
      {
        case _RemoteDeviceRole.Application:
          destinationRole = _RemoteDeviceRole.Remote;
          break;

        case _RemoteDeviceRole.Remote:
          destinationRole = _RemoteDeviceRole.Application;
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }

      var destination = _internetRemoteConnection.GetDeviceWithRole(destinationRole);

      if (destination == null)
      {
        throw new InvalidOperationException("Unknown destination, remote connection not ready.");
      }

      _internetRemoteConnection.SendMessage(tag, transportType, data, destination.Value);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
      _internetRemoteConnection.Disconnect();
    }

    /// <inheritdoc />
    public void Dispose()
    {
      if (_internetRemoteConnection != null)
        _internetRemoteConnection.Dispose();
    }
  }
}
