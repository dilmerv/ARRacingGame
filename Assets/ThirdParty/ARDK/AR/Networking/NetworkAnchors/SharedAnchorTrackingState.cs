// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  /// Values for shared anchor tracking state.
  /// @note This is currently in internal development, and not useable.
  public enum SharedAnchorTrackingState
  {
    /// The anchor is present in the active map layer but the device does not yet know
    /// where the anchor is located in the local coordinate space.
    Unresolved = 0,

    /// The device knows where the anchor is located in the local coordinate space. With this
    /// state, the shared anchor's `Transform` can be used to place virtual content in the scene.
    Tracking = 1,

    /// The anchor is no longer available or being updated on the ARDK servers.
    Deleted = 2,

    /// The anchor is not available in the active map layer
    Failed = 3,
  }
}
