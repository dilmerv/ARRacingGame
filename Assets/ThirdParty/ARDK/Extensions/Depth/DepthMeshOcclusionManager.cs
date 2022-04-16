// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;

using Niantic.ARDK.AR;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth.Effects;
using Niantic.ARDK.AR.Depth;
using Niantic.ARDK.AR.Depth.Effects;
using Niantic.ARDK.Internals.EditorUtilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Depth
{
  /// <summary>
  /// This helper can be placed in a scene to easily add occlusions, with minimal setup time. It
  /// reads synchronized depth output from ARFrame, and feeds it into an DepthMeshOcclusionEffect
  /// that then performs the actual shader occlusion. Both precision options of
  /// DepthMeshOcclusionEffect are available, and can be toggled between.
  /// </summary>
  [Obsolete("Use the ARDepthManager's ScreenSpaceMesh instead")]
  public class DepthMeshOcclusionManager:
    UnityLifecycleDriver
  {
  }
}
