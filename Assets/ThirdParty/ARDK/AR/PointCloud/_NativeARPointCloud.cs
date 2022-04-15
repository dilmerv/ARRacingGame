// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.PointCloud
{
  internal sealed class _NativeARPointCloud:
    IARPointCloud
  {
    static _NativeARPointCloud()
    {
      Platform.Init();
    }

    private IntPtr _nativeHandle;

    // Used to inform the C# GC that there is managed memory held by this object
    // points + identifiers (estimating 200 points)
    private const long _MemoryPressure = (200L * (3L * 4L)) + (200L * 8L);

    public _NativeARPointCloud(IntPtr nativeHandle, float worldScale)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      WorldScale = worldScale;

      GC.AddMemoryPressure(_MemoryPressure);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARPointCloud_Release(nativeHandle);
    }

    ~_NativeARPointCloud()
    {
      _ReleaseImmediate(_nativeHandle);
      GC.RemoveMemoryPressure(_MemoryPressure);
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _nativeHandle = IntPtr.Zero;

        _ReleaseImmediate(nativeHandle);
        GC.RemoveMemoryPressure(_MemoryPressure);
      }
    }

    public float WorldScale { get; private set; }

    private ReadOnlyCollection<Vector3> _points;
    public ReadOnlyCollection<Vector3> Points
    {
      get
      {
        if (_points != null)
          return _points;

        var points = EmptyArray<Vector3>.Instance;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            var obtained = _NARPointCloud_GetPoints(_nativeHandle, points.Length, points);

            if (obtained == points.Length)
              break;

            points = new Vector3[Math.Abs(obtained)];
          }
        }

        var pointCount = points.Length;
        if (pointCount > 0)
        {
          for (var i = 0; i < pointCount; i++)
          {
            var rawPoint = points[i];
            var point =
              new Vector3(rawPoint.x * WorldScale, rawPoint.y * WorldScale, rawPoint.z * WorldScale);

            points[i] = NARConversions.FromNARToUnity(point);
          }
        }

        _points = new ReadOnlyCollection<Vector3>(points);
        return _points;
      }
    }

    private ReadOnlyCollection<ulong> _identifiers;
    public ReadOnlyCollection<ulong> Identifiers
    {
      get
      {
        if (_identifiers != null)
          return _identifiers;

        var identifiers = EmptyArray<ulong>.Instance;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            var obtained =
              _NARPointCloud_GetIdentifiers(_nativeHandle, identifiers.Length, identifiers);

            if (obtained == identifiers.Length)
              break;

            identifiers = new ulong[Math.Abs(obtained)];
          }
        }

        _identifiers = new ReadOnlyCollection<ulong>(identifiers);
        return _identifiers;
      }
    }


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARPointCloud_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NARPointCloud_GetPoints
    (
      IntPtr nativeHandle,
      Int32 lengthOfOutPoints,
      Vector3[] outPoints
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NARPointCloud_GetIdentifiers
    (
      IntPtr nativeHandle,
      Int32 lengthOfOutIdentifiers,
      UInt64[] outIdentifiers
    );
  }
}
