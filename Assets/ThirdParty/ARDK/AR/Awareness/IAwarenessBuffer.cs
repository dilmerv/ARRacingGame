// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Camera;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  public interface IAwarenessBuffer
  {
    /// View matrix of the ARCamera when this buffer was generated.
    Matrix4x4 ViewMatrix { get; }

    /// Intrinsics values of the image this depth buffer was generate from.
    CameraIntrinsics Intrinsics { get; }

    /// True if this buffer is a keyframe (i.e. not interpolated).
    bool IsKeyframe { get; }

    /// Width of the buffer.
    UInt32 Width { get; }

    /// Height of the buffer.
    UInt32 Height { get; }

    /// Copies the awareness buffer
    /// @returns
    ///   A new typed awareness buffer copied.
    IAwarenessBuffer GetCopy();
  }
}
