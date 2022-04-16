// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Extensions;

using UnityEngine.Networking.PlayerConnection;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// An interface that defines a compatibility layer between all instances of remote connection.
  /// </summary>
  internal interface _IRemoteConnectionCompat:
    IDisposable
  {
    /// <summary>
    /// True if the remote connection is connected to another device.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// True if the remote connection is ready to connect with another device.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// The name of the device group the connection is connected to.
    /// </summary>
    string DeviceGroupIdentifier { get; }

    /// <summary>
    /// The identifier of the local device.
    /// </summary>
    string LocalDeviceIdentifier { get; }

    /// <summary>
    /// Connects to a device group.
    /// </summary>
    /// <param name="groupName">The device group name.</param>
    void Connect(string groupName);

    /// <summary>
    /// Registers a message tag with a callback to invoke when a message of that tag is received.
    /// </summary>
    void Register(Guid tag, Action<MessageEventArgs> e);

    /// <summary>
    /// Unregisters a callback with a tag.
    /// </summary>
    void Unregister(Guid tag, Action<MessageEventArgs> e);

    /// <summary>
    /// Sends a message to the remote device.
    /// </summary>
    /// <param name="tag">The tag to send the message with.</param>
    /// <param name="data">The data to send with the message.</param>
    void Send(Guid tag, byte[] data, TransportType transportType);

    /// <summary>
    /// Disconnects from the remote session.
    /// </summary>
    void Disconnect();
  }
}
