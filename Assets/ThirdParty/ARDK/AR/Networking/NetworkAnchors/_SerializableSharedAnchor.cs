// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Networking.NetworkAnchors;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  [Serializable]
  internal sealed class _SerializableSharedAnchor:
    _SerializableARAnchor,
    IARSharedAnchor
  {
    public _SerializableSharedAnchor
    (
      Guid identifier,
      Matrix4x4 transform,
      SharedAnchorTrackingState trackingState,
      bool isPersistent
    ):
      base(transform, identifier)
    {
      TrackingState = trackingState;
      SharedAnchorIdentifier = new SharedAnchorIdentifier(identifier);
      IsShared = isPersistent;
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Shared; }
    }

    public override _SerializableARAnchor Copy()
    {
      return new _SerializableSharedAnchor(Identifier, Transform, TrackingState, IsShared);
    }

    public SharedAnchorTrackingState TrackingState { get; private set; }
    public Matrix4x4 AnchorToLocalTransform { get; private set; }
    public Matrix4x4 AnchorTransform { get; private set; }

    public bool IsShared { get; private set; }

    public SharedAnchorIdentifier SharedAnchorIdentifier { get; private set; }
  }
}
