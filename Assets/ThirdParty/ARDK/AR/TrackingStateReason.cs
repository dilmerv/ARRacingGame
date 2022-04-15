// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR
{
  /// @brief Possible causes for limited position tracking quality.
  /// @note iOS-only value.
  public enum TrackingStateReason:
    byte
  {
    /// Current tracking peerState is not limited.
    /// @note This is the only Android-supported value of TrackingStateReason.
    None = 0,

    /// The AR session has not gathered enough camera or motion data to provide
    /// tracking information.
    Initializing = 1,

    /// Tracking is limited due to excessive motion of the device.
    ExcessiveMotion = 2,

    /// Tracking is limited due to insufficient features viewable by the camera.
    InsufficientFeatures = 3,

    /// Tracking is limited due to a relocalization in progress.
    Relocalizing = 4
  }
}
