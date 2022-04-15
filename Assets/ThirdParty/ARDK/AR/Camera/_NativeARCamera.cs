// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Camera
{
  internal sealed class _NativeARCamera:
    IARCamera
  {
    // tracking peerState + tracking peerState reason + image resolution + transform +
    // projection matrix + view matrix
    private const long _MemoryPressure =
      (1L * 8L) + (1L * 8L) + (2L * 8L) + (16L * 4L) + (16L * 4L) + (16L * 4L);

    static _NativeARCamera()
    {
      Platform.Init();
    }

    // Directly release a native camera without generating an object or waiting for GC.
    internal static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      _NARCamera_Release(nativeHandle);
    }

    private static _WeakValueDictionary<_CppAddressAndScale, _NativeARCamera> _allCameras =
      new _WeakValueDictionary<_CppAddressAndScale, _NativeARCamera>();

    internal static _NativeARCamera _FromNativeHandle(IntPtr nativeHandle, float worldScale)
    {
      _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _allCameras);

      var cppAddress = _NARCamera_GetCppAddress(nativeHandle);
      var handleAndScale = new _CppAddressAndScale(cppAddress, worldScale);

      var result = _allCameras.TryGetValue(handleAndScale);
      if (result != null)
      {
        // We found an existing C# wrapper for the actual C++ object. Let's release the new
        // nativeHandle and return the existing wrapper.

        _ReleaseImmediate(nativeHandle);
        return result;
      }

      Func<_CppAddressAndScale, _NativeARCamera> creator =
        (_) => new _NativeARCamera(nativeHandle, worldScale);

      result = _allCameras.GetOrAdd(handleAndScale, creator);

      if (result._nativeHandle != nativeHandle)
      {
        // We got on the very odd situation where just after a TryGetValue another value was added.
        // We should release our handle immediately. Then, we can return the result as it is valid.
        _ReleaseImmediate(nativeHandle);
      }

      return result;
    }

    internal _NativeARCamera(IntPtr nativeHandle, float worldScale)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      WorldScale = worldScale;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    ~_NativeARCamera()
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

    private IntPtr _nativeHandle;
    internal IntPtr _NativeHandle
    {
      get { return _nativeHandle; }
    }

    private TrackingState _trackingState;
    public TrackingState TrackingState
    {
      get
      {
        _PopulateTrackingStateIfNeeded();

        return _trackingState;
      }
    }

    private TrackingStateReason _trackingStateReason;
    public TrackingStateReason TrackingStateReason
    {
      get
      {
        _PopulateTrackingStateIfNeeded();

        return _trackingStateReason;
      }
    }

    public Resolution ImageResolution
    {
      get
      {
        var nativeImageResolution = new Int32[2];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARCamera_GetGPUImageResolution(_nativeHandle, nativeImageResolution);

        return
          new Resolution
          {
            width = nativeImageResolution[0],
            height = nativeImageResolution[1]
          };
      }
    }

    public Resolution CPUImageResolution
    {
      get
      {
        var nativeImageResolution = new Int32[2];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARCamera_GetCPUImageResolution(_nativeHandle, nativeImageResolution);

        return
          new Resolution
          {
            width = nativeImageResolution[0],
            height = nativeImageResolution[1]
          };
      }
    }

    public CameraIntrinsics Intrinsics
    {
      get
      {
        var nativeIntrinsics = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARCamera_GetGPUIntrinsics(_nativeHandle, nativeIntrinsics);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        return
          new CameraIntrinsics
          (
            nativeIntrinsics[0],
            nativeIntrinsics[4],
            nativeIntrinsics[6],
            nativeIntrinsics[7]
          );
      }
    }

    public CameraIntrinsics CPUIntrinsics
    {
      get
      {
        var nativeIntrinsics = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARCamera_GetCPUIntrinsics(_nativeHandle, nativeIntrinsics);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        return
          new CameraIntrinsics
          (
            nativeIntrinsics[0],
            nativeIntrinsics[4],
            nativeIntrinsics[6],
            nativeIntrinsics[7]
          );
      }
    }

    public Matrix4x4 Transform
    {
      get
      {
        var nativeTransform = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARCamera_GetTransform(_nativeHandle, nativeTransform);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        var transform = NARConversions.FromNARToUnity
          (_Convert.InternalToMatrix4x4(nativeTransform));

        _Convert.ApplyScale(ref transform, WorldScale);
        return transform;
      }
    }

    /* The major difference between Unity and OpenGL is that Unity uses a left-handed
        coordinate system while OpenGL uses a right-handed system.

                      X-axis          Y-axis         z-axis
             Unity    left-to-right   bottom-to-top  near-to-far
             OpenGL   left-to-right   bottom-to-top  far-to-near

     So "forward" in OpenGL is "-z". In Unity forward is "+z". Most hand-rules you might know
        from math are inverted in Unity.
     For example the cross product usually uses the right hand rule c = a x b where a is thumb,
        b is index finger and c is the middle finger.
     In Unity you would use the same logic, but with the left hand.

     However this does not affect the projection matrix as Unity uses the OpenGL convention
        for the projection matrix.
     The required z-flipping is done by the cameras worldToCameraMatrix. So the projection
        matrix should look the same as in OpenGL.
     */
    public Matrix4x4 ProjectionMatrix
    {
      get
      {
        var nativeMatrix = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARCamera_GetProjectionMatrix(_nativeHandle, nativeMatrix);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        return _Convert.InternalToMatrix4x4(nativeMatrix);
      }
    }

    public Matrix4x4 CalculateProjectionMatrix
    (
      ScreenOrientation orientation,
      int viewportWidth,
      int viewportHeight,
      float nearClipPlane,
      float farClipPlane
    )
    {
      var nativeProjectionMatrix = new float[16];

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARCamera_CalculateProjectionMatrix
        (
          _nativeHandle,
          (UInt64)orientation,
          viewportWidth,
          viewportHeight,
          nearClipPlane,
          farClipPlane,
          nativeProjectionMatrix
        );
      }
      #pragma warning disable 0162
      else
          throw new IncorrectlyUsedNativeClassException();
      #pragma warning restore 0162

      return _Convert.InternalToMatrix4x4(nativeProjectionMatrix);
    }

    public Matrix4x4 GetViewMatrix(ScreenOrientation orientation)
    {
      var nativeViewMatrix = new float[16];

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARCamera_GetViewMatrix
        (
          _nativeHandle,
          (UInt64)orientation,
          nativeViewMatrix
        );
      }
      #pragma warning disable 0162
      else
        throw new IncorrectlyUsedNativeClassException();
      #pragma warning restore 0162

      var viewMatrix = NARConversions.FromNARToUnity(_Convert.InternalToMatrix4x4(nativeViewMatrix));
      _Convert.ApplyScale(ref viewMatrix, WorldScale);
      return viewMatrix;
    }

    public Vector2 ProjectPoint
    (
      Vector3 point,
      ScreenOrientation orientation,
      int viewportWidth,
      int viewportHeight
    )
    {
      var outPoint = new float[2];

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        point = NARConversions.FromUnityToNAR(point);
        var worldScale = WorldScale;

        _NARCamera_ProjectPoint
        (
          _nativeHandle,
          point.x * (1 / worldScale),
          point.y * (1 / worldScale),
          point.z * (1 / worldScale),
          (UInt64)orientation,
          viewportWidth,
          viewportHeight,
          outPoint
        );
      }

      return new Vector2(outPoint[0], outPoint[1]);
    }

    private bool _isTrackingStateLoaded;
    // Populates the tracking state and reason.
    private void _PopulateTrackingStateIfNeeded()
    {
      if (_isTrackingStateLoaded)
        return;

      var nativeTrackingStates = new UInt64[2];

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARCamera_GetTrackingStates(_nativeHandle, nativeTrackingStates);

      _trackingState = (TrackingState)nativeTrackingStates[0];
      _trackingStateReason = (TrackingStateReason)nativeTrackingStates[1];
      _isTrackingStateLoaded = true;
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARCamera_GetCppAddress(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern byte _NARCamera_GetTrackingStates
    (
      IntPtr nativeHandle,
      UInt64[] outTrackingStates
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_GetTransform(IntPtr nativeHandle, float[] outMatrix);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_GetGPUImageResolution
    (
      IntPtr nativeHandle,
      Int32[] outResolution
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_GetCPUImageResolution
    (
      IntPtr nativeHandle,
      Int32[] outResolution
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_GetGPUIntrinsics
    (
      IntPtr nativeHandle,
      float[] outIntrinsics
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_GetCPUIntrinsics
    (
      IntPtr nativeHandle,
      float[] outIntrinsics
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_GetProjectionMatrix
    (
      IntPtr nativeHandle,
      float[] outMatrix
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_CalculateProjectionMatrix
    (
      IntPtr nativeHandle,
      UInt64 viewportOrientation,
      Int32 viewportWidth,
      Int32 viewportHeight,
      float zNear,
      float zFar,
      float[] outMatrix
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_GetViewMatrix
    (
      IntPtr nativeHandle,
      UInt64 viewportOrientation,
      float[] outMatrix
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARCamera_ProjectPoint
    (
      IntPtr nativeHandle,
      float pointX,
      float pointY,
      float pointZ,
      UInt64 viewportOrientation,
      Int32 viewportWidth,
      Int32 viewportHeight,
      float[] outPoint
    );
  }
}
