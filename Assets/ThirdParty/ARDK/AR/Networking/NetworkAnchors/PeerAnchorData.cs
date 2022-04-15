// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Diagnostics;

using UnityEngine;

using Niantic.ARDK.Networking;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  /// @note This is currently in internal development, and not useable.
  public struct PeerAnchorData
  {
    public PeerAnchorData(IPeer peer, PeerAnchorStatus status, Matrix4x4 anchorToLocalTransform)
    {
      Peer = peer;
      Status = status;
      AnchorToLocalTransform = anchorToLocalTransform;
    }

    public readonly IPeer Peer;
    public readonly PeerAnchorStatus Status;
    public readonly Matrix4x4 AnchorToLocalTransform;

    public override string ToString()
    {
      return
        "Peer: " + Peer.ToString() +
        ", Status: " + Status +
        ", Transform: " + AnchorToLocalTransform.ToString();
    }
  }
}
