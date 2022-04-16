// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.HitTest
{
  /// Types of objects to hit-test against and also the type of objects found by the hit-test.
  [Flags]
  public enum ARHitTestResultType
  {
    /// Unrecognized hit-test result type.
    None = 0,

    /// A point detected by an AR session representing a distinguishable
    /// feature, but without a corresponding anchor.
    /// @note This is not supported in VirtualStudio's Mock or Remote mode.
    FeaturePoint = 1,

    /// A real-world planar surface detected by the search (without
    /// a corresponding anchor), whose orientation is perpendicular to gravity.
    /// @note This is an Android-only value.
    EstimatedArbitraryPlane = 2,

    /// A real-world planar surface detected by the search (without
    /// a corresponding anchor), whose orientation is perpendicular to gravity.
    /// @note This is not supported in VirtualStudio's Mock or Remote mode.
    EstimatedHorizontalPlane = 4,

    /// A real-world planar surface detected by the search (without
    /// a corresponding anchor), whose orientation is parallel to gravity.
    /// @note This is not supported in VirtualStudio's Mock or Remote mode.
    EstimatedVerticalPlane = 8,

    /// A plane anchor already in the scene, without considering the plane's size.
    /// @note This is not supported in VirtualStudio's Mock or Remote mode.
    ExistingPlane = 16,

    /// A plane anchor already in the scene, respecting the plane's size.
    ExistingPlaneUsingExtent = 32,

    /// A plane anchor already in the scene, respecting the plane's rough, physical geometry.
    /// @note This is not supported in VirtualStudio's Mock or Remote mode.
    ExistingPlaneUsingGeometry = 64,

    /// Any of the types above.
    All = ~None,
  }
}