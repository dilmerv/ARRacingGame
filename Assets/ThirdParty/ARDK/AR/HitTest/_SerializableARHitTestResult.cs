// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.AR.Anchors;
using UnityEngine;

namespace Niantic.ARDK.AR.HitTest
{
  [Serializable]
  internal sealed class _SerializableARHitTestResult:
    IARHitTestResult
  {
    internal _SerializableARHitTestResult
    (
      ARHitTestResultType type,
      _SerializableARAnchor anchor,
      float distance,
      Matrix4x4 localTransform,
      Matrix4x4 worldTransform,
      float worldScale
    )
    {
      Type = type;
      Anchor = anchor;
      Distance = distance;
      LocalTransform = localTransform;
      WorldTransform = worldTransform;
      WorldScale = worldScale;
    }

    public ARHitTestResultType Type { get; private set; }
    public float Distance { get; private set; }
    public Matrix4x4 LocalTransform { get; private set; }
    public Matrix4x4 WorldTransform { get; private set; }
    public float WorldScale { get; private set; }

    internal _SerializableARAnchor Anchor { get; private set; }

    IARAnchor IARHitTestResult.Anchor
    {
      get { return Anchor; }
    }

    void IDisposable.Dispose()
    {
      // Do nothing. This IARHitTestResult implementation is fully managed.
    }
  }
}
