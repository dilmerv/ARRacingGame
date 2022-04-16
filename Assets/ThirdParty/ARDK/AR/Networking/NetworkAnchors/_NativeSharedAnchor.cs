// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Networking.NetworkAnchors;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  internal sealed class _NativeSharedAnchor:
    IARSharedAnchor
  {
    public _NativeSharedAnchor(_SharedAnchorDataUpdate dataUpdate)
    {
      SharedAnchorIdentifier = dataUpdate.Identifier;
      NewData(dataUpdate);
    }

    public void NewData(_SharedAnchorDataUpdate dataUpdate)
    {
      if (dataUpdate.AnchorToLocalTransform.HasValue)
        Transform = dataUpdate.AnchorToLocalTransform.Value;

      TrackingState = dataUpdate.TrackingState;
    }

    public void SetSharedState(bool isUploaded)
    {
      IsShared = isUploaded;
    }

    public void Dispose() {}

    public Matrix4x4 Transform { get; private set; }

    public SharedAnchorIdentifier SharedAnchorIdentifier { get; private set; }

    public Guid Identifier
    {
      get
      {
        return SharedAnchorIdentifier.Guid;
      }
    }

    public AnchorType AnchorType
    {
      get
      {
        return AnchorType.Shared;
      }
    }

    public float WorldScale
    {
      get
      {
        return 1.0f;
      }
    }

    public SharedAnchorTrackingState TrackingState { get; private set; }

    public bool IsShared { get; private set; }

    public override string ToString()
    {
      return "SharedAnchorIdentifier: " + SharedAnchorIdentifier + ", TrackingState: " + TrackingState + ", Transform:\n" + Transform;
    }
  }
}
