// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Networking.NetworkAnchors;

using UnityEngine;

/// @note This is currently in internal development, and not useable.
namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  /// Shared anchors are the same as base AR anchors, except they are persistent and shareable,
  /// meaning they can be discovered in AR sessions other than the session they are created in.
  /// See [IARNetworking.CreateSharedAnchor(transform, identifier)](@ref ARDK.AR.Networking.IARNetworking)
  /// for how to create and add them to sessions.
  /// @note This is currently in internal development, and not useable.
  public interface IARSharedAnchor: IARAnchor
  {
    /// Unique identifier for this shared anchor. If this anchor is uploaded to the cloud,
    /// it will have the same identifier across all sessions it is discovered in.
    SharedAnchorIdentifier SharedAnchorIdentifier { get; }

    /// Current tracking resolution of the anchor. This anchor's Transform value is not valid
    /// until its TrackingState is TrackingState.Tracking.
    SharedAnchorTrackingState TrackingState { get; }

    /// True if this anchor is available on the cloud
    bool IsShared { get; }
  }
}
