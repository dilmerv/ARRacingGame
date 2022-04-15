// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;

namespace Niantic.ARDK.AR.Image
{
  internal sealed class _NativeImageBuffer:
    IImageBuffer
  {
    static _NativeImageBuffer()
    {
      Platform.Init();
    }

    private readonly long _consumedUnmanagedMemory;

    public _NativeImageBuffer(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;

      Planes = _CreatePlanes(nativeHandle);
      _consumedUnmanagedMemory = _CalculateConsumedMemory();
      GC.AddMemoryPressure(_consumedUnmanagedMemory);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARImage_Release(nativeHandle);
    }

    ~_NativeImageBuffer()
    {
      _ReleaseImmediate(_nativeHandle);
      GC.RemoveMemoryPressure(_consumedUnmanagedMemory);
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _nativeHandle = IntPtr.Zero;

        _ReleaseImmediate(nativeHandle);
        GC.RemoveMemoryPressure(_consumedUnmanagedMemory);
      }

      Planes = null;
    }

    private IntPtr _nativeHandle;
    internal IntPtr _NativeHandle
    {
      get { return _nativeHandle; }
    }

    public ImageFormat Format
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (ImageFormat)_NARImage_GetFormat(_nativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    public _NativeImagePlanes Planes { get; private set; }

    private _NativeImagePlanes _CreatePlanes(IntPtr nativeHandle)
    {
      return new _NativeImagePlanes(nativeHandle);
    }

    IImagePlanes IImageBuffer.Planes
    {
      get { return Planes; }
    }

    private long _CalculateConsumedMemory()
    {
      var planeCount = Planes.Count;
      if (planeCount == 0)
        return 0;

      var plane = Planes[0];

      if (planeCount == 1)
        return plane.BytesPerRow * plane.PixelHeight;

      // This code currently tries to estimate the total memory consumed without querying all
      // the planes. A possible optimization is to have C++ return the size of the unmanaged
      // memory together with the count of planes.
      // 1.5 was obtained because some planes use 1 byte per pixel while others use 2 per pixel.
      return (int)(plane.PixelWidth * plane.PixelHeight * planeCount * 1.5);
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NARImage_GetFormat(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARImage_Release(IntPtr nativeHandle);
  }
}
