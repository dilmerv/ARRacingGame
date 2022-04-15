// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Networking.HLAPI.Object
{
  public sealed class NetworkedField<TValue>:
    NetworkedDataHandlerBase,
    INetworkedField<TValue>
  {
    private static readonly EqualityComparer<Optional<TValue>> _equalityComparer =
      EqualityComparer<Optional<TValue>>.Default;

    private readonly NetworkedDataDescriptor _descriptor;
    private readonly HashSet<IPeer> _cleanPeers = new HashSet<IPeer>();
    private Optional<TValue> _value;
    private bool _default;

    public NetworkedField
    (
      string identifier,
      NetworkedDataDescriptor descriptor,
      INetworkGroup group,
      Optional<TValue> defaultValue = default(Optional<TValue>)
    )
    {
      ARLog._DebugFormat
      (
        "Creating a NetworkedField {0} on group {1}",
        false,
        identifier,
        group == null ? "Null" : group.NetworkId.RawId.ToString()
      );
      
      Identifier = identifier;
      _descriptor = descriptor;

      _default = true;

      if (defaultValue.HasValue)
      {
        ARLog._DebugFormat("Setting default value to {0}", false, defaultValue.Value);
        _value = defaultValue;
      }

      if (group != null)
        group.RegisterHandler(this);
    }

    /// <inheritdoc />
    public Optional<TValue> Value
    {
      get
      {
        return _value;
      }
      set
      {
        if (!IsSender(Group.Session.Networking.Self))
          throw new InvalidOperationException("Cannot set value if not sender.");

        if (_equalityComparer.Equals(_value, value))
          return;

        _value = value;
        _default = false;
        _cleanPeers.Clear();

        var valueChanged = ValueChanged;
        var valueChangedIfSender = ValueChangedIfSender;
        if (valueChanged == null && valueChangedIfSender == null)
          return;

        var args =
          new NetworkedFieldValueChangedArgs<TValue>(NetworkedFieldValueChangedMode.Sender, value);

        if (valueChanged != null)
          valueChanged(args);

        if (valueChangedIfSender != null)
          valueChangedIfSender(args);
      }
    }

    /// <inheritdoc />
    public void SetIfSender(TValue newValue)
    {
      var self = GetSelfOrNull();
      if (IsSender(self))
      {
        ARLog._DebugFormat
        (
          "Setting NetworkedField {0} on group {1} to {2}",
          false,
          Identifier,
          Group.NetworkId.RawId,
          newValue
        );
        Value = newValue;
      }
    }

    private bool IsSender(IPeer peer)
    {
      return _descriptor.GetSenders().Contains(peer);
    }

    /// <inheritdoc />
    public event ArdkEventHandler<NetworkedFieldValueChangedArgs<TValue>> ValueChanged;

    /// <inheritdoc />
    public event ArdkEventHandler<NetworkedFieldValueChangedArgs<TValue>> ValueChangedIfSender;

    /// <inheritdoc />
    public event ArdkEventHandler<NetworkedFieldValueChangedArgs<TValue>> ValueChangedIfReceiver;

    protected override object GetDataToSend
    (
      ICollection<IPeer> targetPeers,
      ReplicationMode replicationMode
    )
    {
      var self = GetSelfOrNull();

      // Don't send if not the sender or if the value is still at its default.
      if (!IsSender(self) || _default)
        return NothingToWrite;

      var targetPeerList = targetPeers.ToList();

      // Only send when sending to a specific peer.
      if (targetPeerList.Count > 1)
        return NothingToWrite;

      // Only send on the correct transport type.
      if (replicationMode.Transport != _descriptor.TransportType)
        return NothingToWrite;

      var needsCleanMarking = replicationMode.Transport != TransportType.UnreliableUnordered;

      var peer = targetPeerList.First();

      // Only send if the peer is dirty.
      if (needsCleanMarking && _cleanPeers.Contains(peer))
        return NothingToWrite;

      // only send if the peer is a receiver.
      if (!_descriptor.GetReceivers().Contains(peer))
        return NothingToWrite;
      
      // This cleaning marker should come before the next 2 returns.
      if (needsCleanMarking)
        _cleanPeers.Add(peer);

      if (_value.HasValue)
      {
        ARLog._DebugFormat
        (
          "Propagating NetworkedField {0} on group {1}'s value to peer {2}",
          false,
          Identifier,
          Group.NetworkId.RawId,
          peer
        );
        return _value.Value;
      }

      // Note that null gets serialized as null. NothingToWrite is not serialized.
      return null;
    }

    protected override void HandleReceivedData
    (
      object data,
      IPeer sender,
      ReplicationMode replicationMode
    )
    {
      if (!IsSender(sender))
      {
        ARLog._WarnFormat
        (
          "Received field info from {0} who is not a sender for the field: {1}",
          false,
          sender,
          Identifier
        );

        return;
      }

      Optional<TValue> value = default(Optional<TValue>);
      if (data != null)
        value = (TValue)data;

      _value = value;

      ARLog._DebugFormat
      (
        "NetworkedField {0} on group {1} received value {2} from peer {3}",
        false,
        Identifier,
        Group.NetworkId.RawId,
        value.Value,
        sender
      );

      var valueChanged = ValueChanged;
      var valueChangedIfReceiver = ValueChangedIfReceiver;

      if (valueChanged == null && valueChangedIfReceiver == null)
        return;

      var args =
        new NetworkedFieldValueChangedArgs<TValue>(NetworkedFieldValueChangedMode.Receiver, value);

      if (valueChanged != null)
        valueChanged(args);

      if (valueChangedIfReceiver != null)
        valueChangedIfReceiver(args);
    }
  }
}
