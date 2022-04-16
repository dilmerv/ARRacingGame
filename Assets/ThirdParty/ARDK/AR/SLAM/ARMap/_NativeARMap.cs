// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.SLAM
{
  internal sealed class _NativeARMap:
    IARMap
  {
    // Used to inform the C# GC that there is managed memory held by this object
    // guid + transform
    private const long _MemoryPressure = (16L * 1L) + (16L * 4L);

    static _NativeARMap()
    {
      Platform.Init();
    }

    private static readonly _WeakValueDictionary<_CppAddressAndScale, _NativeARMap> _allMaps =
      new _WeakValueDictionary<_CppAddressAndScale, _NativeARMap>();

    internal static _NativeARMap _FromNativeHandle(IntPtr nativeHandle, float worldScale)
    {
      _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _allMaps);
      
      var cppAddress = _NARMapViz_GetCppAddress(nativeHandle);
      var handleAndScale = new _CppAddressAndScale(cppAddress, worldScale);

      var result = _allMaps.TryGetValue(handleAndScale);
      if (result != null)
      {
        // We found an existing C# wrapper for the actual C++ object. Let's release the new
        // nativeHandle and return the existing wrapper.

        _ReleaseImmediate(nativeHandle);
        return result;
      }

      Func<_CppAddressAndScale, _NativeARMap> creator =
        (_) => new _NativeARMap(nativeHandle, worldScale);

      result = _allMaps.GetOrAdd(handleAndScale, creator);

      if (result._nativeHandle != nativeHandle)
      {
        // We got on the very odd situation where just after a TryGetValue another value was added.
        // We should release our handle immediately. Then, we can return the result as it is valid.
        _ReleaseImmediate(nativeHandle);
      }

      return result;
    }

    private _NativeARMap(IntPtr nativeHandle, float worldScale = 1.0f)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      WorldScale = worldScale;

      GC.AddMemoryPressure(_MemoryPressure);
    }

    internal static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARMapViz_Release(nativeHandle);
    }

    ~_NativeARMap()
    {
      _ReleaseImmediate(_nativeHandle);
      GC.RemoveMemoryPressure(_MemoryPressure);
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

    public float WorldScale { get; private set; }

    public bool CanGetNativeHandle
    {
      get { return true; }
    }

    private IntPtr _nativeHandle;
    public IntPtr NativeHandle
    {
      get { return _nativeHandle; }
    }

    public Guid Identifier
    {
      get
      {
        Guid result;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARMapViz_GetIdentifier(_nativeHandle, out result);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        return result;
      }
    }

    public Matrix4x4 Transform
    {
      get
      {
        var nativeTransform = new float[16];

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARMapViz_GetTransform(_nativeHandle, nativeTransform);
        #pragma warning disable 0162
        else
          throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162

        var transform =
          NARConversions.FromNARToUnity(_Convert.InternalToMatrix4x4(nativeTransform));

        _Convert.ApplyScale(ref transform, WorldScale);

        return transform;
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMapViz_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARMapViz_GetCppAddress(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMapViz_GetIdentifier(IntPtr nativeHandle, out Guid identifier);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMapViz_GetTransform(IntPtr nativeHandle, float[] outTransform);
  }
}
