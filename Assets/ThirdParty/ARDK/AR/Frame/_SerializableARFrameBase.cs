// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Linq;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.Image;
using Niantic.ARDK.AR.LightEstimate;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.VirtualStudio.Remote;

using UnityEngine;

namespace Niantic.ARDK.AR.Frame
{
  [Serializable]
  internal abstract class _SerializableARFrameBase:
    _ARFrameBase,
    IARFrame
  {
    protected _SerializableARFrameBase()
    {
    }

    protected _SerializableARFrameBase
    (
      _SerializableImageBuffer capturedImageBuffer,
      _SerializableDepthBuffer depthBuffer,
      _SerializableSemanticBuffer buffer,
      _SerializableARCamera camera,
      _SerializableARLightEstimate lightEstimate,
      ReadOnlyCollection<IARAnchor> anchors, // Even native ARAnchors are directly serializable.
      _SerializableARMap[] maps,
      float worldScale,
      Matrix4x4 estimatedDisplayTransform
    )
    {
      CapturedImageBuffer = capturedImageBuffer;
      DepthBuffer = depthBuffer;
      SemanticBuffer = buffer;
      Camera = camera;
      LightEstimate = lightEstimate;
      WorldScale = worldScale;

      Anchors = anchors;
      Maps = maps.AsNonNullReadOnly<IARMap>();

      _estimatedDisplayTransform = estimatedDisplayTransform;
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
      ReleaseImageAndTextures();
    }

    public ARFrameDisposalPolicy? DisposalPolicy { get; set; }
    public _SerializableImageBuffer CapturedImageBuffer { get; set; }
    public _SerializableDepthBuffer DepthBuffer { get; set; }
    public _SerializableSemanticBuffer SemanticBuffer { get; set; }
    public _SerializableARCamera Camera { get; set; }
    public _SerializableARLightEstimate LightEstimate { get; set; }
    public ReadOnlyCollection<IARAnchor> Anchors { get; set; }
    public ReadOnlyCollection<IARMap> Maps { get; set; }
    public float WorldScale { get; set; }

    public IntPtr[] CapturedImageTextures
    {
      get { return EmptyArray<IntPtr>.Instance; }
    }

    public IARPointCloud RawFeaturePoints
    {
      get { return null; }
    }

    public abstract ReadOnlyCollection<IARHitTestResult> HitTest
    (
      int viewportWidth,
      int viewportHeight,
      Vector2 screenPoint,
      ARHitTestResultType types
    );

    private readonly Matrix4x4 _estimatedDisplayTransform;
    public Matrix4x4 CalculateDisplayTransform
    (
      ScreenOrientation orientation,
      int viewportWidth,
      int viewportHeight
    )
    {
      return _estimatedDisplayTransform;
    }

    public void ReleaseImageAndTextures()
    {
      var capturedBuffer = CapturedImageBuffer;
      if (capturedBuffer != null)
      {
        CapturedImageBuffer = null;
        capturedBuffer.Dispose();
      }

      var depthBuffer = DepthBuffer;
      if (depthBuffer != null)
      {
        DepthBuffer = null;
        depthBuffer.Dispose();
      }

      var semanticBuffer = SemanticBuffer;
      if (semanticBuffer != null)
      {
        SemanticBuffer = null;
        semanticBuffer.Dispose();
      }
    }

    public IARFrame Serialize
    (
      bool includeImageBuffers = true,
      bool includeAwarenessBuffers = true,
      int compressionLevel = 70
    )
    {
      return _Serialize(this, includeImageBuffers, includeAwarenessBuffers, compressionLevel);
    }

    IImageBuffer IARFrame.CapturedImageBuffer
    {
      get { return CapturedImageBuffer; }
    }
    IDepthBuffer IARFrame.Depth
    {
      get { return DepthBuffer; }
    }

    ISemanticBuffer IARFrame.Semantics
    {
      get { return SemanticBuffer; }
    }

    IARCamera IARFrame.Camera
    {
      get { return Camera; }
    }

    IARLightEstimate IARFrame.LightEstimate
    {
      get { return LightEstimate; }
    }
  }
}
