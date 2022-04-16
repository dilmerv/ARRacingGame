// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Networking.NetworkAnchors;
using Niantic.ARDK.Networking;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  /// @note This is currently in internal development, and not useable.
  public interface IARPeerAnchor: IARAnchor
  {
    IPeer PeerID { get; }

    /// <summary>
    /// This is a transform from the peer's coordinate space to the
    /// local Unity coordinate space.
    /// </summary>
    Matrix4x4 PeerToLocalTransform { get; }

    /// <summary>
    /// This is the peer's transform inside their local coordinate space. This, combined with
    /// PeerToLocalTransform, is the anchor's Transform.
    /// </summary>
    Matrix4x4 PeerPoseTransform { get; }

    // This is the current resolution status of the anchor.
    PeerAnchorStatus Status { get; }
  }
}
