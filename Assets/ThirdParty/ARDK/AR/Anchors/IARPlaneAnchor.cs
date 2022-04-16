// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  /// Information about the position and orientation of a real-world flat surface
  /// detected in a world-tracking AR session.
  /// @note When a session is run with plane estimation enabled, the Session will search and
  /// automatically add anchors representing detected surfaces.
  public interface IARPlaneAnchor:
    IARAnchor
  {
    /// <summary>
    /// The general orientation of the detected plane with respect to gravity.
    /// </summary>
    PlaneAlignment Alignment { get; }

    /// <summary>
    /// Possible characterizations of real-world surfaces represented by plane anchors.
    /// @note This is only available on iPhone Xs, Xr, Xs Max, with iOS 12+.
    /// </summary>
    PlaneClassification Classification { get; }

    /// <summary>
    /// Possible states of ARKit's process for classifying plane anchors.
    /// </summary>
    PlaneClassificationStatus ClassificationStatus { get; }

    /// <summary>
    /// The center point of the plane relative to its anchor position.
    /// </summary>
    Vector3 Center { get; }

    /// <summary>
    /// The estimated width and length of the detected plane.
    /// @remark The y-component of this vector will always be zero.
    /// </summary>
    Vector3 Extent { get; }

    /// <summary>
    /// A coarse triangle mesh representing the general shape of the detected plane.
    /// @note **May be null**.
    /// </summary>
    IARPlaneGeometry Geometry { get; }
  }
}
