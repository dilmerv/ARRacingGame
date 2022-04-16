// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Image
{
  // Can't use [Serializable]. Need to provide a serializer.
  internal sealed class _SerializableImageBuffer:
    IImageBuffer
  {
    internal _SerializableImageBuffer
    (
      ImageFormat format,
      _SerializableImagePlanes planes,
      int compressionLevel
    )
    {
      if (planes == null)
        throw new ArgumentNullException(nameof(planes));

      Format = format;
      Planes = planes;
      CompressionLevel = compressionLevel;
    }

    public void Dispose()
    {
      var planes = Planes;
      if (planes != null)
      {
        Planes = null;
        planes._Dispose();
      }
    }

    public ImageFormat Format { get; private set; }
    public _SerializableImagePlanes Planes { get; private set; }
    
    public int CompressionLevel { get; private set; }

    IImagePlanes IImageBuffer.Planes
    {
      get { return Planes; }
    }
  }
}
