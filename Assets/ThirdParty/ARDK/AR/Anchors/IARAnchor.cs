// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  /// An anchor is anything in the physical environment that can be tracked by the AR session.
  /// @note
  ///   To track the position and orientation of static physical or virtual objects relative to
  ///   the camera, use the [IARSession.AddAnchor(transform)](@ref ARDK.AR.IARSession) method
  ///   to add them to your AR session.
  ///   \n
  ///   AR sessions can also detect and track [IARPlaneAnchor](@ref ARDK.AR.Anchors.IARPlaneAnchor)
  ///   and [IARImageAnchor](@ref ARDK.AR.Anchors.IARImageAnchor) objects if configured to do so.
  public interface IARAnchor:
    IDisposable
  {
    /// Position, rotation and scale of the anchor in the coordinate space of the AR session
    /// it is being tracked in.
    Matrix4x4 Transform { get; }

    /// A unique identifier representing this anchor.
    Guid Identifier { get; }

    /// The type of this anchor (See AnchorType).
    AnchorType AnchorType { get; }
  }
}
