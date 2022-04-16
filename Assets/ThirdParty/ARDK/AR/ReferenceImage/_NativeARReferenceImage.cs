// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.ReferenceImage
{
  internal sealed class _NativeARReferenceImage:
    IARReferenceImage
  {
    static _NativeARReferenceImage()
    {
      Platform.Init();
    }

    private static readonly _WeakValueDictionary<IntPtr, _NativeARReferenceImage> _allImages =
      new _WeakValueDictionary<IntPtr, _NativeARReferenceImage>();

    internal static _NativeARReferenceImage _FromNativeHandle(IntPtr nativeHandle)
    {
      _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _allImages);
      
      var cppAddress = _NARReferenceImage_GetCppAddress(nativeHandle);

      var possibleResult = _allImages.TryGetValue(cppAddress);
      if (possibleResult != null)
      {
        // An existing C# wrapper already exists for the same C++ address.
        // So, we release the new handle but return the wrapper to the previous handle, as it will
        // still safely point to the right C++ object.

        _ReleaseImmediate(nativeHandle);
        return possibleResult;
      }

      var result =
        _allImages.GetOrAdd(cppAddress, (_) => new _NativeARReferenceImage(nativeHandle));

      if (result._nativeHandle != nativeHandle)
      {
        // We got in a very rare situation. After our TryGetValue, another thread actually did add
        // a wrapper for the same cppAddress we are using. This means there are 2 handles for the
        // same C++ object, and ours is not going to be used. So we should release it immediately.
        _ReleaseImmediate(nativeHandle);
      }

      return result;
    }

    // Estimated memory usage of a reference image: 25 char string + 2 floats + 0.5MB (image)
    private const long _MemoryPressure = (25L * 8L) + (2L * 4L) + (512L * 1024L);

    private _NativeARReferenceImage(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentNullException("nativeHandle can't be Zero.", "nativeHandle");

      _nativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    ~_NativeARReferenceImage()
    {
      _ReleaseImmediate(_nativeHandle);
      
      GC.RemoveMemoryPressure(_MemoryPressure);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARReferenceImage_Release(nativeHandle);
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
      
      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _nativeHandle = IntPtr.Zero;

        _ReleaseImmediate(nativeHandle);
        
        GC.RemoveMemoryPressure(_MemoryPressure);
      }
    }

    private IntPtr _nativeHandle;
    internal IntPtr _NativeHandle
    {
      get { return _nativeHandle;}
    }

    public string Name
    {
      get
      {
        var stringBuilder = new StringBuilder(25);
        
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARReferenceImage_GetName(_nativeHandle, stringBuilder, stringBuilder.Capacity);
        
        return stringBuilder.ToString();
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARReferenceImage_SetName(_nativeHandle, value);
      }
    }

    public Vector2 PhysicalSize
    {
      get
      {
        var rawPhysicalSize = new float[2];
        
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARReferenceImage_GetPhysicalSize(_nativeHandle, rawPhysicalSize);
        
        return new Vector2(rawPhysicalSize[0], rawPhysicalSize[1]);
      }
    }
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARReferenceImage_Release(IntPtr nativeHandle);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARReferenceImage_GetCppAddress(IntPtr nativeHandle);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARReferenceImage_GetName
    (
      IntPtr nativeHandle,
      StringBuilder outName,
      int maxNameSize
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARReferenceImage_SetName(IntPtr nativeHandle, string name);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARReferenceImage_GetPhysicalSize
    (
      IntPtr nativeHandle,
      float[] outPhysicalSize
    );
  }
}
