// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Configuration
{
  /// @brief Options for how a scene coordinate system is contructed based on real-world device motion.
  /// @note This is an iOS-only enum.
  public enum WorldAlignment
  {
    /// The coordinate system's y-axis is parallel to gravity, and it's origin 
    /// is the initial position of the device.
    Gravity = 0,

    /// The coordinate system's y-axis is parallel to gravity, and the x and z-axes 
    /// are aligned to east and south, respectively. Note, the origin remains the 
    /// initial position of the device.
    GravityAndHeading = 1,

    /// The coordinate system is locked to match the orientation of the device camera.
    Camera = 2,
  }
}
