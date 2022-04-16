// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Depth.Generators
{
  /// This class takes in a frame of DepthBuffer raw data and generates a point cloud based
  /// on it and the camera intrinsics.
  public sealed class DepthPointCloudGenerator:
    IDisposable
  {
    private const string GENERATOR_SHADER_NAME = "DepthPointCloudGenerator";

    private static readonly ComputeShader _pointCloudShader =
      (ComputeShader)Resources.Load(GENERATOR_SHADER_NAME);

    private const string KERNEL_NAME = "Generate";

#region Shader Handles
    private static readonly int DEPTH_BUFFER_WIDTH_HANDLE =
      Shader.PropertyToID("DepthBufferWidth");

    private static readonly int DEPTH_BUFFER_HEIGHT_HANDLE =
      Shader.PropertyToID("DepthBufferHeight");

    private static readonly int INTRINSICS_HANDLE =
      Shader.PropertyToID("Intrinsics");

    private static readonly int POSE_HANDLE =
      Shader.PropertyToID("CameraToWorld");

    private static readonly int POINT_CLOUD_HANDLE =
      Shader.PropertyToID("PointCloud");

    private static readonly int DEPTH_HANDLE =
      Shader.PropertyToID("Depth");

    private static readonly int VERTICAL_OFFSET_HANDLE =
      Shader.PropertyToID("VerticalOffsetPerMeter");

#endregion

    [Serializable]
    public sealed class Settings
    {
      public bool IsEnabled;

      /// This is a fixup scale value that pulls points downward larger amounts as they go further
      /// from the camera's position. At the present it is necessary to compensate for the fact that
      /// current models believe the floor to be a large bowl, with you standing in the centermost,
      /// deepest part.
      public float VerticalOffsetPerMeter = -0.025f;

      /// If true, the cloud will be fit to the specified target pixel width and height
      public bool FitToViewport;

      /// The pixel width of the target display that this generator should create points for
      public int TargetWidth;

      /// The pixel height of the target display that this generator should create points for
      public int TargetHeight;

      /// If true, the depth cloud will be interpolated to match the current ARFrame
      public bool Interpolate;

      /// Value passed into IDepthBuffer.Interpolation calls. See IDepthBuffer documentation
      public float BackProjectionDistance = AwarenessParameters.DefaultBackProjectionDistance;

      public Settings Copy()
      {
        return
          new Settings
          {
            IsEnabled = this.IsEnabled,
            VerticalOffsetPerMeter = this.VerticalOffsetPerMeter
          };
      }
    }

    private Settings _settings;
    private int _kernel;

    private uint _kernelThreadsX;
    private uint _kernelThreadsY;

    private int _size = 0;
    private ComputeBuffer _pointCloudBuffer;
    private ComputeBuffer _depthComputeBuffer;
    private Vector3[] _pointCloud;

    /// The output of this class. Contains a point for every point on the DepthBuffer, stored
    /// as a flat two-dimensional array.
    public Vector3[] PointCloud
    {
      get { return _pointCloud; }
      private set { _pointCloud = value; }
    }

    /// Constructs a new generator.
    /// @param settings User-controlled settings specific to this generator. Cached.
    /// @param depthBuffer Depth buffer used to initialize the generator. Not cached.
    public DepthPointCloudGenerator(Settings settings)
    {
      if (!SystemInfo.supportsComputeShaders)
      {
        var msg =
          "DepthPointCloudGenerator failed to initialize because compute shaders are not " +
          "supported on this platform.";

#if UNITY_EDITOR
        msg +=
          " Go to the Edit > Project Settings > Player tab and look in the Other Settings " +
          "section to enable Metal Editor Support.";
#endif

        ARLog._Error(msg);
        return;
      }

      _settings = settings;
      _kernel = _pointCloudShader.FindKernel(KERNEL_NAME);

      // Constant value
      _pointCloudShader.SetFloat(VERTICAL_OFFSET_HANDLE, _settings.VerticalOffsetPerMeter);

      uint kernelThreadsZ;
      _pointCloudShader.GetKernelThreadGroupSizes(
          _kernel,
          out _kernelThreadsX,
          out _kernelThreadsY,
          out kernelThreadsZ
        );

    }

    ~DepthPointCloudGenerator()
    {
      ARLog._Error("DepthPointCloudGenerator must be released by calling Dispose().");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var depthBuffer = _depthComputeBuffer;
      if (depthBuffer != null)
      {
        _depthComputeBuffer = null;
        depthBuffer.Release();
      }

      var pointCloudBuffer = _pointCloudBuffer;
      if (pointCloudBuffer != null)
      {
        _pointCloudBuffer = null;
        pointCloudBuffer.Release();
      }
    }

    /// <summary>
    /// Uses the compute shaders to generate the a point cloud from the depth image. Each pixel
    /// in the depth image will be turned into a 3d point in world space (defined by the
    /// inverseViewMat and the focal length). Each 3d point can optionally be categorized.
    /// </summary>
    /// @param depthBuffer A depth buffer with which to generate a point cloud
    /// @returns A point cloud based on the depth buffer
    public IDepthPointCloud GeneratePointCloud(IDepthBuffer rawDepthBuffer, IARCamera camera)
    {
      // Handle buffer manipulation
      var depthBuffer = rawDepthBuffer.RotateToScreenOrientation();

      IDepthBuffer interpolated = null, fit = null;
      if (_settings.Interpolate)
      {
        interpolated =
          depthBuffer.Interpolate
          (
            camera,
            _settings.TargetWidth,
            _settings.TargetHeight,
            _settings.BackProjectionDistance
          );

        depthBuffer.Dispose();
        depthBuffer = interpolated;
      }

      if (_settings.FitToViewport)
      {
        fit =
          depthBuffer.FitToViewport
          (
            _settings.TargetWidth,
            _settings.TargetHeight
          );

        depthBuffer.Dispose();
        interpolated?.Dispose();

        depthBuffer = fit;
      }

      int size = (int)(depthBuffer.Width * depthBuffer.Height);
      if(_size != size ){

        // Setting up input data buffer
        _depthComputeBuffer =
          new ComputeBuffer
          (
            (int)(depthBuffer.Width * depthBuffer.Height),
            Marshal.SizeOf(typeof(float))
          );

        _pointCloudShader.SetBuffer(_kernel, DEPTH_HANDLE, _depthComputeBuffer);

        // Setting up output data buffer
        _pointCloudBuffer =
          new ComputeBuffer
          (
            (int)(depthBuffer.Width * depthBuffer.Height),
            Marshal.SizeOf(typeof(Vector3))
          );

        _pointCloud = new Vector3[depthBuffer.Width * depthBuffer.Height];
        _pointCloudBuffer.SetData(_pointCloud);
        _pointCloudShader.SetBuffer(_kernel, POINT_CLOUD_HANDLE, _pointCloudBuffer);
        _size = size;
      }

      _pointCloudShader.SetInt(DEPTH_BUFFER_WIDTH_HANDLE, (int)depthBuffer.Width);
      _pointCloudShader.SetInt(DEPTH_BUFFER_HEIGHT_HANDLE, (int)depthBuffer.Height);
      _pointCloudShader.SetVector(INTRINSICS_HANDLE, depthBuffer.Intrinsics);
      _pointCloudShader.SetMatrix(POSE_HANDLE, depthBuffer.ViewMatrix.inverse);

      // update the depth image
      _depthComputeBuffer.SetData(depthBuffer.Data);

      // We can't assume the width/height are evenly divisible by thread count.
      // So, to get around this, we'll oversize if needed and then no-op in the shader for
      // any excess workers
      var threadGroupPaddingX = depthBuffer.Width % _kernelThreadsX > 0 ? 1 : 0;
      var threadGroupPaddingY = depthBuffer.Height % _kernelThreadsY > 0 ? 1 : 0;
      var threadGroupX = (int)(depthBuffer.Width / _kernelThreadsX) + threadGroupPaddingX;
      var threadGroupY = (int)(depthBuffer.Height / _kernelThreadsY) + threadGroupPaddingY;

      // TODO : Profile this Dispatch & Get. Maybe we can avoid having the main thread wait?
      // calculate the point cloud
      _pointCloudShader.Dispatch(_kernel, threadGroupX, threadGroupY, 1);

      // copy the point cloud into the local buffer
      _pointCloudBuffer.GetData(_pointCloud);

      var retVal = new _DepthPointCloud
        (
          new ReadOnlyCollection<Vector3>(_pointCloud),
          depthBuffer.Width,
          depthBuffer.Height
        );

      depthBuffer.Dispose();
      return retVal;
    }
  }
}
