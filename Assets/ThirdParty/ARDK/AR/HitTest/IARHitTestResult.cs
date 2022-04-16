// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.AR.Anchors;
using UnityEngine;

namespace Niantic.ARDK.AR.HitTest
{
  /// Information about a real-world surface found by examining a point
  /// in the device camera view of an AR session.
  /// @note Use the IARFrame.HitTest method to get an array of ARHitTestResults.
  public interface IARHitTestResult:
    IDisposable
  {
    /// The kind of detected feature this result represents.
    ARHitTestResultType Type { get; }

    /// The anchor representing the detected surface, if any.
    /// @note **May be null**.
    IARAnchor Anchor { get; }

    /// The distance, in meters, from the camera to the hit test result.
    float Distance { get; }

    /// The position and orientation of the hit test result relative to the nearest anchor or
    /// feature point.
    Matrix4x4 LocalTransform { get; }

    /// The position and orientation of the hit test result relative to the world coordinate system.
    Matrix4x4 WorldTransform { get; }

    /// The scaling factor applied to this hitTestResult's data.
    float WorldScale { get; }
  }
}
