// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.AR.Networking.NetworkAnchors;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Marker;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking
{
  /// Possible states of local or remote peers in an ARNetworking session.
  public enum PeerState
  {
    /// Initial state. Peers will exit this state after connecting to the server.
    Unknown,

    /// Currently unused. Peers will never reach this state.
    Initializing,

    /// Mapper: Trying to create a map.
    /// Non-Mapper: Waiting to receive localization data.
    WaitingForLocalizationData,

    /// Mapper: Will not reach this state.
    /// Non-Mapper: Has received map data from the server and is attempting to localize.
    Localizing,

    /// State is only reached when using MarkerSync
    /// (i.e. when `IARNetworking.InitializeForMarkerScanning` has been called).
    /// Mapper: Enters this state after exiting Unknown.
    /// Non-Mapper: Has successfully localized through MarkerSync.
    Stabilizing,

    /// Mapper: Has successfully created at least one map.
    /// Non-Mapper: Has successfully localized against a map.
    Stable,

    /// Currently unused. Peers will never reach this state.
    Limited,

    /// Mapper: Will not reach this state.
    /// Non-Mapper: If the mapper has left the ARNetworking session before publishing any maps,
    ///   all non-mapper peers that join afterward will enter this state after exiting Unknown.
    Failed
  }

  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  [Flags]
  public enum MarkerScanOption
  {
    ScanToJoin = 0x1,
    ScanToSync = 0x2,
  }

  public interface IARNetworking:
    IDisposable
  {
    /// A reference to the underlying MultipeerNetworking instance used to communicate with other
    /// peers.
    IMultipeerNetworking Networking { get; }

    /// A reference to the underlying ARSession object generating data used to synchronize with
    /// other peers.
    IARSession ARSession { get; }

    /// Latest poses received from each peer (excluding the local peer).
    /// Peers will be removed from the dictionary when they leave the session.
    IReadOnlyDictionary<IPeer, Matrix4x4> LatestPeerPoses { get; }

    /// Latest PeerStates received from each peer (including the local peer).
    /// Peers will be removed from the dictionary when they leave the session
    IReadOnlyDictionary<IPeer, PeerState> LatestPeerStates { get; }

    /// Current localization state of the local peer.
    PeerState LocalPeerState { get; }

    /// Enable broadcasting of the local peer pose.
    void EnablePoseBroadcasting();

    /// Disable broadcasting of the local peer pose.
    void DisablePoseBroadcasting();

    /// Set target latency (milliseconds) for broadcasting the local peer pose.
    void SetTargetPoseLatency(Int64 targetPoseLatency);

    /// Sets up this ARNetworking session to be a MarkerScanning host. The peer calling
    /// this method must be the host in an existing networking session. Only needs to be called
    /// when using a marker for map syncing.
    /// @note This is part of an experimental feature that is not advised to be used in release builds.
    /// <param name="markerPointLocations">
    /// Positions of the marker points that should be detected by
    /// scanners in real-world space (unit: meters), relative to the center of the device's screen.
    /// </param>
    void InitializeForMarkerScanning(Vector3[] markerPointLocations = null);

    /// Initiates and runs the process to scan for a marker and join a networking session and/or
    ///  sync maps using that marker.
    /// @note This is part of an experimental feature that is not advised to be used in release builds.
    /// <param name="options">
    /// Configure to scan for a marker in order to join a networking session,
    /// to sync maps, or to do both (in last case will first join, then sync when able).
    /// </param>
    /// <param name="gotResult">
    /// Callback raised when a marker is properly scanned and parsed.
    /// If both joining and syncing MarkerScanOption flags are enabled, the callback will
    /// only be raised after the syncing metadata has been parsed. At the time of the callback
    /// it is not guaranteed this ARNetworking has either joined or synced, so still use the
    /// MultipeerNetworking.DidConnect and ARNetworking.DidReceiveStateFromPeer to verify those
    /// statuses.
    /// </param>
    /// <param name="scanner">
    /// If null, will default to using an ARFrameScanner scanning for a ZXing barcode.
    /// </param>
    /// <param name="deserializer">
    /// If null, will default to using BasicMetadataSerializer and
    /// EmbeddedStationaryMetadataSerializer (if MarkerScanOption.ScanToJoin is flagged).
    /// </param>
    void ScanForMarker
    (
      MarkerScanOption options,
      Action<MarkerMetadata> gotResult = null,
      IMarkerScanner scanner = null,
      IMetadataSerializer deserializer = null
    );

    /// Alerts subscribers when any peer's (including the local peer's) localization state has changed.
    event ArdkEventHandler<PeerStateReceivedArgs> PeerStateReceived;

    /// Alerts subscribers when any peer's (excluding the local peer's) pose has changed.
    event ArdkEventHandler<PeerPoseReceivedArgs> PeerPoseReceived;

    /// Alerts subscribers when this object is deinitialized.
    event ArdkEventHandler<ARNetworkingDeinitializedArgs> Deinitialized;
  }
}
