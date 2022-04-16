// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;

namespace Niantic.ARDK.AR.Image
{
  internal sealed class _NativeImagePlanes:
    IImagePlanes
  {
    private readonly _NativeImagePlane[] _planes;

    internal _NativeImagePlanes(IntPtr nativeHandle)
    {
      _nativeHandle = nativeHandle;

      ulong count = 0;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        count = _NARImage_GetPlaneCount(nativeHandle);

      _planes = new _NativeImagePlane[count];
    }

    private IntPtr _nativeHandle;

    internal IntPtr _NativeHandle
    {
      get { return _nativeHandle; }
    }

    public int Count
    {
      get { return _planes.Length; }
    }

    public _NativeImagePlane this[int planeIndex]
    {
      get
      {
        if (planeIndex < 0 || planeIndex >= Count)
          throw new ArgumentOutOfRangeException(nameof(planeIndex));

        var result = _planes[planeIndex];
        if (result == null)
        {
          result = _CreatePlane(planeIndex);
          _planes[planeIndex] = result;
        }

        return result;
      }
    }

    public IEnumerator<_NativeImagePlane> GetEnumerator()
    {
      int count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }

    private _NativeImagePlane _CreatePlane(int planeIndex)
    {
      return new _NativeImagePlane(_nativeHandle, planeIndex);
    }

    IImagePlane IImagePlanes.this[int planeIndex]
    {
      get { return this[planeIndex]; }
    }

    IEnumerator<IImagePlane> IEnumerable<IImagePlane>.GetEnumerator()
    {
      // In newer versions of .NET we could just:
      //   return GetEnumerator();

      int count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARImage_GetPlaneCount(IntPtr nativeHandle);
  }
}
