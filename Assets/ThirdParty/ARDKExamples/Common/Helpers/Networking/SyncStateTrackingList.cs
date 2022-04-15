// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Helpers
{
  public class SyncStateTrackingList:
    MonoBehaviour
  {
    /// Layout box containing the log entries
    [SerializeField]
    private VerticalLayoutGroup _peerTrackers = null;

    /// Prefab for an individual peer tracker
    [SerializeField]
    private SyncStateTrackedPeer _peerTrackerPrefab = null;

    /// Dictionary of peer tracker objects keyed on peer ID
    private readonly Dictionary<IPeer, GameObject> _peerTrackerDict =
      new Dictionary<IPeer, GameObject>();

    private IARNetworking _arNetworking = null;

    private void Awake()
    {
      ARNetworkingFactory.ARNetworkingInitialized += OnAnyInitialized;
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnAnyInitialized;

      var args = new DeinitializedArgs();
      OnWillDeinitialize(args);
    }

    private void OnAnyInitialized(AnyARNetworkingInitializedArgs args)
    {
      // This currently only supports catching the first networking object it sees
      if (_arNetworking != null)
        return;

      _arNetworking = args.ARNetworking;
      _arNetworking.Networking.Connected += OnDidConnect;
      _arNetworking.Networking.PeerAdded += OnDidAddPeer;
      _arNetworking.Networking.PeerRemoved += OnDidRemovePeer;
      _arNetworking.Networking.Disconnected += OnWillDisconnect;
      _arNetworking.Networking.Deinitialized += OnWillDeinitialize;

      // In case ARNetworking was initialized after Networking was already connected,
      // iterate through all already-added peers
      if (_arNetworking.Networking.IsConnected)
      {
        foreach (var peer in _arNetworking.Networking.OtherPeers)
          CreateTracker(peer);
      }
    }

    private void OnWillDeinitialize(DeinitializedArgs args)
    {
      if (_arNetworking == null)
        return;

      _arNetworking.Networking.Connected -= OnDidConnect;
      _arNetworking.Networking.PeerAdded -= OnDidAddPeer;
      _arNetworking.Networking.PeerRemoved -= OnDidRemovePeer;
      _arNetworking.Networking.Disconnected -= OnWillDisconnect;
      _arNetworking.Networking.Deinitialized -= OnWillDeinitialize;
      _arNetworking = null;
    }

    private void OnDidConnect(ConnectedArgs args)
    {
      CreateTracker(args.Self);
    }

    private void OnDidAddPeer(PeerAddedArgs args)
    {
      CreateTracker(args.Peer);
    }

    private void OnDidRemovePeer(PeerRemovedArgs args)
    {
      var trackerObj = _peerTrackerDict[args.Peer];
      _peerTrackerDict.Remove(args.Peer);
      Destroy(trackerObj);
    }

    private void OnWillDisconnect(DisconnectedArgs args)
    {
      ClearTrackers();
    }

    private void CreateTracker(IPeer peer)
    {
      var trackerObj = Instantiate(_peerTrackerPrefab, Vector3.zero, Quaternion.identity);
      trackerObj.SetupToTrackPeer(peer);

      trackerObj.transform.SetParent(_peerTrackers.transform, false);
      _peerTrackerDict.Add(peer, trackerObj.gameObject);
    }

    private void ClearTrackers()
    {
      foreach (var entry in _peerTrackerDict)
        Destroy(entry.Value);

      _peerTrackerDict.Clear();
    }
  }
}
