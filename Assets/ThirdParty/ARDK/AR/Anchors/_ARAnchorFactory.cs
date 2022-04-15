// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  internal static class _ARAnchorFactory
  {
    internal static IARAnchor _Create(Matrix4x4 transform)
    {
      return _CreateForPlatform(transform);
    }

    private static IARAnchor _CreateForPlatform(Matrix4x4 transform)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var nativeTransform =
          _Convert.Matrix4x4ToInternalArray(NARConversions.FromUnityToNAR(transform));

        var nativeHandle = _NARAnchor_Init(nativeTransform);
        if (nativeHandle == IntPtr.Zero)
          throw new InvalidOperationException("Unable to create anchor.");

        ARLog._DebugFormat
        (
          "Successfully created a new native ARAnchor with handle: {0}",
          false,
          nativeHandle
        );

        return _FromNativeHandle(nativeHandle);
      }
#pragma warning disable 0162
      else
      {
        var identifier = Guid.NewGuid();

        ARLog._DebugFormat
        (
          "Creating new _SerializableARBaseAnchor with identifier: {0}",
          false,
          identifier
        );

        return new _SerializableARBaseAnchor(transform, identifier);
      }
#pragma warning restore 0162
    }

    private static _WeakValueDictionary<IntPtr, _NativeARAnchor> _allAnchors =
      new _WeakValueDictionary<IntPtr, _NativeARAnchor>();

    internal static _NativeARAnchor _FromNativeHandle(IntPtr nativeHandle)
    {
      _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _allAnchors);

      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle");

      var cppAddress = _GetCppAddress(nativeHandle);

      var result = _allAnchors.TryGetValue(cppAddress);
      if (result != null)
      {
        // We already have an existing Anchor for the same C++ address.
        // We need to release the nativeHandle, as this is a different handle for the same object.
        _NativeARAnchor._ReleaseImmediate(nativeHandle);

        return result;
      }

      result = _allAnchors.GetOrAdd(cppAddress, (_) => _CreateFromHandle(nativeHandle));

      // If an existing anchor is found (that is, got added just after our TryGetValue but
      // before GetOrAdd) we need to release the nativeHandle as it is a duplicate to the same
      // C++ object.
      if (result._NativeHandle != nativeHandle)
        _NativeARAnchor._ReleaseImmediate(nativeHandle);

      return result;
    }

    [ThreadStatic]
    internal static AnchorType _testOnly_DefaultAnchorType;

    private static _NativeARAnchor _CreateFromHandle(IntPtr nativeHandle)
    {
      AnchorType anchorType;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        anchorType = (AnchorType)_NARAnchor_GetAnchorType(nativeHandle);
      #pragma warning disable 0162
      else
        anchorType = _testOnly_DefaultAnchorType;
      #pragma warning restore 0162


      switch (anchorType)
      {
        case AnchorType.Base: return new _NativeARBaseAnchor(nativeHandle);
        case AnchorType.Image: return new _NativeARImageAnchor(nativeHandle);
        case AnchorType.Plane: return new _NativeARPlaneAnchor(nativeHandle);
      }

      throw new InvalidEnumArgumentException("Unknown AnchorType value: " + anchorType);
    }

    internal static void _RemoveFromCache(_NativeARAnchor anchor)
    {
      _allAnchors.Remove(_GetCppAddress(anchor._NativeHandle));
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARAnchor_Init(float[] transform);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARAnchor_GetAnchorType(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARAnchor_GetCppAddress(IntPtr nativeHandle);

    private static IntPtr _GetCppAddress(IntPtr nativeHandle)
    {
#pragma warning disable 0162
      if (NativeAccess.Mode != NativeAccess.ModeType.Native)
        return nativeHandle;
#pragma warning restore 0162

      return _NARAnchor_GetCppAddress(nativeHandle);
    }
  }
}
