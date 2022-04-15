// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Configuration
{
  /// <summary>
  /// Options for detecting real-world surfaces with a world-tracking configuration.
  /// </summary>
  /// Limit changing this enum, because it is used in comparisons with the
  /// ARDK.AR.Anchors.PlaneAlignment enum. This rigidness is due to how these enums
  /// are backed in ARKit and ARCore.
  [Flags]
  public enum PlaneDetection
  {
    /// No plane detection.
    None = 0,

    /// Used for detecting planar surfaces perpendicular to gravity.
    Horizontal = 1,

    /// Used for detecting planar surfaces parallel to gravity.
    Vertical = 2,
  }
}
