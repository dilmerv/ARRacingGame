// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.AR.VideoFormat
{
  [Serializable]
  internal sealed class _SerializableARVideoFormat: 
    IARVideoFormat
  {
    internal _SerializableARVideoFormat
    (
      int framesPerSecond,
      Vector2 imageResolution,
      Vector2 textureResolution
    )
    {
      FramesPerSecond = framesPerSecond;
      ImageResolution = imageResolution;
      TextureResolution = textureResolution;
    }

    public int FramesPerSecond { get; private set; }
    public Vector2 ImageResolution { get; private set; }
    public Vector2 TextureResolution { get; private set; }
  }
}
