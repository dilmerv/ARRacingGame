// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.VersionUtilities;

using UnityEngine;

namespace Niantic.ARDKExamples.Helpers
{
  /// A helper class for testing basic network messaging functionality.
  public class SendDebugMessageHelper:
    MonoBehaviour
  {
    // Magic number to denote a debug message type
    private const int TRANSPORT_TAG = 4;

    /// A reference to a multipeer networking instance. The main interaction point for the low-level
    /// multipeer API.
    private IMultipeerNetworking _networking = null;

    private void Awake()
    {
      MultipeerNetworkingFactory.NetworkingInitialized += _NetworkingInitialized;
    }

    private void OnDestroy()
    {
      MultipeerNetworkingFactory.NetworkingInitialized -= _NetworkingInitialized;

      if (_networking != null)
      {
        _networking.Deinitialized -= OnDeinitialized;
        _networking.PeerDataReceived -= OnPeerDataReceived;
        _networking = null;
      }
    }

    private void _NetworkingInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      _networking = args.Networking;
      _networking.Deinitialized += OnDeinitialized;
      _networking.PeerDataReceived += OnPeerDataReceived;
    }

    private void OnDeinitialized(DeinitializedArgs args)
    {
      _networking = null;
    }

    private void OnPeerDataReceived(PeerDataReceivedArgs args)
    {
      if (args.Tag != TRANSPORT_TAG)
        return;

      Debug.LogFormat
      (
        "Got debug data {0} (length: {1}, sender: {2}, type: {3})",
        Encoding.UTF8.GetString(args.CopyData()),
        args.DataLength,
        args.Peer.ToString(6),
        args.TransportType
      );
    }

    /// Sends a random message using all transports types to all connected peers.
    public void Send()
    {
      if (!_networking.IsConnected)
        return;

      var data = new byte[8];
      Buffer.BlockCopy(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()), 0, data, 0, 8);

      foreach (var transportType in Enum.GetValues(typeof(TransportType)))
        _networking.BroadcastData(TRANSPORT_TAG, data, (TransportType)transportType);
    }
  }
}
