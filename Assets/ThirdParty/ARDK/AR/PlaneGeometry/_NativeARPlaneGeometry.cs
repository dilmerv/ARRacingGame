// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.PlaneGeometry
{
  internal sealed class _NativeARPlaneGeometry:
    IARPlaneGeometry
  {
    // Estimated unmanaged memory: assuming ~50 vertices (3 floats each)
    private const long _MemoryPressure = (50L * 3L * 4L);

    private IntPtr _nativeHandle;

    internal _NativeARPlaneGeometry(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _nativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARPlaneGeometry_Release(nativeHandle);
    }

    ~_NativeARPlaneGeometry()
    {
      GC.RemoveMemoryPressure(_MemoryPressure);
      _ReleaseImmediate(_nativeHandle);
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _nativeHandle = IntPtr.Zero;

        GC.RemoveMemoryPressure(_MemoryPressure);
        _ReleaseImmediate(nativeHandle);
      }
    }

#if UNITY_IOS
    private ReadOnlyCollection<Vector3> _vertices;
#endif
    public ReadOnlyCollection<Vector3> Vertices
    {
      get
      {
#if UNITY_IOS
        if (_vertices != null)
          return _vertices;

        var vertices = EmptyArray<Vector3>.Instance;
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            var obtained = _NARPlaneGeometry_GetVertices(_nativeHandle, vertices.Length, vertices);
            if (obtained == vertices.Length)
              break;

            vertices = new Vector3[Math.Abs(obtained)];
          }
        }

        var vertexCount = vertices.Length;
        if (vertexCount == 0)
          _vertices = EmptyReadOnlyCollection<Vector3>.Instance;
        else
        {
          for (var i = 0; i < vertexCount; i++)
          {
            var vertex = vertices[i];
            vertices[i] = NARConversions.FromNARToUnity(vertex);
          }

          _vertices = new ReadOnlyCollection<Vector3>(vertices);
        }

        return _vertices;
#else
        return EmptyReadOnlyCollection<Vector3>.Instance;
#endif
      }
    }

#if UNITY_IOS
    private ReadOnlyCollection<Vector2> _textureCoordinates;
#endif
    public ReadOnlyCollection<Vector2> TextureCoordinates
    {
      get
      {
#if UNITY_IOS
        if (_textureCoordinates != null)
          return _textureCoordinates;

        var textureCoordinates = EmptyArray<Vector2>.Instance;
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            var obtained =
              _NARPlaneGeometry_GetTextureCoordinates
              (
                _nativeHandle,
                textureCoordinates.Length,
                textureCoordinates
              );

            if (obtained == textureCoordinates.Length)
              break;

            textureCoordinates = new Vector2[Math.Abs(obtained)];
          }
        }

        if (textureCoordinates.Length == 0)
          _textureCoordinates = EmptyReadOnlyCollection<Vector2>.Instance;
        else
          _textureCoordinates = new ReadOnlyCollection<Vector2>(textureCoordinates);

        return _textureCoordinates;
#else
        return EmptyReadOnlyCollection<Vector2>.Instance;
#endif
      }
    }

#if UNITY_IOS
    private ReadOnlyCollection<Int16> _triangleIndices;
#endif
    public ReadOnlyCollection<Int16> TriangleIndices
    {
      get
      {
#if UNITY_IOS
        if (_triangleIndices != null)
          return _triangleIndices;

        var triangleIndices = EmptyArray<Int16>.Instance;
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            var obtained =
              _NARPlaneGeometry_GetTriangleIndices
              (
                _nativeHandle,
                triangleIndices.Length,
                triangleIndices
              );

            if (obtained == triangleIndices.Length)
              break;

            triangleIndices = new Int16[Math.Abs(obtained)];
          }
        }

        if (triangleIndices.Length == 0)
          _triangleIndices = EmptyReadOnlyCollection<Int16>.Instance;
        else
        _triangleIndices = new ReadOnlyCollection<Int16>(triangleIndices);

        return _triangleIndices;
#else
        return EmptyReadOnlyCollection<Int16>.Instance;
#endif
      }
    }

    private ReadOnlyCollection<Vector3> _boundaryVertices;
    public ReadOnlyCollection<Vector3> BoundaryVertices
    {
      get
      {
        if (_boundaryVertices != null)
          return _boundaryVertices;

        var boundaryVertices = EmptyArray<Vector3>.Instance;
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            int obtained = 0;

            _NARPlaneGeometry_GetBoundaryVertices
            (
              _nativeHandle,
              boundaryVertices.Length,
              boundaryVertices
            );

            // If at the first time we have the wrong size, we resize or vector and try again.
            // This loop should execute at most 2 times.
            if (obtained == boundaryVertices.Length)
              break;

            boundaryVertices = new Vector3[Math.Abs(obtained)];
          }
        }

        // Initialize cache
        var boundaryVertexCount = boundaryVertices.Length;
        if (boundaryVertexCount == 0)
          _boundaryVertices = EmptyReadOnlyCollection<Vector3>.Instance;
        else
        {
          for (var i = 0; i < boundaryVertexCount; i++)
          {
            var boundaryVertex = boundaryVertices[i];
            boundaryVertices[i] = NARConversions.FromNARToUnity(boundaryVertex);
          }

          _boundaryVertices = new ReadOnlyCollection<Vector3>(boundaryVertices);
        }

        return _boundaryVertices;
      }
    }

    /// Gets a fully managed representation of this object. This means that a copy will be made.
    internal _SerializableARPlaneGeometry _AsSerializable()
    {
      return
        new _SerializableARPlaneGeometry
        (
          Vertices,
          TextureCoordinates,
          TriangleIndices,
          BoundaryVertices
        );
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARPlaneGeometry_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NARPlaneGeometry_GetVertices
    (
      IntPtr nativeHandle,
      int verticesLength,
      Vector3[] vertices
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NARPlaneGeometry_GetTextureCoordinates
    (
      IntPtr nativeHandle,
      int textureCoordinatesLength,
      Vector2[] textureCoordinates
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NARPlaneGeometry_GetTriangleIndices
    (
      IntPtr nativeHandle,
      int triangleIndicesLength,
      Int16[] triangleIndices
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NARPlaneGeometry_GetBoundaryVertices
    (
      IntPtr nativeHandle,
      int boundaryVerticesLength,
      Vector3[] boundaryVertices
    );
  }
}
