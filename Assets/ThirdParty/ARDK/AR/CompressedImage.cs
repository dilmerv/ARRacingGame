// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR
{
  [Serializable]
  public sealed class CompressedImage
  {
    public byte[] CompressedData { get; set; }
  }
}
