// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Camera
{
  [Serializable]
  internal sealed class _SerializableARCamera:
    IARCamera
  {
    internal _SerializableARCamera()
    {
    }

    internal _SerializableARCamera
    (
      TrackingState trackingState,
      TrackingStateReason trackingStateReason,
      Resolution imageResolution,
      Resolution cpuImageResolution,
      CameraIntrinsics intrinsics,
      CameraIntrinsics cpuIntrinsics,
      Matrix4x4 transform,
      Matrix4x4 projectionMatrix,
      float worldScale,
      Matrix4x4 estimatedViewMatrix
    )
    {
      TrackingState = trackingState;
      TrackingStateReason = trackingStateReason;
      ImageResolution = imageResolution;
      CPUImageResolution = cpuImageResolution;
      Intrinsics = intrinsics;
      CPUIntrinsics = cpuIntrinsics;
      Transform = transform;
      ProjectionMatrix = projectionMatrix;
      WorldScale = worldScale;
      _estimatedViewMatrix = estimatedViewMatrix;
    }

    public TrackingState TrackingState { get; internal set; }
    public TrackingStateReason TrackingStateReason { get; internal set; }
    public Resolution ImageResolution { get; internal set; }
    public Resolution CPUImageResolution { get; internal set; }
    public CameraIntrinsics Intrinsics { get; internal set; }
    public CameraIntrinsics CPUIntrinsics { get; internal set; }
    public Matrix4x4 Transform { get; internal set; }
    public Matrix4x4 ProjectionMatrix { get; internal set; }
    public float WorldScale { get; internal set; }
    
    public Matrix4x4 CalculateProjectionMatrix
    (
      ScreenOrientation orientation,
      int viewportWidth,
      int viewportHeight,
      float nearClipPlane,
      float farClipPlane
    )
    {
      #if UNITY_EDITOR
      // Screen.orientation doesn't work in the editor
      if (orientation == Screen.orientation)
        orientation = Screen.width > Screen.height
          ? ScreenOrientation.LandscapeLeft
          : ScreenOrientation.Portrait;
      #endif
      
      return MathUtils.CalculateProjectionMatrix
      (
        this,
        orientation,
        viewportWidth,
        viewportHeight,
        nearClipPlane,
        farClipPlane
      );
    }

    internal Matrix4x4 _estimatedViewMatrix;
    public Matrix4x4 GetViewMatrix(ScreenOrientation orientation)
    {
      return _estimatedViewMatrix;
    }

    public Vector2 ProjectPoint
    (
      Vector3 point,
      ScreenOrientation orientation,
      int viewportWidth,
      int viewportHeight
    )
    {
      // TODO(grayson): send message to project point and return result!
      return new Vector2();
    }

    void IDisposable.Dispose()
    {
      // Do nothing. This object is fully managed.
    }
  }
}
