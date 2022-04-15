using System;

using Niantic.ARDK.AR.Awareness.Depth.Effects;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.AR.Depth.Effects
{
  public sealed class DepthMeshOccluder : IDisposable
  {
    private const string ShaderName = "ARDK/Effects/DepthMeshOccluder";
    private static readonly Shader OcclusionShader = Shader.Find(ShaderName);

    private readonly UnityEngine.Camera _targetCamera;
    private readonly CommandBuffer _commandBuffer;
    private readonly UnityEngine.Mesh _landscapeMesh, _portraitMesh;
    private readonly Material _material;

    public enum ColorMask
    {
      None = 0, // RGBA: 0000
      Depth = 5, // RGBA: 0101
      UV = 11, // RGBA: 1011
      All = 15, // RGBA: 1111
    }
    private ColorMask _colorMask = ColorMask.None;

    public ColorMask DebugColorMask
    {
      get => _colorMask;
      set
      {
        _colorMask = value;
        _material.SetFloat(PropertyBindings.DebugColorMask, (int)_colorMask);
      }
    }

    /// Enables or disables the occlusion effect.
    public bool Enabled
    {
      get => _isEnabled;
      set
      {
        if (_isEnabled == value)
          return;
        
        _isEnabled = value;
        
        if (_isEnabled)
          DepthMeshBufferHelper.AddCommandBuffer(_targetCamera, _commandBuffer);
        else
          DepthMeshBufferHelper.RemoveCommandBuffer(_targetCamera, _commandBuffer);
      }
    }
    private bool _isEnabled;

    public Texture SuppressionTexture
    {
      set => _material.SetTexture(PropertyBindings.DepthSuppressionMask, value);
    }
    
    public Matrix4x4 DepthTransform
    {
      set => _material.SetMatrix(PropertyBindings.DepthTransform, value);
    }
    
    public Matrix4x4 SemanticsTransform
    {
      set => _material.SetMatrix(PropertyBindings.SemanticsTransform, value);
    }

    private ScreenOrientation _orientation;

    public ScreenOrientation Orientation
    {
      set
      {
        if (_orientation == value)
          return;

        _orientation = value;
        _commandBuffer.Clear();
        _commandBuffer.DrawMesh(_orientation == ScreenOrientation.Portrait ||
          _orientation == ScreenOrientation.PortraitUpsideDown
            ? _portraitMesh
            : _landscapeMesh, 
          Matrix4x4.identity, _material);
      }
    }

    public DepthMeshOccluder
    (
      UnityEngine.Camera targetCamera, 
      Texture depthTexture, 
      Resolution meshResolution
    )
    {
      if (depthTexture == null)
      {
        ARLog._Error("No Depth Texture provided. Occlusion Mesh Not Created");
        return;
      }
      
      if (targetCamera == null)
      {
        ARLog._Error("No Target Camera provided. Occlusion Mesh Not Created");
        return;
      }

      // Calculate the aspect ratio
      var aspect = (float)meshResolution.width / meshResolution.height;
      
      // Create a landscape version of the mesh using the specified resolution
      _landscapeMesh = aspect > 1.0f
        ? CreateMesh(meshResolution.width, meshResolution.height)
        : CreateMesh(meshResolution.height, meshResolution.width);
      
      // Create a portrait version of the mesh using the specified resolution
      _portraitMesh = aspect < 1.0f
        ? CreateMesh(meshResolution.width, meshResolution.height)
        : CreateMesh(meshResolution.height, meshResolution.width);

      _material = new Material(OcclusionShader);
      _material.SetTexture(PropertyBindings.DepthChannel, depthTexture);
      _material.SetFloat(PropertyBindings.DebugColorMask, (int)_colorMask);
      
      // Allocate the command buffer for drawing the mesh
      _commandBuffer = new CommandBuffer();
      _commandBuffer.name = "DepthMeshOccluder";
      
      // Assign target camera
      _targetCamera = targetCamera;
      
      // Activate
      Orientation = Screen.orientation;
      Enabled = true;
    }

    /// Sets the range for depth values. This should be invoked in case the GPU depth buffer
    /// contains normalized values that need to be scaled before use.
    /// @param isScalingEnabled Whether the depth texture contains normalized depth values.
    /// @param minDepth Minimum depth value (depth near).
    /// @param maxDepth Maximum depth value (depth far).
    // TODO: do we need to explicitly set these, or defaults to 0-1?
    public void SetScaling(float minDepth, float maxDepth)
    {
      // Set depth range
      _material.SetFloat(PropertyBindings.DepthScaleMin, minDepth);
      _material.SetFloat(PropertyBindings.DepthScaleMax, maxDepth);
    }

    public void Dispose()
    {
      Enabled = false;
      
      // Release the command buffer
      _commandBuffer?.Dispose();
      
      // Release other resources
      UnityEngine.Object.Destroy(_material);
      UnityEngine.Object.Destroy(_portraitMesh);
      UnityEngine.Object.Destroy(_landscapeMesh);
    }

    private static UnityEngine.Mesh CreateMesh(int width, int height)
    {
      var numPoints = width * height;
      var vertices = new Vector3[numPoints];
      var uvs = new Vector2[numPoints];
      var numTriangles = 2 * (width - 1) * (height - 1); // just under 2 triangles per point, total

      // Map vertex indices to triangle in triplets
      var triangleIdx = new int[numTriangles * 3]; // 3 vertices per triangle
      var startIndex = 0;

      for (var i = 0; i < width * height; ++i)
      {
        var h = i / width;
        var w = i % width;
        uvs[i] = new Vector2((float)w / (width - 1), (float)h / (height - 1));

        if (h == height - 1 || w == width - 1)
          continue;

        // Triangle indices are counter-clockwise to face you
        triangleIdx[startIndex] = i;
        triangleIdx[startIndex + 1] = i + width;
        triangleIdx[startIndex + 2] = i + width + 1;
        triangleIdx[startIndex + 3] = i;
        triangleIdx[startIndex + 4] = i + width + 1;
        triangleIdx[startIndex + 5] = i + 1;
        startIndex += 6;
      }

      var mesh = new UnityEngine.Mesh();
      mesh.MarkDynamic();
      mesh.indexFormat = width * height >= 65534 ? IndexFormat.UInt32 : IndexFormat.UInt16;
      mesh.vertices = vertices;
      mesh.uv = uvs;
      mesh.triangles = triangleIdx;

      return mesh;
    }
  }
}
