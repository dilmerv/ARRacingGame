// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.AR.ReferenceImage
{
  [Serializable]
  internal sealed class _SerializableARReferenceImage:
    IARReferenceImage
  {
    internal _SerializableARReferenceImage(string name, Vector2 physicalSize)
    {
      Name = name;
      PhysicalSize = physicalSize;
    }
    
    public string Name { get; set; }
    public Vector2 PhysicalSize { get; private set; }
    
    void IDisposable.Dispose()
    {
      // Do nothing. This implementation of IARReferenceImage is fully managed.
    }
  }
}
