using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Rendering;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Depth
{
  public class DepthBufferProcessor: 
    AwarenessBufferProcessor<IDepthBuffer>,
    IDepthBufferProcessor
  {
    // The currently active AR session
    private IARSession _session;

    // The render target descriptor used to determine the viewport resolution
    private RenderTarget _viewport;

    #region Public API

    /// Allocates a new depth buffer processor. By default, the
    /// awareness buffer will be fit to the main camera's viewport.
    public DepthBufferProcessor()
    {
      _viewport = UnityEngine.Camera.main;
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
    }
    
    /// Allocates a new depth buffer processor.
    /// @param viewport Determines the target viewport to fit the awareness buffer to.
    public DepthBufferProcessor(RenderTarget viewport)
    {
      _viewport = viewport;
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
    }

    public float MinDepth
    {
      get => AwarenessBuffer?.NearDistance ?? float.PositiveInfinity;
    }

    public float MaxDepth
    {
      get => AwarenessBuffer?.FarDistance ?? float.PositiveInfinity;
    }
    
    /// Assigns a new render target descriptor for the depth processor.
    /// The render target defines the viewport attributes to correctly
    /// fit the depth buffer.
    public void AssignViewport(RenderTarget target)
    {
      _viewport = target;
    }

    /// <inheritdoc />
    public float GetDepth(int viewportX, int viewportY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return float.PositiveInfinity;

      var x = viewportX + 0.5f;
      var y = viewportY + 0.5f;
      var resolution = _viewport.GetResolution(Screen.orientation);
      var uv = new Vector4(x / resolution.width, y / resolution.height, 1.0f, 1.0f);

      // Sample the depth buffer
      return depthBuffer.Sample(uv, SamplerTransform);
    }

    /// <inheritdoc />
    public float GetDistance(int viewportX, int viewportY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return float.PositiveInfinity;

      var x = viewportX + 0.5f;
      var y = viewportY + 0.5f;
      var resolution = _viewport.GetResolution(Screen.orientation);
      var uv = new Vector4(x / resolution.width, y / resolution.height, 1.0f, 1.0f);

      // Sample the depth buffer
      var depth = depthBuffer.Sample(uv, SamplerTransform);

      // Retrieve point in camera space
      var pointRelativeToCamera = depth * BackProjectionTransform.MultiplyPoint(uv);

      // Calculate distance
      return pointRelativeToCamera.magnitude;
    }

    /// <inheritdoc />
    public Vector3 GetWorldPosition(int viewportX, int viewportY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return Vector3.zero;

      var x = viewportX + 0.5f;
      var y = viewportY + 0.5f;
      var resolution = _viewport.GetResolution(Screen.orientation);
      var uv = new Vector4(x / resolution.width, y / resolution.height, 1.0f, 1.0f);

      // Sample the depth buffer
      // The sampler transform may contain re-projection. We do this because
      // we need the depth value at the pixel predicted with interpolation.
      var depth = depthBuffer.Sample(uv, SamplerTransform);

      // Retrieve point in camera space
      var pointRelativeToCamera = depth * BackProjectionTransform.MultiplyPoint(uv);

      // Transform to world coordinates
      return CameraToWorldTransform.MultiplyPoint(pointRelativeToCamera);
    }

    /// <inheritdoc />
    public Vector3 GetSurfaceNormal(int viewportX, int viewportY)
    {
      var depthBuffer = AwarenessBuffer;
      if (depthBuffer == null)
        return Vector3.up;

      var resolution = _viewport.GetResolution(Screen.orientation);
      var viewportMax = Mathf.Max(resolution.width, resolution.height);
      var bufferMax = Mathf.Max((int)depthBuffer.Width, (int)depthBuffer.Height);
      var viewportDelta = Mathf.CeilToInt((float)viewportMax / bufferMax) + 1;

      // TODO: calculate normals without back-projection
      var a = GetWorldPosition(viewportX, viewportY);
      var b = GetWorldPosition(viewportX + viewportDelta, viewportY);
      var c = GetWorldPosition(viewportX, viewportY + viewportDelta);

      return Vector3.Cross(a - b, c - a).normalized;
    }
    
    public void CopyToAlignedTextureARGB32(ref Texture2D texture, ScreenOrientation orientation)
    {
      // Get a typed buffer
      IDepthBuffer depthBuffer = AwarenessBuffer;
      float max = depthBuffer.FarDistance;
      float min = depthBuffer.NearDistance;
      
      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Call base method
      CreateOrUpdateTextureARGB32
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what color needs to be written to that position?
        sampler: uv =>
        {
          // Sample raw depth from the buffer
          var depth = depthBuffer.Sample(uv, transform);
          
          // Normalize depth
          var val = (depth - min) / (max - min);
          
          // Copy to value to color channels
          return new Color(val, val, val, 1.0f);
        }
      );
    }
    
    public void CopyToAlignedTextureRFloat(ref Texture2D texture, ScreenOrientation orientation)
    {
      // Get a typed buffer
      IDepthBuffer depthBuffer = AwarenessBuffer;

      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Call base method
      CreateOrUpdateTextureRFloat
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what value needs to be written to that position?
        sampler: uv => depthBuffer.Sample(uv, transform)
      );
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;
    }

  #endregion

  #region Implementation

    private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;

      _session = args.Session;
      _session.FrameUpdated += OnFrameUpdated;
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      var frame = args.Frame;
      if (frame == null)
        return;

#if UNITY_EDITOR 
      var orientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
#else
      var orientation = Screen.orientation;
#endif
      
      ProcessFrame
      (
        buffer: frame.Depth,
        arCamera: frame.Camera,
        targetResolution: _viewport.GetResolution(forOrientation: orientation),
        targetOrientation: orientation
      );
    }

  #endregion
  }
}