// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.SLAM
{
  /// <summary>
  /// An implementation of IMarkerSyncer that sends data about markers found by the device
  /// to be processed by NAR native
  /// </summary>
  internal sealed class NativeMarkerSyncer:
    IMarkerSyncer
  {
    public Guid StageIdentifier { get; private set; }

    // Private handles and code to deal with native callbacks and initialization
    private IntPtr _nativeHandle = IntPtr.Zero;

    public NativeMarkerSyncer(Guid stageIdentifier)
    {
      StageIdentifier = stageIdentifier;
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _nativeHandle = _NARMarkerManager_Init(StageIdentifier.ToByteArray());
    }

    ~NativeMarkerSyncer()
    {
      _ReleaseUnmanagedResources();

      ARLog._Error("NativeMarkerSyncer should be destroyed by an explicit call to Dispose().");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      _ReleaseUnmanagedResources();
    }

    private void _ReleaseUnmanagedResources()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        if (_nativeHandle != IntPtr.Zero)
        {
          _NARMarkerManager_Release(_nativeHandle);
          _nativeHandle = IntPtr.Zero;
        }
      }
    }

    public void SendMarkerInformation(Vector3[] markerPointLocations)
    {
      if (_nativeHandle == IntPtr.Zero)
      {
        ARLog._Warn("Tried to access invalid NativeMarkerSyncer");
        return;
      }

      var markerPointsFlattened = FlattenVectorArray(markerPointLocations);

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        ARLog._Debug("Sending marker information to native marker manager");
        _NARMarkerManager_SendMarkerInformation
        (
          _nativeHandle,
          markerPointsFlattened,
          markerPointLocations.Length
        );
      }
    }

    public void ScanStationaryMarker
    (
      IARCamera arCamera,
      Matrix4x4 markerWorldTransform,
      Vector3[] markerPointPositions,
      Vector2[] scannedPointPositions,
      double timestamp
    )
    {
      if (_nativeHandle == IntPtr.Zero)
      {
        ARLog._Warn("Tried to access invalid NativeMarkerSyncer");
        return;
      }

      ARLog._Debug("Called ScanStationaryMarker");

      // Object point calculations
      var objectPointsFlattened = FlattenVectorArray(markerPointPositions);

      var mwt = markerWorldTransform;
      var objectExtrinsicsArray =
        new float[]
        {
          mwt.m00, mwt.m01, mwt.m02, mwt.m03,
          mwt.m10, mwt.m11, mwt.m12, mwt.m13,
          mwt.m20, mwt.m21, mwt.m22, mwt.m23,
          mwt.m30, mwt.m31, mwt.m32, mwt.m33,
        };

      // Input point calculations
      var imagePointsFlattened = FlattenVectorArray(scannedPointPositions);

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        ARLog._Debug("Sending stationary marker to be evaluated by native marker manager");
        var nativeCamera = (_NativeARCamera)arCamera;
        _NARMarkerManager_ScanPrintedMarker
        (
          _nativeHandle,
          nativeCamera._NativeHandle,
          objectPointsFlattened,
          markerPointPositions.Length,
          objectExtrinsicsArray,
          imagePointsFlattened,
          scannedPointPositions.Length,
          timestamp
        );
      }
    }

    public void ScanMarkerOnDevice
    (
      IARCamera arCamera,
      Vector2[] scannedPointLocations,
      double timestamp
    )
    {
      if (_nativeHandle == IntPtr.Zero)
      {
        ARLog._Warn("Tried to access invalid NativeMarkerSyncer");
        return;
      }

      var imagePointsFlattened = FlattenVectorArray(scannedPointLocations);

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        ARLog._Debug("Sending device marker to be evaluated by native marker manager");
        var nativeCamera = (_NativeARCamera)arCamera;
        _NARMarkerManager_ScanMarkerOnDevice
        (
          _nativeHandle,
          nativeCamera._NativeHandle,
          imagePointsFlattened,
          scannedPointLocations.Length,
          timestamp
        );
      }
    }

    private static float[] FlattenVectorArray(Vector3[] inputArray)
    {
      var numInputs = inputArray.Length;
      var flattenedArray = new float[numInputs * 3];

      var flattenedIndex = 0;
      for (var inputIndex = 0; inputIndex < numInputs; ++inputIndex)
      {
        var vector = inputArray[inputIndex];
        flattenedArray[flattenedIndex++] = vector.x;
        flattenedArray[flattenedIndex++] = vector.y;
        flattenedArray[flattenedIndex++] = vector.z;
      }

      return flattenedArray;
    }

    private static float[] FlattenVectorArray(Vector2[] inputArray)
    {
      var numInputs = inputArray.Length;
      var flattenedArray = new float[numInputs * 2];

      var flattenedIndex = 0;
      for (var inputIndex = 0; inputIndex < numInputs; ++inputIndex)
      {
        var vector = inputArray[inputIndex];
        flattenedArray[flattenedIndex++] = vector.x;
        flattenedArray[flattenedIndex++] = vector.y;
      }

      return flattenedArray;
    }

#region Externals
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARMarkerManager_Init(byte[] stageIdentifier);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMarkerManager_SendMarkerInformation
    (
      IntPtr nativeHandle,
      float[] objectPoints,
      int numObjectPoints
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMarkerManager_ScanMarkerOnDevice
    (
      IntPtr nativeHandle,
      IntPtr arCameraHandle,
      float[] imagePoints,
      int numImagePoints,
      double timestamp
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMarkerManager_ScanPrintedMarker
    (
      IntPtr nativeHandle,
      IntPtr arCameraHandle,
      float[] objectPoints,
      int numObjectPoints,
      float[] objectExtrinsics,
      float[] imagePoints,
      int numImagePoints,
      double timestamp
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMarkerManager_Release(IntPtr nativeHandle);
#endregion
  }
}
