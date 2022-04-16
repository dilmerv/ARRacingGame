// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  [Serializable]
  internal sealed class _SerializableARBaseAnchor:
    _SerializableARAnchor
  {
    public _SerializableARBaseAnchor
    (
      Matrix4x4 transform,
      Guid identifier
    ):
      base(transform, identifier)
    {
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Base; }
    }

    public override _SerializableARAnchor Copy()
    {
      return new _SerializableARBaseAnchor(Transform, Identifier);
    }
  }
}
