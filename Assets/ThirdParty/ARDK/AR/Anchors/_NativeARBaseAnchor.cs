// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Anchors
{
  internal sealed class _NativeARBaseAnchor:
    _NativeARAnchor
  {
    public _NativeARBaseAnchor(IntPtr nativeHandle):
      base(nativeHandle)
    {
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Base; }
    }
  }
}

