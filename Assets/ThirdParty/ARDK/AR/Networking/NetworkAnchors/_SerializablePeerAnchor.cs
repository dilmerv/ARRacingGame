// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Networking.NetworkAnchors;
using Niantic.ARDK.Networking;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  [Serializable]
  internal sealed class _SerializablePeerAnchor:
    _SerializableARAnchor,
    IARPeerAnchor
  {
    public _SerializablePeerAnchor
    (
      Guid identifier,
      Matrix4x4 peerToLocalTransform,
      Matrix4x4 peerPoseTransform,
      PeerAnchorStatus status
    ):
      base(peerToLocalTransform * peerPoseTransform, identifier)
    {
      PeerToLocalTransform = peerToLocalTransform;
      PeerPoseTransform = peerPoseTransform;
      Status = status;
    }

    public IPeer PeerID
    {
      get
      {
        return _Peer.FromIdentifier(Identifier);
      }
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Peer; }
    }

    public override _SerializableARAnchor Copy()
    {
      return new _SerializablePeerAnchor(Identifier, PeerToLocalTransform, PeerPoseTransform, Status);
    }

    public Matrix4x4 PeerToLocalTransform { get; private set; }

    // This is the peer's transform inside their local coordinate space. This, combined with
    // LocalToPeerTransform, is the anchor's Transform.
    public Matrix4x4 PeerPoseTransform { get; private set; }

    // This is the current resolution status of the anchor.
    public PeerAnchorStatus Status { get; private set; }
  }
}
