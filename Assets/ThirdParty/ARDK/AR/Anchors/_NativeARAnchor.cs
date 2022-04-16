// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  internal abstract class _NativeARAnchor:
    IARAnchor
  {
    static _NativeARAnchor()
    {
      Platform.Init();
    }

    internal static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        if (nativeHandle != IntPtr.Zero)
          _NARAnchor_Release(nativeHandle);
      }
      #pragma warning disable 0162
      else if (NativeAccess.Mode == NativeAccess.ModeType.Testing)
      {
        _TestingShim.ReleasedHandles.Add(nativeHandle);
      }
      #pragma warning restore 0162
    }

    internal static Guid _GetIdentifier(IntPtr nativeHandle)
    {
      Guid result;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARAnchor_GetIdentifier(nativeHandle, out result);
      }
      #pragma warning disable 0162
      else
      {
        ulong handleNumber = (ulong)nativeHandle;
        var bytes = new byte[16];
        bytes[0] = (byte)handleNumber;
        bytes[1] = (byte)(handleNumber >> 8);
        bytes[2] = (byte)(handleNumber >> 16);
        bytes[3] = (byte)(handleNumber >> 24);
        bytes[4] = (byte)(handleNumber >> 32);
        bytes[5] = (byte)(handleNumber >> 40);
        bytes[6] = (byte)(handleNumber >> 48);
        bytes[7] = (byte)(handleNumber >> 56);
        result = new Guid(bytes);
      }
      #pragma warning restore 0162

      return result;
    }

    internal _NativeARAnchor(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;

      GC.AddMemoryPressure(MemoryPressure);
    }

    ~_NativeARAnchor()
    {
      _ReleaseImmediate(_nativeHandle);
      GC.RemoveMemoryPressure(MemoryPressure);
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _ARAnchorFactory._RemoveFromCache(this);

        _nativeHandle = IntPtr.Zero;

        _ReleaseImmediate(nativeHandle);
        GC.RemoveMemoryPressure(MemoryPressure);
      }
    }

    private IntPtr _nativeHandle;
    internal IntPtr _NativeHandle
    {
      get { return _nativeHandle; }
    }

    public abstract AnchorType AnchorType { get; }

    // Used to inform the C# GC that there is managed memory held by this object
    protected virtual long MemoryPressure
    {
      get { return (16L * 4L) + (16L * 1L); }
    }

    public Matrix4x4 Transform
    {
      get
      {
        var nativeTransform = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARAnchor_GetTransform(_nativeHandle, nativeTransform);
        #pragma warning disable 0162
        else if (NativeAccess.Mode == NativeAccess.ModeType.Testing)
          nativeTransform = _TestingShim.RawTransform;
        #pragma warning restore 0162

        return NARConversions.FromNARToUnity(_Convert.InternalToMatrix4x4(nativeTransform));
      }
    }

    public Guid Identifier
    {
      get
      {
        return _GetIdentifier(_nativeHandle);
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARAnchor_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARAnchor_GetIdentifier(IntPtr nativeHandle, out Guid identifier);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARAnchor_GetTransform(IntPtr nativeHandle, float[] outTransform);

    internal static class _TestingShim
    {
      // This stores all native handles that would normally be freed by passing them to
      // _NARAnchor_Release.
      public static List<IntPtr> ReleasedHandles = new List<IntPtr>();

      // All anchors will act as if _NARAnchor_GetTransform output this transform. The actual value
      // of Transform will be different based on the coordinate transform and the scale.
      public static float[] RawTransform = new float[16];
    }
  }
}
