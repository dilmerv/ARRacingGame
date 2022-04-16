// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.AR.ReferenceImage;
using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  [Serializable]
  internal sealed class _SerializableARImageAnchor:
    _SerializableARAnchor,
    IARImageAnchor
  {
    public _SerializableARImageAnchor
    (
      Matrix4x4 transform,
      Guid identifier,
      _SerializableARReferenceImage referenceImage
    ):
      base(transform, identifier)
    {
      ReferenceImage = referenceImage;
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Image; }
    }

    public override _SerializableARAnchor Copy()
    {
      return new _SerializableARImageAnchor(Transform, Identifier, ReferenceImage);
    }

    internal _SerializableARReferenceImage ReferenceImage { get; private set; }

    IARReferenceImage IARImageAnchor.ReferenceImage
    {
      get { return ReferenceImage; }
    }
  }
}
