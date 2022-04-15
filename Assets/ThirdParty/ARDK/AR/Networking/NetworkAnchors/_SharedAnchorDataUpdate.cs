// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  internal struct _SharedAnchorDataUpdate
  {
    internal _SharedAnchorDataUpdate
      (
        SharedAnchorIdentifier identifier,
        SharedAnchorTrackingState state,
        Matrix4x4? anchorToLocalTransform
    )
    {
      Identifier = identifier;
      TrackingState = state;
      AnchorToLocalTransform = anchorToLocalTransform;
    }

    public override string ToString()
    {
      return
        string.Format
        (
          "Identifier: {0}, state: {1}, Transform: {2}",
          Identifier.ToString(),
          TrackingState,
          AnchorToLocalTransform.ToString()
        );
    }

    public readonly SharedAnchorIdentifier Identifier;
    public readonly SharedAnchorTrackingState TrackingState;
    public readonly Matrix4x4? AnchorToLocalTransform;
  }
}
