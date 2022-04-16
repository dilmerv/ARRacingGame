// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Niantic.ARDK.AR
{
  // TODO: In newer versions of .NET we could just use IReadOnlyCollection<ImagePlane>
  // instead of creating this entire interface.
  public interface IImagePlanes:
    IEnumerable<IImagePlane>
  {
    int Count { get; }
    IImagePlane this[int planeIndex] { get; }
  }
}
