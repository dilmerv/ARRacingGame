// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Anchors
{
  /// <summary>
  /// Values describing possible general orientations of a detected plane with respect to gravity.
  /// </summary>
  /// Limit changing this enum, because it is used in comparisons with the
  /// AR.Configuration.PlaneDetection enum. This rigidness is due to how these enums
  /// are backed in ARKit and ARCore.
  public enum PlaneAlignment
  {
    /// The plane's alignment is unknown
    Unknown = 0,

    /// The plane is perpendicular to gravity.
    Horizontal = 1,

    /// The plane is parallel to gravity.
    Vertical = 2,
  }
}
