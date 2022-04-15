// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Unity.Collections;

namespace Niantic.ARDK.AR.Awareness
{
  public interface IDataBuffer<T> : IAwarenessBuffer
  where T: struct
  {
    /// Raw data of this buffer.
    NativeArray<T> Data { get; }
  }
}
