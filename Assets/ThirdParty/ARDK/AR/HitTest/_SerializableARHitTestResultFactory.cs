// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Anchors;

namespace Niantic.ARDK.AR.HitTest
{
  internal static class _SerializableARHitTestResultFactory
  {
    internal static _SerializableARHitTestResult _AsSerializable(this IARHitTestResult source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableARHitTestResult;
      if (possibleResult != null)
        return possibleResult;

      var result =
        new _SerializableARHitTestResult
        (
          source.Type,
          source.Anchor._AsSerializable(),
          source.Distance,
          source.LocalTransform,
          source.WorldTransform,
          source.WorldScale
        );

      return result;
    }
  }
}
