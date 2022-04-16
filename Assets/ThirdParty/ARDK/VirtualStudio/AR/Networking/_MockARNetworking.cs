// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Marker;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Networking.Mock
{
  internal sealed class _MockARNetworking:
    IMockARNetworking
  {
    private readonly _IVirtualStudioManager _virtualStudioManager;

    private bool _isInitialized;
    private bool _isDisposed;

    // milliseconds
    private long _poseLatency = 17; // roughly 60 times per second
    private float _timeSinceLastPoseSend;

    private bool _isPoseBroadcastingEnabled = true;

    public _MockARNetworking
    (
      IARSession arSession,
      IMultipeerNetworking networking,
      _IVirtualStudioManager virtualStudioMaster
    )
    {
      ARSession = arSession;
      Networking = networking;
      _virtualStudioManager = virtualStudioMaster;

      _readOnlyLatestPeerPoses = new _ReadOnlyDictionary<IPeer, Matrix4x4>(_latestPeerPoses);
      _readOnlyLatestPeerStates = new _ReadOnlyDictionary<IPeer, PeerState>(_latestPeerStates);

      Networking.PeerAdded += _HandleNetworkingAddedPeer;
      Networking.PeerRemoved += _HandleNetworkingRemovedPeer;
      Networking.Connected += _HandleNetworkingConnected;
      Networking.Disconnected += _HandleNetworkingDisconnected;

      Networking.Deinitialized += (_) => Dispose();
      ARSession.Deinitialized += (_) => Dispose();

      _isInitialized = true;

      ARNetworkingFactory.ARNetworkingInitialized += OnARNetworkingInitialized;
      ARNetworkingFactory.NonLocalARNetworkingInitialized += OnARNetworkingInitialized;
    }

    private void OnARNetworkingInitialized(AnyARNetworkingInitializedArgs args)
    {
      if (args.ARNetworking.ARSession.StageIdentifier == ARSession.StageIdentifier)
      {
        ARNetworkingFactory.ARNetworkingInitialized -= OnARNetworkingInitialized;
        ARNetworkingFactory.NonLocalARNetworkingInitialized -= OnARNetworkingInitialized;
      }
    }

    /// <inheritdoc />
    public void Dispose()
    {
      if (_isDisposed)
        return;

      _isDisposed = true;

      if (_isInitialized)
      {
        var deinitializing = Deinitialized;
        if (deinitializing != null)
        {
          var args = new ARNetworkingDeinitializedArgs();
          deinitializing(args);
        }
      }
    }

    /// <inheritdoc />
    public IMultipeerNetworking Networking { get; private set; }

    /// <inheritdoc />
    public IARSession ARSession { get; private set; }

    /// <inheritdoc />
    public PeerState LocalPeerState { get; private set; }

    private readonly Dictionary<IPeer, Matrix4x4> _latestPeerPoses =
      new Dictionary<IPeer, Matrix4x4>();

    private _ReadOnlyDictionary<IPeer, Matrix4x4> _readOnlyLatestPeerPoses;

    /// <inheritdoc />
    public IReadOnlyDictionary<IPeer, Matrix4x4> LatestPeerPoses
    {
      get { return _readOnlyLatestPeerPoses; }
    }

    private readonly Dictionary<IPeer, PeerState> _latestPeerStates =
      new Dictionary<IPeer, PeerState>();

    private _ReadOnlyDictionary<IPeer, PeerState> _readOnlyLatestPeerStates;

    /// <inheritdoc />
    public IReadOnlyDictionary<IPeer, PeerState> LatestPeerStates
    {
      get { return _readOnlyLatestPeerStates; }
    }

    /// <inheritdoc />
    public void EnablePoseBroadcasting()
    {
      _isPoseBroadcastingEnabled = true;
    }

    /// <inheritdoc />
    public void DisablePoseBroadcasting()
    {
      _isPoseBroadcastingEnabled = false;
    }

    /// <inheritdoc />
    public void SetTargetPoseLatency(long targetPoseLatency)
    {
      _poseLatency = targetPoseLatency;
    }

    private void _HandleNetworkingAddedPeer(PeerAddedArgs args)
    {
      var peer = args.Peer;

      _latestPeerPoses.Add(peer, Matrix4x4.identity);
      _latestPeerStates.Add(peer, PeerState.Unknown);
    }

    private void _HandleNetworkingRemovedPeer(PeerRemovedArgs args)
    {
      var peer = args.Peer;
      if (peer.Equals(Networking.Self))
      {
        LocalPeerState = PeerState.Unknown;
        _latestPeerPoses.Clear();
        _latestPeerStates.Clear();
      }
      else
      {
        _latestPeerPoses.Remove(peer);
        _latestPeerStates.Remove(peer);
      }
    }

    private void _HandleNetworkingConnected(ConnectedArgs args)
    {
      LocalPeerState = PeerState.Unknown;
      _latestPeerStates.Add(args.Self, PeerState.Unknown);

      // Needs to happen on next frame, so that BroadcastState
      // happens after all networking connected callbacks are invoked.
      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (!_isDisposed)
            BroadcastState(PeerState.WaitingForLocalizationData);
        }
      );
    }

    private void _HandleNetworkingDisconnected(DisconnectedArgs args)
    {
      LocalPeerState = PeerState.Unknown;
      _latestPeerPoses.Clear();
    }

    public void BroadcastPose(Matrix4x4 pose, float deltaTime)
    {
      if (!_isPoseBroadcastingEnabled)
        return;

      _timeSinceLastPoseSend += deltaTime * 1000f;
      if (_timeSinceLastPoseSend < _poseLatency)
        return;

      var mediator = _virtualStudioManager.ArNetworkingMediator;
      var receivers = mediator.GetConnectedSessions(Networking.StageIdentifier);

      foreach (var receiver in receivers)
      {
        // Skip broadcasting to self
        if (receiver.Networking.StageIdentifier == Networking.StageIdentifier)
          continue;

        receiver._ReceivePoseFromPeer(pose, Networking.Self);
      }

      _timeSinceLastPoseSend = 0;
    }

    private void _ReceivePoseFromPeer(Matrix4x4 pose, IPeer peer)
    {
      _latestPeerPoses[peer] = pose;

      var peerPoseReceived = PeerPoseReceived;
      if (peerPoseReceived != null)
      {
        var args = new PeerPoseReceivedArgs(peer, pose);
        peerPoseReceived(args);
      }
    }

    public void BroadcastState(PeerState state)
    {
      PeerState currState;
      if (_latestPeerStates.TryGetValue(Networking.Self, out currState) && currState == state)
      {
        ARLog._WarnFormat("Already in state {0}, will not broadcast change.", false, state);
        return;
      }

      var mediator = _virtualStudioManager.ArNetworkingMediator;
      var receivers = mediator.GetConnectedSessions(Networking.StageIdentifier);

      foreach (var receiver in receivers)
      {
        var mockReceiver = receiver;
        if (mockReceiver != null)
          mockReceiver._ReceiveStateFromPeer(state, Networking.Self);
      }
    }

    private void _ReceiveStateFromPeer(PeerState state, IPeer peer)
    {
      _latestPeerStates[peer] = state;

      if (peer.Equals(Networking.Self))
        LocalPeerState = state;

      var peerStateReceived = _peerStateReceived;
      if (peerStateReceived != null)
      {
        var args = new PeerStateReceivedArgs(peer, state);
        peerStateReceived(args);
      }
    }

    private ArdkEventHandler<PeerStateReceivedArgs> _peerStateReceived;

    /// <inheritdoc />
    public event ArdkEventHandler<PeerStateReceivedArgs> PeerStateReceived
    {
      add
      {
        _peerStateReceived += value;

        foreach (var pair in _latestPeerStates)
        {
          if (pair.Value != PeerState.Unknown)
          {
            var args = new PeerStateReceivedArgs(pair.Key, pair.Value);
            value(args);
          }
        }
      }
      remove
      {
        _peerStateReceived -= value;
      }
    }

    /// <inheritdoc />
    public event ArdkEventHandler<PeerPoseReceivedArgs> PeerPoseReceived;

    /// <inheritdoc />
    public event ArdkEventHandler<ARNetworkingDeinitializedArgs> Deinitialized;

    /// <inheritdoc />
    void IARNetworking.InitializeForMarkerScanning(Vector3[] markerPointLocations)
    {
      throw new NotSupportedException();
    }

    /// <inheritdoc />
    void IARNetworking.ScanForMarker
    (
      MarkerScanOption options,
      Action<MarkerMetadata> gotResult,
      IMarkerScanner scanner,
      IMetadataSerializer deserializer
    )
    {
      throw new NotSupportedException();
    }
  }
}
