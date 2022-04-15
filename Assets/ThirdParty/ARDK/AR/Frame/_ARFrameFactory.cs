// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Image;
using Niantic.ARDK.AR.LightEstimate;
using Niantic.ARDK.AR.SLAM;

using UnityEngine;

namespace Niantic.ARDK.AR.Frame
{
  internal static class _ARFrameFactory
  {
    internal static _SerializableARFrame _AsSerializable(this IARFrame source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableARFrame;
      if (possibleResult != null)
        return possibleResult;

      var estimatedDisplayTransform =
        source.CalculateDisplayTransform
        (
          Screen.orientation,
          Screen.width,
          Screen.height
        );

      var result =
        new _SerializableARFrame
        (
          source.CapturedImageBuffer._AsSerializable(70),
          source.Depth._AsSerializable(),
          source.Semantics._AsSerializable(),
          source.Camera._AsSerializable(),
          source.LightEstimate._AsSerializable(),
          source.Anchors,
          source.Maps._AsSerializableArray(),
          source.WorldScale,
          estimatedDisplayTransform
        );

      return result;
    }
  }
}
