// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Utilities;

using Unity.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  // Can't use [Serializable]. Need to provide a serializer.
  internal abstract class _SerializableAwarenessBufferBase<T>
    : _AwarenessBufferBase,
      IDataBuffer<T>
  where T: struct
  {
    private readonly Matrix4x4 _viewMatrix;

    private bool _disposed;
    private readonly long _consumedUnmanagedMemory;

    private readonly CameraIntrinsics _intrinsics;

    internal _SerializableAwarenessBufferBase
    (
      uint width,
      uint height,
      bool isKeyframe,
      Matrix4x4 viewMatrix,
      NativeArray<T> data,
      CameraIntrinsics intrinsics
    )
      : base(width, height, isKeyframe, intrinsics)
    {
      _viewMatrix = viewMatrix;
      _intrinsics = intrinsics;
      Data = data;

      _consumedUnmanagedMemory = _CalculateConsumedMemory();
      GC.AddMemoryPressure(_consumedUnmanagedMemory);
    }

    ~_SerializableAwarenessBufferBase()
    {
      Dispose();
    }

    public override Matrix4x4 ViewMatrix
    {
      get
      {
        return _viewMatrix;
      }
    }

    public override CameraIntrinsics Intrinsics
    {
      get
      {
        return _intrinsics;
      }
    }

    public NativeArray<T> Data { get; }

    public bool IsRotatedToScreenOrientation { get; set; }

    public void Dispose()
    {
      if (_disposed)
        return;

      if (Data.IsCreated)
        Data.Dispose();

      GC.SuppressFinalize(this);
      GC.RemoveMemoryPressure(_consumedUnmanagedMemory);
      _disposed = true;
    }

    private long _CalculateConsumedMemory()
    {
      return Width * Height * Marshal.SizeOf(typeof(T));
    }
  }
}
