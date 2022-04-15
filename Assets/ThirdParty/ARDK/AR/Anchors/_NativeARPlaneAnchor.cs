// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.PlaneGeometry;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  internal sealed class _NativeARPlaneAnchor:
    _NativeARAnchor,
    IARPlaneAnchor
  {
    public _NativeARPlaneAnchor(IntPtr nativeHandle):
      base(nativeHandle)
    {
    }

    protected override long MemoryPressure
    {
      get { return base.MemoryPressure + (1L * 8L) + (3L * 4L) + (3L * 4L); }
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Plane; }
    }

    public PlaneAlignment Alignment
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (PlaneAlignment)_NARPlaneAnchor_GetAlignment(_NativeHandle);
        #pragma warning disable 0162
        else
          return PlaneAlignment.Unknown;
        #pragma warning restore 0162
      }
    }

    public PlaneClassification Classification
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (PlaneClassification)_NARPlaneAnchor_GetClassification(_NativeHandle);
        #pragma warning disable 0162
        else
          return PlaneClassification.None;
        #pragma warning restore 0162
      }
    }

    public PlaneClassificationStatus ClassificationStatus
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (PlaneClassificationStatus)_NARPlaneAnchor_GetClassificationStatus(_NativeHandle);
        #pragma warning disable 0162
        else
          return PlaneClassificationStatus.NotAvailable;
        #pragma warning restore 0162
      }
    }

    public Vector3 Center
    {
      get
      {
        var nativeCenter = new float[3];
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARPlaneAnchor_GetCenter(_NativeHandle, nativeCenter);

        var center =
          new Vector3
          (
            nativeCenter[0],
            nativeCenter[1],
            nativeCenter[2]
          );

        return NARConversions.FromNARToUnity(center);
      }
    }

    public Vector3 Extent
    {
      get
      {
        var nativeExtent = new float[3];
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARPlaneAnchor_GetExtent(_NativeHandle, nativeExtent);

        return
          new Vector3
          (
            nativeExtent[0],
            nativeExtent[1],
            nativeExtent[2]
          );
      }
    }

    internal _NativeARPlaneGeometry Geometry
    {
      get
      {
        IntPtr geometryHandle = IntPtr.Zero;
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          geometryHandle = _NARPlaneAnchor_GetGeometry(_NativeHandle);

        if (geometryHandle == IntPtr.Zero)
          return null;

        return new _NativeARPlaneGeometry(geometryHandle);
      }
    }

    IARPlaneGeometry IARPlaneAnchor.Geometry
    {
      get { return Geometry; }
    }

    public override string ToString()
    {
      return _NativeHandle.ToString();
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARPlaneAnchor_GetAlignment(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARPlaneAnchor_GetClassification(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARPlaneAnchor_GetClassificationStatus(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARPlaneAnchor_GetCenter(IntPtr nativeHandle, float[] outCenter);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARPlaneAnchor_GetExtent(IntPtr nativeHandle, float[] outExtent);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARPlaneAnchor_GetGeometry(IntPtr nativeHandle);
  }
}
