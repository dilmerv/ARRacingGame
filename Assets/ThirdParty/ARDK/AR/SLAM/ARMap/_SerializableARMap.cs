// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.AR.SLAM
{
  [Serializable]
  internal sealed class _SerializableARMap:
    IARMap
  {
    internal _SerializableARMap(Guid identifier, float worldScale, Matrix4x4 transform)
    {
      Identifier = identifier;
      WorldScale = worldScale;
      Transform = transform;
    }

    public Guid Identifier { get; private set; }
    public float WorldScale { get; private set; }
    public Matrix4x4 Transform { get; private set; }

    public bool CanGetNativeHandle
    {
      get { return false; }
    }

    IntPtr IARMap.NativeHandle
    {
      get { throw new NotSupportedException("This object doesn't have a NativeHandle."); }
    }
    
    void IDisposable.Dispose()
    {
      // Do nothing. This object is fully managed.
    }
  }
}
