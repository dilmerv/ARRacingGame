// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Helpers
{
  public class SyncStateTrackedPeer:
    MonoBehaviour
  {
    /// Optional. Indicates the current session state with a color.
    [SerializeField]
    private Image _connectedIndicator = null;

    /// Optional. Displays text explaining what the current network state is
    [SerializeField]
    private Text _connectedIndicatorText = null;

    /// Only print of first X digits of peer id onto screen
    [SerializeField]
    private int _peerIdLimit = 6;

    private IPeer _trackedPeer;

    private bool _isSelf;

    private readonly Dictionary<PeerState, Color> _indicatorColors =
      new Dictionary<PeerState, Color>()
      {
        { PeerState.Unknown, Color.white },
        { PeerState.Initializing, Color.yellow },
        { PeerState.WaitingForLocalizationData, Color.cyan },
        { PeerState.Localizing, Color.blue },
        { PeerState.Stabilizing, Color.magenta },
        { PeerState.Stable, Color.green },
        { PeerState.Limited, Color.magenta },
        { PeerState.Failed, Color.red }
      };

    private IARNetworking _arNetworking = null;

    private void Awake()
    {
      ARNetworkingFactory.ARNetworkingInitialized += OnAnyInitialized;
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnAnyInitialized;

      OnDeinitialized(new ARNetworkingDeinitializedArgs());
    }

    private void OnAnyInitialized(AnyARNetworkingInitializedArgs args)
    {
      // This currently only supports catching the first networking object it sees
      if (_arNetworking != null)
        return;

      _arNetworking = args.ARNetworking;
      _arNetworking.Deinitialized += OnDeinitialized;

      if (_trackedPeer != null)
      {
        Debug.LogFormat("SyncStateTrackedPeer listening to updates from {0}", _trackedPeer);
        _arNetworking.PeerStateReceived += OnPeerStateReceived;
      }
    }

    public void SetupToTrackPeer(IPeer peer)
    {
      _trackedPeer = peer;
      var peerID = _trackedPeer.ToString(_peerIdLimit);
      UpdateIndicatorText(peerID + " - UnknownSyncState");

      if (_arNetworking != null)
      {
        Debug.LogFormat("SyncStateTrackedPeer listening to updates from {0}", _trackedPeer);
        _arNetworking.PeerStateReceived += OnPeerStateReceived;
      }
      
      _isSelf = peer.Equals(_arNetworking.Networking.Self);
    }

    private void OnDeinitialized(ARNetworkingDeinitializedArgs args)
    {
      if (_arNetworking == null)
        return;

      _arNetworking.PeerStateReceived -= OnPeerStateReceived;
      _arNetworking.Deinitialized -= OnDeinitialized;
      _arNetworking = null;
    }

    private void OnPeerStateReceived(PeerStateReceivedArgs args)
    {
      // If not tracking a peer, or this isn't the peer we're tracking, ignore.
      if (_trackedPeer == null || !args.Peer.Equals(_trackedPeer))
        return;

      UpdateIndicator(args.State);
    }

    private void UpdateIndicator(PeerState newState)
    {
      var peerID = _trackedPeer.ToString(_peerIdLimit);
      
      if (_isSelf)
        peerID += " (self)";

      UpdateIndicatorText(peerID + " - " + newState.ToString());

      if (_connectedIndicator)
      {
        var color = _indicatorColors[newState];
        _connectedIndicator.color = color;
      }
    }

    private void UpdateIndicatorText(string newText)
    {
      if (_connectedIndicatorText)
        _connectedIndicatorText.text = newText;
    }
  }
}
