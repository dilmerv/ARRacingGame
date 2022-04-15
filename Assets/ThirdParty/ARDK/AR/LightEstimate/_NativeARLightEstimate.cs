// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.AR.LightEstimate
{
  internal sealed class _NativeARLightEstimate:
    IARLightEstimate
  {
    // Estimated unmanaged memory usage: 2 floats + 4 float array
    private const long _MemoryPressure = (2L * 4L) + (4L * 4L);

    static _NativeARLightEstimate()
    {
      Platform.Init();
    }

    private IntPtr _nativeHandle;

    internal _NativeARLightEstimate(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      GC.AddMemoryPressure(_MemoryPressure);
      _nativeHandle = nativeHandle;
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARLightEstimate_Release(nativeHandle);
    }

    ~_NativeARLightEstimate()
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

    public float AmbientIntensity
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARLightEstimate_GetAmbientIntensity(_nativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    public float AmbientColorTemperature
    {
      get
      {
#if UNITY_IOS
        return _NARLightEstimate_GetAmbientColorTemperature(_nativeHandle);
#else
        return 0f;
#endif
      }
    }

    public ReadOnlyCollection<float> ColorCorrection
    {
      get
      {
#if UNITY_ANDROID
        // Can we cache both the array and the read-only collection and just update it
        // at every request instead of allocating new ones???

        var correction = new float[4];
        _NARLightEstimate_GetColorCorrection(_nativeHandle, correction);
        return new ReadOnlyCollection<float>(correction);
#else
        return EmptyReadOnlyCollection<float>.Instance;
#endif
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARLightEstimate_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NARLightEstimate_GetAmbientIntensity(IntPtr nativeHandle);

#if UNITY_IOS
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NARLightEstimate_GetAmbientColorTemperature(IntPtr nativeHandle);
#elif UNITY_ANDROID
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARLightEstimate_GetColorCorrection
    (
      IntPtr nativeHandle,
      float[] outCorrection
    );
#endif
  }
}
