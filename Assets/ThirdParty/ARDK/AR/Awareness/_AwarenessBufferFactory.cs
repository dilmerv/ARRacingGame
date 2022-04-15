// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;

namespace Niantic.ARDK.AR.Awareness
{
  internal static class _AwarenessBufferFactory
  {
    internal static _SerializableDepthBuffer _AsSerializable(this IDepthBuffer source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableDepthBuffer;
      if (possibleResult != null)
        return possibleResult;

      return
        new _SerializableDepthBuffer
        (
          source.Width,
          source.Height,
          source.IsKeyframe,
          source.ViewMatrix,
          source.Data,
          source.NearDistance,
          source.FarDistance,
          source.Intrinsics
        );
    }

    internal static _SerializableSemanticBuffer _AsSerializable(this ISemanticBuffer source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableSemanticBuffer;
      if (possibleResult != null)
        return possibleResult;

      return
        new _SerializableSemanticBuffer
        (
          source.Width,
          source.Height,
          source.IsKeyframe,
          source.ViewMatrix,
          source.Data,
          source.ChannelNames,
          source.Intrinsics
        );
    }
  }
}
