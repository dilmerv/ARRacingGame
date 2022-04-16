// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.AR
{
  public interface IARVideoFormat
  {
    /// <summary>
    /// How many frames of video will be processed per second on the native side.
    /// </summary>
    int FramesPerSecond { get; }

    /// <summary>
    /// The resolution [width, height] of the video feed from an AR session.
    /// </summary>
    Vector2 ImageResolution { get; }

    /// <summary>
    /// The resolution [width, height] of the gpu video feed from an AR session.
    /// </summary>
    Vector2 TextureResolution { get; }
  }
}
