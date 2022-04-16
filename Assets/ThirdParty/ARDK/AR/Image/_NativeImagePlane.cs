// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Niantic.ARDK.AR.Image
{
  internal sealed class _NativeImagePlane:
    IImagePlane
  {
    private readonly IntPtr _nativeHandle;
    internal readonly UInt64 _planeIndex;

    private NativeArray<byte> _data;

    internal _NativeImagePlane(IntPtr nativeHandle, int planeIndex)
    {
      _nativeHandle = nativeHandle;
      _planeIndex = (UInt64)planeIndex;
    }

    public int PixelWidth
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARImage_GetWidthOfPlane(_nativeHandle, _planeIndex);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    public int PixelHeight
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARImage_GetHeightOfPlane(_nativeHandle, _planeIndex);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    public int BytesPerRow
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return checked((int)_NARImage_GetBytesPerRowOfPlane(_nativeHandle, _planeIndex));

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }

    public int BytesPerPixel
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return checked((int)_NARImage_GetBytesPerPixelOfPlane(_nativeHandle, _planeIndex));

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
    }


    public NativeArray<byte> Data
    {
      get
      {
        unsafe
        {
          if (!_data.IsCreated)
          {
            var dataAddress = _GetBaseDataAddress().ToPointer();
            var length = BytesPerRow * PixelHeight;

            _data =
              NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>
              (
                dataAddress,
                length,
                Allocator.None
              );
          }

          return _data;
        }
      }
    }

    private IntPtr _GetBaseDataAddress()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _NARImage_GetBaseAddressOfPlane(_nativeHandle, _planeIndex);

      #pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
      #pragma warning restore 0162
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARImage_GetBaseAddressOfPlane
    (
      IntPtr nativeHandle,
      UInt64 planeIndex
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NARImage_GetWidthOfPlane(IntPtr nativeHandle, UInt64 planeIndex);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NARImage_GetHeightOfPlane(IntPtr nativeHandle, UInt64 planeIndex);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARImage_GetBytesPerRowOfPlane
    (
      IntPtr nativeHandle,
      UInt64 planeIndex
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARImage_GetBytesPerPixelOfPlane
    (
      IntPtr nativeHandle,
      UInt64 planeIndex
    );
  }
}
