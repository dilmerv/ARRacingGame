// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.NetworkAnchors;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  internal sealed class _NativePeerAnchor: IARPeerAnchor
  {
    private _Peer _peerID;
    private PeerAnchorData? _data;
    private Matrix4x4? _pose;

    internal _NativePeerAnchor(IPeer peer)
    {
      _peerID = _Peer.FromIdentifier(peer.Identifier);
    }
    
    internal _NativePeerAnchor(PeerAnchorData data, Matrix4x4 pose)
    {
      _peerID = (_Peer)data.Peer;
      _data = data;
      _pose = pose;
    }

    internal void NewData(PeerAnchorData data)
    {
      if (!data.Peer.Equals(_peerID))
      {
        ARLog._ErrorFormat
        (
          "Received data: {0} not equivalent to the cached _peerID: {1}",
          false,
          data.Peer,
          _peerID
        );
      }

      _data = data;
    }

    internal void NewPose(Matrix4x4 pose)
    {
      _pose = pose;
    }

    public void Dispose()
    {
    }

    public IPeer PeerID
    {
      get
      {
        return _peerID;
      }
    }

    public Matrix4x4 PeerToLocalTransform
    {
      get
      {
        if (_data.HasValue)
          return _data.Value.AnchorToLocalTransform;
        
        return Matrix4x4.identity;
      }
    }

    public Matrix4x4 PeerPoseTransform
    {
      get
      {
        if (_pose.HasValue)
          return _pose.Value;
        
        return Matrix4x4.identity;
      }
    }

    public PeerAnchorStatus Status
    {
      get
      {
        // If the anchor is fully created, return the real status.
        if (_pose.HasValue && _data.HasValue)
          return _data.Value.Status;
        // Even if the anchor isn't fully created, if it's known to be deleted consider it deleted.
        else if (_data.HasValue && _data.Value.Status == PeerAnchorStatus.Deleted)
          return PeerAnchorStatus.Deleted;

        // If the anchor isn't fully created it's still unresolved.
        return PeerAnchorStatus.Unresolved;
      }
    }

    public Matrix4x4 Transform
    {
      get
      {
        if (_pose.HasValue && _data.HasValue)
          return _data.Value.AnchorToLocalTransform * _pose.Value;
          
        // If the anchor isn't fully created, just always give it the identity transform.
        return Matrix4x4.identity;
      }
    }

    public Guid Identifier
    {
      get
      {
        return _peerID.Identifier;
      }
    }

    public AnchorType AnchorType {
      get
      {
        return AnchorType.Peer;
      }
    }

    public float WorldScale {
      get
      {
        return 1.0f;
      }
    }
    
    public override string ToString()
    {
      return "ID: " +
        Identifier +
        ", Status: " +
        Status +
        ", Peer to local Transform:\n" +
        PeerToLocalTransform +
        "\nPeer pose:\n" +
        PeerPoseTransform;
    }
  }
}
