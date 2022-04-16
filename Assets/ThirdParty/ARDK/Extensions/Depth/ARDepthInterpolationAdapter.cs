using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  /// <summary>
  /// A component that allows for dynamically setting the interpolation preference (whether to
  ///   apply interpolation that prefers near or far depths).
  /// Allows users to specify whether to track a particular virtual object (_occludee) to align
  ///   interpolation for, or the average depths of the entire depth buffer.
  /// </summary>
  [RequireComponent(typeof(Camera))]
  [RequireComponent(typeof(ARDepthManager))]
  public class ARDepthInterpolationAdapter : MonoBehaviour
  {
    public enum AdaptionMode
    {
      /// Take a few samples of the full buffer to
      /// determine the closest occluder on the screen.
      SampleFullScreen = 0,
      
      // Sample the sub-region of the buffer that is directly over
      // the main CG object, to determine the distance of its occluder
      // in the world.
      TrackOccludee = 1
    }
    
    private Camera _camera;
    private ARDepthManager _depthManager;
    
    [SerializeField]
    private AdaptionMode _mode = AdaptionMode.SampleFullScreen;

    public AdaptionMode Mode
    {
      get => _mode;
      set => _mode = value;
    }

    [SerializeField]
    private Renderer _occludee;

    private void Awake()
    {
      _camera = GetComponent<Camera>();
      _depthManager = GetComponent<ARDepthManager>();
      
      if (_mode  == AdaptionMode.TrackOccludee && _occludee == null)
      {
        ARLog._Error("Missing occludee renderer to track.");
        _mode = AdaptionMode.SampleFullScreen;
      }
    }

    private void OnEnable()
    {
      _depthManager.DepthBufferUpdated += OnDepthBufferUpdated;
    }

    private void OnDisable()
    {
      _depthManager.DepthBufferUpdated -= OnDepthBufferUpdated;
    }

    /// Sets the main occludee used to adjust interpolation preference for.
    /// @note This method changes the adaption mode setting.
    public void TrackOccludee(Renderer occludee)
    {
      if (occludee != null)
      {
        _occludee = occludee;
        _mode = AdaptionMode.TrackOccludee;
      }
    }

    private void OnDepthBufferUpdated(ContextAwarenessStreamUpdatedArgs<IDepthBuffer> args)
    {
      if (!args.IsKeyFrame)
        return;

      var processor = args.Sender;
      var depthBuffer = processor.AwarenessBuffer;
      var samplerTransform = processor.SamplerTransform;

      Vector2 center, extents;
      if (_mode == AdaptionMode.SampleFullScreen)
      {
        center = new Vector2(0.5f, 0.5f);
        extents = new Vector2(0.4f, 0.4f);
      }
      else 
        CalculateViewportRectangle(_occludee, _camera, out center, out extents);

      var depth = GetClosestDepth
      (
        depthBuffer,
        samplerTransform,
        center,
        extents
      );

      // Depth to non-linear
      processor.InterpolationPreference = 
        (1.0f - depth * AwarenessParameters.ZBufferParams.w) /
        (depth * AwarenessParameters.ZBufferParams.z);
    }
    
    /// Calculates the normalized bounds that encloses
    /// the provided renderer's pixels on the viewport.
    private static void CalculateViewportRectangle
    (
      Renderer forRenderer,
      Camera usingCamera,
      out Vector2 center,
      out Vector2 extents
    )
    {
      var bounds = forRenderer.bounds;
      var inCenter = bounds.center;
      var inExtents = bounds.extents;

      Vector3[] points =
      {
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x + inExtents.x, inCenter.y + inExtents.y, inCenter.z + inExtents.z)
        ),
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x + inExtents.x, inCenter.y + inExtents.y, inCenter.z - inExtents.z)
        ),
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x + inExtents.x, inCenter.y - inExtents.y, inCenter.z + inExtents.z)
        ),
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x + inExtents.x, inCenter.y - inExtents.y, inCenter.z - inExtents.z)
        ),
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x - inExtents.x, inCenter.y + inExtents.y, inCenter.z + inExtents.z)
        ),
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x - inExtents.x, inCenter.y + inExtents.y, inCenter.z - inExtents.z)
        ),
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x - inExtents.x, inCenter.y - inExtents.y, inCenter.z + inExtents.z)
        ),
        usingCamera.WorldToScreenPoint
        (
          new Vector3(inCenter.x - inExtents.x, inCenter.y - inExtents.y, inCenter.z - inExtents.z)
        ),
      };

      var xMin = float.MaxValue;
      var yMin = float.MaxValue;
      var xMax = float.MinValue;
      var yMax = float.MinValue;

      for (int i = 0; i < points.Length; i++)
      {
        var point = points[i];

        if (point.x < xMin)
          xMin = point.x;

        if (point.x > xMax)
          xMax = point.x;

        if (point.y < yMin)
          yMin = point.y;

        if (point.y > yMax)
          yMax = point.y;
      }

      var widthNormalized = (xMax - xMin) / Screen.width;
      var heightNormalized = (yMax - yMin) / Screen.height;
      extents = new Vector2(widthNormalized / 2.0f, heightNormalized / 2.0f);
      center = new Vector2
      (
        (xMin / Screen.width) + extents.x,
        (yMin / Screen.height) + extents.y
      );
    }

    /// Sparsely samples the specified subregion for the closest depth value.
    private static float GetClosestDepth
    (
      IDepthBuffer depthBuffer,
      Matrix4x4 transform,
      Vector2 center,
      Vector2 extents
    )
    {
      float depth = float.MaxValue;

      var startX = center.x - extents.x;
      var endX = center.x + extents.x;
      var stepX = extents.x * 0.2f;
      var horizontal = new Vector2(0.0f, center.y);

      for (horizontal.x = startX; horizontal.x <= endX; horizontal.x += stepX)
      {
        var sample = depthBuffer.Sample(horizontal, transform);
        if (sample < depth)
          depth = sample;
      }

      var startY = center.y - extents.y;
      var endY = center.y + extents.y;
      var stepY = extents.y * 0.2f;
      var vertical = new Vector2(center.x, 0.0f);

      for (vertical.y = startY; vertical.y <= endY; vertical.y += stepY)
      {
        var sample = depthBuffer.Sample(vertical, transform);
        if (sample < depth)
          depth = sample;
      }

      return depth;
    }
  }
}
