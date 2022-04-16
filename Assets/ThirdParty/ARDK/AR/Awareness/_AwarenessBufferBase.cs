// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  internal abstract class _AwarenessBufferBase: IAwarenessBuffer
  {
    internal _AwarenessBufferBase
    (
      uint width,
      uint height,
      bool isKeyframe,
      CameraIntrinsics intrinsics
    )
    {
      IsKeyframe = isKeyframe;
      Width = width;
      Height = height;
    }

    public abstract IAwarenessBuffer GetCopy();

    public bool IsKeyframe { get; private set; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }

    public abstract Matrix4x4 ViewMatrix { get; }
    public abstract CameraIntrinsics Intrinsics { get; }
  }
}
