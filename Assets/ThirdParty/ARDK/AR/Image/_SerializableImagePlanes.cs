// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Niantic.ARDK.AR.Image
{
  [Serializable]
  internal sealed class _SerializableImagePlanes:
    IImagePlanes
  {
    internal readonly _SerializableImagePlane[] _planes;
    internal _SerializableImagePlanes(_SerializableImagePlane[] planeArray)
    {
      if (planeArray == null)
        throw new ArgumentNullException(nameof(planeArray));

      _planes = planeArray;
    }

    internal void _Dispose()
    {
      foreach (var plane in _planes)
        plane._Dispose();
    }

    public int Count
    {
      get { return _planes.Length; }
    }

    public _SerializableImagePlane this[int planeIndex]
    {
      get { return _planes[planeIndex]; }
    }
    IImagePlane IImagePlanes.this[int planeIndex]
    {
      get { return this[planeIndex]; }
    }
    
    public IEnumerator<_SerializableImagePlane> GetEnumerator()
    {
      foreach (var plane in _planes)
        yield return plane;
    }

    IEnumerator<IImagePlane> IEnumerable<IImagePlane>.GetEnumerator()
    {
      return GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
