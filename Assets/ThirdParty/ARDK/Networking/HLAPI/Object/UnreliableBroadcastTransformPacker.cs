// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

using UnityEngine;

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity
{
  [Flags]
  public enum TransformPiece
  {
    Position = 0x1,
    Rotation = 0x2,
    Scale = 0x4,
    PositionAndRotation = Position | Rotation,
    All = Position | Rotation | Scale,
  }

  /// <summary>
  /// Replicator that will replicate transform un-reliably over the network.
  /// </summary>
  public sealed class UnreliableBroadcastTransformPacker:
    NetworkedDataHandlerBase
  {
    private readonly Transform _transform;
    private readonly TransformPiece _pieces;
    private readonly NetworkedDataDescriptor _descriptor;
    private Vector3? _prevPosition;
    private Quaternion? _prevRotation;
    private Vector3? _prevScale;
    private readonly List<IPeer> _peersSentThisFrame = new List<IPeer>();
    private readonly List<IPeer> _dirtyPeers = new List<IPeer>();

    /// <param name="identifier"></param>
    /// <param name="transform">The transform to be replicated.</param>
    /// <param name="descriptor"></param>
    /// <param name="pieces">The pieces of the transform to replicate.</param>
    /// <param name="group"></param>
    public UnreliableBroadcastTransformPacker(
      string identifier,
      Transform transform,
      NetworkedDataDescriptor descriptor,
      TransformPiece pieces,
      INetworkGroup group)
    {
      Identifier = identifier;
      _transform = transform;
      _pieces = pieces;
      _descriptor = descriptor;

      if (group == null)
        return;

      group.RegisterHandler(this);
    }
    
    // TODO: Find where to put this.
    [Serializable]
    internal struct PackedTransform
    {
      internal TransformPiece _dirtyPieces;
      internal Vector3 _position;
      internal Quaternion _rotation;
      internal Vector3 _localScale;
    }

    protected override object GetDataToSend
    (
      ICollection<IPeer> targetPeers,
      ReplicationMode replicationMode
    )
    {
      if (replicationMode.Transport != _descriptor.TransportType)
        return NothingToWrite;

      if (!_descriptor.GetSenders().Contains(Group.Session.Networking.Self))
        return NothingToWrite;

      // Only send to target groups with one peer, so just grab the first peer 
      var firstPeerInTarget = targetPeers.First();
      if (targetPeers.Count != 1 || !_descriptor.GetReceivers().Contains(firstPeerInTarget))
        return NothingToWrite;

      // If we've never sent to this peer before, override the IsInitial flag. This happens 
      //   when we are sending to a peer that just became an observer (but has already joined
      //   the session).
      if (!_dirtyPeers.Contains(firstPeerInTarget))
      {
        _dirtyPeers.Add(firstPeerInTarget);
        replicationMode.IsInitial = true;
      }

      var dirtyPieces = (TransformPiece)0;

      var shouldSendPosition =
        HasFlag(_pieces, TransformPiece.Position) &&
        (_transform.position != _prevPosition || replicationMode.IsInitial);

      if (shouldSendPosition)
        dirtyPieces |= TransformPiece.Position;

      var shouldSendRotation =
        HasFlag(_pieces, TransformPiece.Rotation) &&
        (_transform.rotation != _prevRotation || replicationMode.IsInitial);

      if (shouldSendRotation)
        dirtyPieces |= TransformPiece.Rotation;

      var shouldSendScale =
        HasFlag(_pieces, TransformPiece.Scale) &&
        (_transform.localScale != _prevScale || replicationMode.IsInitial);

      if (shouldSendScale)
        dirtyPieces |= TransformPiece.Scale;

      if (dirtyPieces == 0)
        return NothingToWrite;

      _peersSentThisFrame.Add(targetPeers.First());

      var result = new PackedTransform();
      result._dirtyPieces = dirtyPieces;

      if (HasFlag(dirtyPieces, TransformPiece.Position))
      {
        if (_peersSentThisFrame.Count == _descriptor.GetReceivers().Count())
          _prevPosition = _transform.position;

        result._position = _transform.position;
      }

      if (HasFlag(dirtyPieces, TransformPiece.Rotation))
      {
        if (_peersSentThisFrame.Count == _descriptor.GetReceivers().Count())
          _prevRotation = _transform.rotation;

        result._rotation = _transform.rotation;
      }

      if (HasFlag(dirtyPieces, TransformPiece.Scale))
      {
        if (_peersSentThisFrame.Count == _descriptor.GetReceivers().Count())
          _prevScale = _transform.localScale;

        result._localScale = _transform.localScale;
      }

      if (_peersSentThisFrame.Count == _descriptor.GetReceivers().Count())
        _peersSentThisFrame.Clear();

      return result;
    }

    protected override void HandleReceivedData
    (
      object data,
      IPeer sender,
      ReplicationMode replicationMode
    )
    {
      if (!_descriptor.GetSenders().Contains(sender))
        return;

      var packedTransform = (PackedTransform)data;
      var receivedPieces = packedTransform._dirtyPieces;
      var relevantPieces = receivedPieces & _pieces;

      if (HasFlag(relevantPieces, TransformPiece.Position))
        _transform.position = packedTransform._position;

      if (HasFlag(relevantPieces, TransformPiece.Rotation))
        _transform.rotation = packedTransform._rotation;

      if (HasFlag(relevantPieces, TransformPiece.Scale))
        _transform.localScale = packedTransform._localScale;
    }

    /// <inheritdoc />
    private bool HasFlag(TransformPiece pieces, TransformPiece flag)
    {
      return (pieces & flag) != 0;
    }
  }
}
