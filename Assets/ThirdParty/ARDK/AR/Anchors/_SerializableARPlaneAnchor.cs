// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.AR.PlaneGeometry;
using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  [Serializable]
  internal sealed class _SerializableARPlaneAnchor:
    _SerializableARAnchor,
    IARPlaneAnchor
  {
    public _SerializableARPlaneAnchor
    (
      Matrix4x4 transform,
      Guid identifier,
      PlaneAlignment alignment,
      PlaneClassification classification,
      PlaneClassificationStatus classificationStatus,
      Vector3 center,
      Vector3 extent
    ):
      base(transform, identifier)
    {
      Alignment = alignment;
      Classification = classification;
      ClassificationStatus = classificationStatus;
      Center = center;
      Extent = extent;
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Plane; }
    }

    public override _SerializableARAnchor Copy()
    {
      return
        new _SerializableARPlaneAnchor
        (
          Transform,
          Identifier,
          Alignment,
          Classification,
          ClassificationStatus,
          Center,
          Extent
        );
    }

    public PlaneAlignment Alignment { get; internal set; }
    public PlaneClassification Classification { get; internal set; }
    public PlaneClassificationStatus ClassificationStatus { get; internal set; }
    public Vector3 Center { get; internal set; }
    public Vector3 Extent { get; internal set; }
    public _SerializableARPlaneGeometry Geometry { get; internal set; }

    IARPlaneGeometry IARPlaneAnchor.Geometry
    {
      get
      {
        return Geometry;
      }
    }
  }
}
