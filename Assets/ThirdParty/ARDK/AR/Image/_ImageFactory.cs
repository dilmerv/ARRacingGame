// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Unity.Collections;

namespace Niantic.ARDK.AR.Image
{
  internal static class _ImageFactory
  {
    // compressionLevel 0 = raw.
    internal static _SerializableImageBuffer _AsSerializable
    (
      this IImageBuffer source,
      int compressionLevel
    )
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableImageBuffer;
      if (possibleResult != null)
        return possibleResult;

      var planes = source.Planes._AsSerializable();
      return new _SerializableImageBuffer(source.Format, planes, compressionLevel);
    }

    internal static _SerializableImagePlanes _AsSerializable(this IImagePlanes source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableImagePlanes;
      if (possibleResult != null)
        return null;

      int count = source.Count;
      var result = new _SerializableImagePlane[count];
      for (int i = 0; i < count; i++)
        result[i] = source[i]._AsSerializable();

      return new _SerializableImagePlanes(result);
    }

    internal static _SerializableImagePlane _AsSerializable(this IImagePlane source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableImagePlane;
      if (possibleResult != null)
        return possibleResult;

      var result =
        new _SerializableImagePlane
        (
          source.Data,
          source.PixelWidth,
          source.PixelHeight,
          source.BytesPerRow,
          source.BytesPerPixel
        );

      return result;
    }
  }
}
