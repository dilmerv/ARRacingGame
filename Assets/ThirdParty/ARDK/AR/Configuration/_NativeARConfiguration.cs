// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using AOT;

using Niantic.ARDK.AR.Awareness.Depth.Generators;
using Niantic.ARDK.AR.VideoFormat;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.Configuration
{
  internal abstract class _NativeARConfiguration:
    IARConfiguration
  {
    static _NativeARConfiguration()
    {
      Platform.Init();
    }

    internal _NativeARConfiguration(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARConfiguration_Release(nativeHandle);
    }

    ~_NativeARConfiguration()
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
      get { return _nativeHandle; }
    }

    // Used to inform the C# GC that there is managed memory held by this object.
    protected virtual long _MemoryPressure
    {
      get { return (1L * 1L) + (1L * 8L); }
    }

    public abstract ReadOnlyCollection<IARVideoFormat> SupportedVideoFormats { get; }

    public bool IsLightEstimationEnabled
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_IsLightEstimationEnabled(_nativeHandle) != 0;

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetLightEstimationEnabled(_nativeHandle, value ? 1 : (UInt32)0);
      }
    }

    public WorldAlignment WorldAlignment
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (WorldAlignment)_NARConfiguration_GetWorldAlignment(_nativeHandle);

#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetWorldAlignment(_nativeHandle, (UInt64)value);
      }
    }

    // TODO: Maybe we should review this. It seems we create a new instance every time we call
    // get. Are users disposing it? Are they allowed to dispose it?
    public IARVideoFormat VideoFormat
    {
      get
      {
        var videoFormatHandle = _NARConfiguration_GetVideoFormat(_nativeHandle);

        if (videoFormatHandle == IntPtr.Zero)
          return null;

        return _NativeARVideoFormat._FromNativeHandle(videoFormatHandle);
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          if (!(value is _NativeARVideoFormat nativeFormat))
            return;
          
          _NARConfiguration_SetVideoFormat(_nativeHandle, nativeFormat._NativeHandle);
        }
      }
    }

    public virtual void CopyTo(IARConfiguration target)
    {
      target.IsLightEstimationEnabled = IsLightEstimationEnabled;
      target.WorldAlignment = WorldAlignment;
      var videoFormat = VideoFormat;
      if (videoFormat != null)
        target.VideoFormat = videoFormat;
    }

    [MonoPInvokeCallback(typeof(_ARConfiguration_CheckCapabilityAndSupport_Callback))]
    protected static void ConfigurationCheckCapabilityAndSupportCallback
    (
      IntPtr context,
      UInt64 hardwareCapability,
      UInt64 softwareSupport
    )
    {
      var safeHandle =
        SafeGCHandle<Action<ARHardwareCapability, ARSoftwareSupport>>.FromIntPtr(context);

      var callback = safeHandle.TryGetInstance();
      safeHandle.Free();

      if (callback == null)
      {
        // callback was deallocated
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          callback((ARHardwareCapability)hardwareCapability, (ARSoftwareSupport)softwareSupport);
        }
      );
    }

    [DllImport(_ARDKLibrary.libraryName)]
    protected static extern void _NARConfiguration_CheckCapabilityAndSupport
    (
      UInt64 type,
      IntPtr applicationContext,
      _ARConfiguration_CheckCapabilityAndSupport_Callback callback
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARConfiguration_IsLightEstimationEnabled(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetLightEstimationEnabled
    (
      IntPtr nativeHandle,
      UInt32 enabled
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARConfiguration_GetWorldAlignment(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetWorldAlignment
    (
      IntPtr nativeHandle,
      UInt64 worldAlignment
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARConfiguration_GetVideoFormat(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetVideoFormat
    (
      IntPtr nativeHandle,
      IntPtr nativeVideoFormat
    );

    protected delegate void _ARConfiguration_CheckCapabilityAndSupport_Callback
    (
      IntPtr context,
      UInt64 hardwareCapability,
      UInt64 softwareSupport
    );
  }
}
