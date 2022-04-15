// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  [Serializable]
  internal abstract class _SerializableARAnchor:
    IARAnchor
  {
    public _SerializableARAnchor(Matrix4x4 transform, Guid identifier)
    {
      Transform = transform;
      Identifier = identifier;
    }

    public Matrix4x4 Transform { get; internal set; }
    public Guid Identifier { get; private set; }

    public abstract AnchorType AnchorType { get; }

    public abstract _SerializableARAnchor Copy();

    void IDisposable.Dispose()
    {
      // Do nothing. This implementation of IARAnchor is fully managed.
    }
  }
}
