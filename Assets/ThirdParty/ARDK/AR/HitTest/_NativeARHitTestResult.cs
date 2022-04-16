// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.HitTest
{
  internal sealed class _NativeARHitTestResult:
    IARHitTestResult
  {
    // Used to inform the C# GC that there is managed memory held by this object
    // type + anchor + distance + world transform + local transform
    private const long _MemoryPressure =
      (1L * 8L) + (1L * 8L) + (1L * 4L) + (16L * 4L) + (16L * 4L);

    static _NativeARHitTestResult()
    {
      Platform.Init();
    }

    private IntPtr _nativeHandle;

    internal _NativeARHitTestResult(IntPtr nativeHandle, float worldScale)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
      WorldScale = worldScale;
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARHitTestResult_Release(nativeHandle);
    }

    ~_NativeARHitTestResult()
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

    public ARHitTestResultType Type
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (ARHitTestResultType) _NARHitTestResult_GetType(_nativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    public float Distance
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARHitTestResult_GetDistance(_nativeHandle) * WorldScale;

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    private _NativeARAnchor _anchor;
    internal _NativeARAnchor Anchor
    {
      get
      {
        var result = _anchor;

        if (result == null)
        {
          var anchorHandle = IntPtr.Zero;

          if (NativeAccess.Mode == NativeAccess.ModeType.Native)
            anchorHandle = _NARHitTestResult_GetAnchor(_nativeHandle);

          if (anchorHandle == IntPtr.Zero)
            return null;

          result = _ARAnchorFactory._FromNativeHandle(anchorHandle);
          _anchor = result;
        }

        return result;
      }
    }

    public Matrix4x4 LocalTransform
    {
      get
      {
        var nativeTransform = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARHitTestResult_GetLocalTransform(_nativeHandle, nativeTransform);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        var transform =
          NARConversions.FromNARToUnity(_Convert.InternalToMatrix4x4(nativeTransform));

        return transform;
      }
    }

    public Matrix4x4 WorldTransform
    {
      get
      {
        var nativeTransform = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARHitTestResult_GetWorldTransform(_nativeHandle, nativeTransform);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        var transform = NARConversions.FromNARToUnity(_Convert.InternalToMatrix4x4(nativeTransform));
        _Convert.ApplyScale(ref transform, WorldScale);

        return transform;
      }
    }

    IARAnchor IARHitTestResult.Anchor
    {
      get { return Anchor; }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARHitTestResult_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARHitTestResult_GetType(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARHitTestResult_GetAnchor(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NARHitTestResult_GetDistance(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARHitTestResult_GetWorldTransform
    (
      IntPtr nativeHandle,
      float[] outTransform
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARHitTestResult_GetLocalTransform
    (
      IntPtr nativeHandle,
      float[] outTransform
    );
  }
}
