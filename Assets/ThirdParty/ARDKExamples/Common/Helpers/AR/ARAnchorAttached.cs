// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDKExamples.Common.Helpers
{
  public class ARAnchorAttachment : MonoBehaviour
  {
    public IARAnchor AttachedAnchor = null;
    public Matrix4x4 Offset = Matrix4x4.identity;
    
    void LateUpdate()
    {
      if (AttachedAnchor != null)
      {
        Matrix4x4 combinedTransform = AttachedAnchor.Transform * Offset;
        transform.position = combinedTransform.ToPosition();
        transform.rotation = combinedTransform.ToRotation();
      }
    }
  }
}
