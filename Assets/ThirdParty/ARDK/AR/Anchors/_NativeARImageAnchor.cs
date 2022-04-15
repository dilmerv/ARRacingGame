// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.ReferenceImage;
using Niantic.ARDK.Internals;

namespace Niantic.ARDK.AR.Anchors
{
  internal sealed class _NativeARImageAnchor:
    _NativeARAnchor,
    IARImageAnchor
  {
    public _NativeARImageAnchor(IntPtr nativeHandle):
      base(nativeHandle)
    {
    }

    public override AnchorType AnchorType
    {
      get { return AnchorType.Image; }
    }

    internal _NativeARReferenceImage ReferenceImage
    {
      get
      {
        #pragma warning disable 0162
        if (NativeAccess.Mode != NativeAccess.ModeType.Native)
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        var nativeHandle = _NARImageAnchor_GetReferenceImage(_NativeHandle);
        return _NativeARReferenceImage._FromNativeHandle(nativeHandle);
      }
    }

    IARReferenceImage IARImageAnchor.ReferenceImage
    {
      get { return ReferenceImage; }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARImageAnchor_GetReferenceImage(IntPtr nativeHandle);
  }
}
