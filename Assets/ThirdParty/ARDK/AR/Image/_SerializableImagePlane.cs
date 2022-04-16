// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace Niantic.ARDK.AR.Image
{
  // This class is not marked [Serializable] on purpose.
  // We need to register the proper Serializer into the global serializer, as this class has some
  // fields that are actually not serializable.
  internal sealed class _SerializableImagePlane:
    IImagePlane
  {
#if UNITY_EDITOR
    internal _SerializableImagePlane
    (
      NativeArray<byte> data,
      AtomicSafetyHandle dataHandle,
      int pixelWidth,
      int pixelHeight,
      int bytesPerRow,
      int bytesPerPixel
    )
    : this(data,pixelWidth, pixelHeight, bytesPerRow, bytesPerPixel)
    {
      _dataHandle = dataHandle;
    }
#endif

    internal _SerializableImagePlane
    (
      NativeArray<byte> data,
      int pixelWidth,
      int pixelHeight,
      int bytesPerRow,
      int bytesPerPixel
    )
    {
      if (data == null)
        throw new ArgumentNullException(nameof(data));

      if (pixelWidth < 0)
        throw new ArgumentOutOfRangeException(nameof(pixelWidth));

      if (pixelHeight < 0)
        throw new ArgumentOutOfRangeException(nameof(pixelHeight));

      if (bytesPerPixel < 1)
        throw new ArgumentOutOfRangeException(nameof(bytesPerPixel));

      if (bytesPerRow < pixelWidth * bytesPerPixel)
        throw new ArgumentOutOfRangeException(nameof(bytesPerRow));

      if (data.Length < bytesPerPixel * pixelHeight)
        throw new ArgumentException("The provided data is too small for the dimensions given.");

      Data = data;
      PixelWidth = pixelWidth;
      PixelHeight = pixelHeight;
      BytesPerRow = bytesPerRow;
      BytesPerPixel = bytesPerPixel;
    }

    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }
    public int BytesPerRow { get; private set; }
    public int BytesPerPixel { get; private set; }

    public NativeArray<byte> Data { get; private set; }

#if UNITY_EDITOR
    // Value depends on whether compression was used when serializing the original device frame
    // to send over RemoteConnection.
    private readonly AtomicSafetyHandle? _dataHandle;
#endif

    // This method is internal because it is the _SerializableImageBuffer that's supposed to call
    // it. Users do not need to Dispose() instances of this class.
    internal void _Dispose()
    {
#if UNITY_EDITOR
      if (_dataHandle.HasValue)
        AtomicSafetyHandle.Release(_dataHandle.Value);
      else
        Data.Dispose();
#else
      Data.Dispose();
#endif
    }
  }
}
