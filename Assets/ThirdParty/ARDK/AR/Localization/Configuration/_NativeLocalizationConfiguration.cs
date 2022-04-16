// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Internals;

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Niantic.ARDK.AR.Localization
{
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  internal sealed class _NativeLocalizationConfiguration:
    ILocalizationConfiguration
  {
    internal _NativeLocalizationConfiguration()
    {
      var nativeHandle = _NARVPSConfiguration_Init();
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARVPSConfiguration_Release(nativeHandle);
    }

    ~_NativeLocalizationConfiguration()
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

    private IntPtr _nativeHandle;
    internal IntPtr _NativeHandle
    {
      get => _nativeHandle;
    }

    // (string/uuid)coordinateSpace + (float)localizationTimeOut + (float)requestTimeLimit +
    // (float)requestPerSecond + (float)gpsWait + (uint32_t)maxRequestInTransit + (uint32_t)meshType
    // (string)localizationEndpoint + (string)meshDownloadEndpoint
    // 1 string/uuid + 4 floats + 2 uint32_t + 2(120-byte strings)
    private long _MemoryPressure
    {
      get => (1L * 8L) + (4L * 4L) + (2L * 4L) + (2L*120L);
    }

    public string MapIdentifier
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          var ptrIdentifier = _NARVPSConfiguration_GetMapIdentifier(_NativeHandle);
          if (ptrIdentifier != IntPtr.Zero)
            return Marshal.PtrToStringAnsi(ptrIdentifier);
          else
            return string.Empty;
        }
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARVPSConfiguration_SetMapIdentifier(_NativeHandle, value);
      }
    }

    public float LocalizationTimeout
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARVPSConfiguration_GetLocalizationTimeout(_NativeHandle);
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARVPSConfiguration_SetLocalizationTimeout(_NativeHandle, value);
      }
    }

    public float RequestTimeLimit
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARVPSConfiguration_GetRequestTimeLimit(_NativeHandle);
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARVPSConfiguration_SetRequestTimeLimit(_NativeHandle, value);
      }
    }

    public string LocalizationEndpoint
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          var stringBuilder = new StringBuilder(512);
          _NARVPSConfiguration_GetLocalizationEndpoint(_NativeHandle, stringBuilder, (ulong)stringBuilder.Capacity);

          var result = stringBuilder.ToString();
          return result;
        }
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARVPSConfiguration_SetLocalizationEndpoint(_NativeHandle, value);
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARVPSConfiguration_Init();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVPSConfiguration_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARVPSConfiguration_GetMapIdentifier(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName, CharSet = CharSet.Ansi)]
    private static extern void _NARVPSConfiguration_SetMapIdentifier
    (
      IntPtr nativeHandle,
      string value
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NARVPSConfiguration_GetLocalizationTimeout(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVPSConfiguration_SetLocalizationTimeout
    (
      IntPtr nativeHandle,
      float value
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NARVPSConfiguration_GetRequestTimeLimit(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVPSConfiguration_SetRequestTimeLimit
    (
      IntPtr nativeHandle,
      float value
    );

    // Set VPS Endpoint
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVPSConfiguration_SetLocalizationEndpoint(IntPtr nativeHandle, string url);

    // Get VPS Endpoint
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVPSConfiguration_GetLocalizationEndpoint
    (
      IntPtr nativeHandle,
      StringBuilder outUrl,
      ulong maxKeySize
    );

  }
}
