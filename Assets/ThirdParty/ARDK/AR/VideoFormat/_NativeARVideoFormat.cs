// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.VideoFormat
{
  internal sealed class _NativeARVideoFormat:
    IARVideoFormat
  {
    // Estimated unmanaged memory usage: 1 int64 + 2 Vector2
    private const long _MemoryPressure = (1L * 8L) + (2L * 4L) + (2L * 4L);

    static _NativeARVideoFormat()
    {
      Platform.Init();
    }

    private static readonly _WeakValueDictionary<IntPtr, _NativeARVideoFormat> _allVideoFormats =
      new _WeakValueDictionary<IntPtr, _NativeARVideoFormat>();

    internal static _NativeARVideoFormat _FromNativeHandle(IntPtr nativeHandle)
    {
      _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _allVideoFormats);
      
      #pragma warning disable CS0162
      if (NativeAccess.Mode != NativeAccess.ModeType.Native)
        return new _NativeARVideoFormat(nativeHandle);
      #pragma warning restore CS0162

      var uniqueId = _NARVideoFormat_GetPlatformHandle(nativeHandle);

      var result = _allVideoFormats.TryGetValue(uniqueId);
      if (result != null)
      {
        // There is an already existing format for the given uniqueId. So we release the new handle
        // immediately and return the existing object.
        _ReleaseImmediate(nativeHandle);
        return result;
      }

      result = _allVideoFormats.GetOrAdd(uniqueId, (_) => new _NativeARVideoFormat(nativeHandle));
      if (result._NativeHandle != nativeHandle)
      {
        // Just after our TryGetValue an instance was registered for the given uniqueId.
        // So, same as before, we need to release our handle immediately.
        _ReleaseImmediate(nativeHandle);
      }

      // We either created a new object or we are using an existing one. In any case, return what
      // we got.
      return result;
    }

    private IntPtr _nativeHandle;

    private _NativeARVideoFormat(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    public static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARVideoFormat_Release(nativeHandle);
    }

    ~_NativeARVideoFormat()
    {
      _ReleaseImmediate(_nativeHandle);
      GC.RemoveMemoryPressure(_MemoryPressure);
    }

    internal IntPtr _NativeHandle
    {
      get { return _nativeHandle; }
    }

    public int FramesPerSecond
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (int)_NARVideoFormat_GetFramesPerSecond(_nativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    public Vector2 ImageResolution
    {
      get
      {
        var rawImageResolution = new Int32[2];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARVideoFormat_GetImageResolution(_nativeHandle, rawImageResolution);

        return new Vector2(rawImageResolution[0], rawImageResolution[1]);
      }
    }

    public Vector2 TextureResolution
    {
      get
      {
        var rawTextureResolution = new Int32[2];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARVideoFormat_GetTextureResolution(_nativeHandle, rawTextureResolution);

        return new Vector2(rawTextureResolution[0], rawTextureResolution[1]);
      }
    }

    internal _SerializableARVideoFormat _AsSerializable()
    {
      return new _SerializableARVideoFormat(FramesPerSecond, ImageResolution, TextureResolution);
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVideoFormat_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARVideoFormat_GetPlatformHandle(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int64 _NARVideoFormat_GetFramesPerSecond(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVideoFormat_GetImageResolution
    (
      IntPtr nativeHandle,
      Int32[] outImageResolution
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVideoFormat_GetTextureResolution
    (
      IntPtr nativeHandle,
      Int32[] outTextureResolution
    );
  }
}

