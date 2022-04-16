// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  internal sealed class _NetworkAnchorManager
  {
    // This stores each peer's pose relative to their local coordinate space.
    private readonly Dictionary<IPeer, Matrix4x4> _latestPeerPoses =
      new Dictionary<IPeer, Matrix4x4>();

    private readonly Dictionary<IPeer, IARPeerAnchor> _peerAnchors =
      new Dictionary<IPeer, IARPeerAnchor>();
    private readonly Dictionary<SharedAnchorIdentifier, IARSharedAnchor> _sharedAnchors =
      new Dictionary<SharedAnchorIdentifier, IARSharedAnchor>();

    private readonly HashSet<SharedAnchorIdentifier> _locallyCreatedAnchors = new HashSet<SharedAnchorIdentifier>();

    private readonly _ReadOnlyDictionary<IPeer, Matrix4x4> _readOnlyLatestPeerPoses;
    private readonly _ReadOnlyDictionary<IPeer, IARPeerAnchor> _readOnlyPeerAnchors;
    private readonly _ReadOnlyDictionary<SharedAnchorIdentifier, IARSharedAnchor> _readOnlySharedAnchors;

    private struct PeerPoseData
    {
      internal IPeer peer;
      internal Matrix4x4 pose;
    }

    private readonly object _queuedDataLock = new object();
    private Queue<_SharedAnchorDataUpdate> _sharedDataQueue = new Queue<_SharedAnchorDataUpdate>();
    private Queue<SharedAnchorIdentifier> _uploadedAnchorsQueue = new Queue<SharedAnchorIdentifier>();
    private Queue<PeerAnchorData> _peerDataQueue = new Queue<PeerAnchorData>();
    private Queue<PeerPoseData> _peerPoseQueue = new Queue<PeerPoseData>();

    public _ReadOnlyDictionary<IPeer, Matrix4x4> LatestPeerPoses
    {
      get
      {
        return _readOnlyLatestPeerPoses;
      }
    }

    public _ReadOnlyDictionary<IPeer, IARPeerAnchor> PeerAnchors
    {
      get
      {
        return _readOnlyPeerAnchors;
      }
    }

    public _ReadOnlyDictionary<SharedAnchorIdentifier, IARSharedAnchor> SharedAnchors
    {
      get
      {
        return _readOnlySharedAnchors;
      }
    }

    public ArdkEventHandler<SharedAnchorsArgs> SharedAnchorsUpdated;
    public ArdkEventHandler<SharedAnchorsArgs> SharedAnchorsUploaded;
    public ArdkEventHandler<PeerAnchorUpdatedArgs> PeerAnchorUpdated;

    public _NetworkAnchorManager()
    {
      _readOnlyLatestPeerPoses = new _ReadOnlyDictionary<IPeer, Matrix4x4>(_latestPeerPoses);
      _readOnlyPeerAnchors = new _ReadOnlyDictionary<IPeer, IARPeerAnchor>(_peerAnchors);
      _readOnlySharedAnchors = new _ReadOnlyDictionary<SharedAnchorIdentifier, IARSharedAnchor>(_sharedAnchors);
    }

    public void RemovePeer(IPeer peer)
    {
      _latestPeerPoses.Remove(peer);
      _peerAnchors.Remove(peer);
    }

    public void RemoveAllPeers()
    {
      _latestPeerPoses.Clear();
      _peerAnchors.Clear();
    }

    public void AddSharedAnchor(_SharedAnchorDataUpdate data)
    {
      ARLog._DebugFormat
      (
        "Adding a shared anchor {0}",
        false,
        data.Identifier
      );
      var anchor = new _NativeSharedAnchor(data);
      _sharedAnchors.Add(anchor.SharedAnchorIdentifier, anchor);
      _locallyCreatedAnchors.Add(anchor.SharedAnchorIdentifier);
    }

    // Queue a shared anchor update to be processed during the next update.
    public void QueueSharedAnchor(_SharedAnchorDataUpdate newData)
    {
      ARLog._DebugFormat
      (
        "Queue a shared anchor update {0}",
        true,
        newData.Identifier
      );
      lock (_queuedDataLock)
      {
        _sharedDataQueue.Enqueue(newData);
      }
    }

    public void QueueUploadedAnchor(SharedAnchorIdentifier identifier)
    {
      ARLog._DebugFormat
      (
        "Queue a shared anchor upload {0}",
        true,
        identifier.Guid
      );
      lock (_queuedDataLock)
      {
        _uploadedAnchorsQueue.Enqueue(identifier);
      }
    }

    // Queue a peer anchor update to be processed during the next update.
    public void QueuePeerAnchor(PeerAnchorData newData)
    {
      ARLog._DebugFormat
      (
        "Queue a peer anchor update {0}",
        true,
        newData.Peer.Identifier
      );
      lock (_queuedDataLock)
      {
        _peerDataQueue.Enqueue(newData);
      }
    }

    // Queue a peer pose update to be processed during the next update.
    public void QueuePeerPose(IPeer peer, Matrix4x4 pose)
    {
      ARLog._DebugFormat
      (
        "Queue a peer pose update {0}",
        true,
        peer.Identifier
      );
      lock (_queuedDataLock)
      {
        _peerPoseQueue.Enqueue
        (
          new PeerPoseData
          {
            peer = peer, pose = pose
          }
        );
      }
    }

    // Process all anchor updates that have happened since the last update.
    public void ProcessAllNewData()
    {
      ARLog._Debug("Processing queued network anchor data.", true);
      
      _SharedAnchorDataUpdate[] newSharedData;
      SharedAnchorIdentifier[] newUploadedAnchors;
      PeerAnchorData[] newPeerData;
      PeerPoseData[] newPeerPoses;

      lock (_queuedDataLock)
      {
        newSharedData = _sharedDataQueue.ToArray();
        _sharedDataQueue.Clear();

        newUploadedAnchors = _uploadedAnchorsQueue.ToArray();
        _uploadedAnchorsQueue.Clear();

        newPeerData = _peerDataQueue.ToArray();
        _peerDataQueue.Clear();

        newPeerPoses = _peerPoseQueue.ToArray();
        _peerPoseQueue.Clear();
      }

      var updatedAnchorsForArgs = new Dictionary<SharedAnchorIdentifier, IARSharedAnchor>();
      var uploadedAnchorsForArgs = new Dictionary<SharedAnchorIdentifier, IARSharedAnchor>();

      foreach (var peerPose in newPeerPoses)
        UpdatePeerPose(peerPose.peer, peerPose.pose);

      foreach (var peerData in newPeerData)
        UpdatePeerAnchor(peerData);

      foreach (var sharedData in newSharedData)
        UpdateSharedAnchorTracking(sharedData, updatedAnchorsForArgs);

      foreach (var anchor in newUploadedAnchors)
        UpdateSharedAnchorUploaded(anchor, uploadedAnchorsForArgs);

      var sharedAnchorUpdated = SharedAnchorsUpdated;
      if (sharedAnchorUpdated != null)
        sharedAnchorUpdated(new SharedAnchorsArgs(updatedAnchorsForArgs.Values.ToArray()));

      var sharedAnchorUploaded = SharedAnchorsUploaded;
      if (sharedAnchorUploaded != null)
        sharedAnchorUploaded(new SharedAnchorsArgs(uploadedAnchorsForArgs.Values.ToArray()));

      updatedAnchorsForArgs.Clear();
      uploadedAnchorsForArgs.Clear();
    }

    // Updates a shared anchor based on new data and stores a reference to the updated anchor in the
    // provided dictionary.
    private void UpdateSharedAnchorTracking
    (
      _SharedAnchorDataUpdate newData,
      Dictionary<SharedAnchorIdentifier, IARSharedAnchor> anchorsArg
    )
    {
      IARSharedAnchor anchor = null;
      if (_sharedAnchors.TryGetValue(newData.Identifier, out anchor))
      {
        var nativeAnchor = (_NativeSharedAnchor) anchor;
        nativeAnchor.NewData(newData);

        ARLog._DebugFormat
        (
          "Updating shared anchor {0}",
          true,
          newData.Identifier
        );
      }
      else
      {
        anchor = new _NativeSharedAnchor(newData);
        _sharedAnchors[newData.Identifier] = anchor;

        // Anchors created in this block instead of in AddSharedAnchor
        // must be downloaded anchors, hence they're shared state is true
        ((_NativeSharedAnchor) anchor).SetSharedState(true);
        
        ARLog._DebugFormat
        (
          "Creating a downloaded shared anchor {0}",
          false,
          newData.Identifier
        );
      }

      // If this anchor is deleted, remove it from the set of anchors.
      if (newData.TrackingState == SharedAnchorTrackingState.Deleted)
        _sharedAnchors.Remove(newData.Identifier);

      anchorsArg[anchor.SharedAnchorIdentifier] = anchor;
    }

    private void UpdateSharedAnchorUploaded
    (
      SharedAnchorIdentifier identifier,
      Dictionary<SharedAnchorIdentifier, IARSharedAnchor> anchorsArg
    )
    {
      ARLog._DebugFormat
      (
        "Successfully uploaded shared anchor {0}",
        false,
        identifier.Guid
      );
      
      var anchor = _sharedAnchors[identifier];
      var nativeAnchor = (_NativeSharedAnchor) anchor;

      nativeAnchor.SetSharedState(true);

      anchorsArg[anchor.SharedAnchorIdentifier] = anchor;
    }

    private _NativePeerAnchor GetOrCreatePeerAnchor(IPeer peer)
    {
      IARPeerAnchor foundAnchor = null;
      if (_peerAnchors.TryGetValue(peer, out foundAnchor))
      {
        return (_NativePeerAnchor) foundAnchor;
      }

      ARLog._DebugFormat
      (
        "Creating new peer anchor {0}",
        false,
        peer.Identifier
      );
      _NativePeerAnchor newAnchor = new _NativePeerAnchor(peer);
      Matrix4x4 pose;
      if (_latestPeerPoses.TryGetValue(peer, out pose))
      {
        newAnchor.NewPose(pose);
      }
      _peerAnchors[peer] = newAnchor;
      return newAnchor;
    }

    // Updates the anchor and calls any update callbacks.
    public void UpdatePeerAnchor(PeerAnchorData newData)
    {
      ARLog._DebugFormat
      (
        "Updating peer anchor {0}",
        true,
        newData.Peer.Identifier
      );
      
      _NativePeerAnchor anchor = GetOrCreatePeerAnchor(newData.Peer);
      anchor.NewData(newData);

      if (newData.Status == PeerAnchorStatus.Deleted)
      {
        _peerAnchors.Remove(newData.Peer);
      }

      UpdateSynthesizedPeerAnchor(anchor);
    }

    // Caches the latest pose and updates the anchor. Then it calls the associated event for both
    // the new pose and the updated anchor.
    private void UpdatePeerPose(IPeer peer, Matrix4x4 newPose)
    {
      ARLog._DebugFormat
      (
        "Updating peer anchor pose {0}",
        true,
        peer.Identifier
      );
      
      _latestPeerPoses[peer] = newPose;

      // By default assume we're doing nothing to any anchor.
      IARPeerAnchor anchor = null;
      if (_peerAnchors.TryGetValue(peer, out anchor))
      {
        ARLog._DebugFormat
        (
          "Updating peer anchor pose, anchor found {0}",
          true,
          peer.Identifier
        );
        
        _NativePeerAnchor nativeAnchor = (_NativePeerAnchor)anchor;
        nativeAnchor.NewPose(newPose);

        UpdateSynthesizedPeerAnchor(anchor);
      }
    }

    // This should be called whenever a peer anchor is updated, either the raw graph anchor data or
    // the peer's pose.
    void UpdateSynthesizedPeerAnchor(IARPeerAnchor anchor)
    {
      var peerAnchorUpdated = PeerAnchorUpdated;
      if (peerAnchorUpdated != null)
      {
        var args = new PeerAnchorUpdatedArgs(anchor);
        peerAnchorUpdated(args);
      }
    }
  }
}
