// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace Niantic.ARDK.AR.Awareness.Depth.Effects
{
  /// This class takes a disparity texture input and generates/manipulates the vertices of a
  /// Unity mesh in order to create an occlusion effect.
  /// It has options for two modes: increased precision when occluded objects are nearer to the
  /// camera, and increased precision when occluded objects are further from the camera.
  [Obsolete("Deprecated, use the DepthMeshOccluder and ARDepthManager instead")]
  public sealed class DepthMeshOcclusionEffect
  {
  }
}
