using System;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  /// Base class for components that extract information from IAwarenessBuffer objects
  /// to be used in game logic and rendering.
  public abstract class AwarenessBufferProcessor<TBuffer> 
    where TBuffer: class, IAwarenessBuffer, IDisposable
  {
    /// Event for when the context awareness feature stream has initialized and the 
    /// application received its initial frame.
    public event ArdkEventHandler<ContextAwarenessArgs<TBuffer>> AwarenessStreamBegan
    {
      add
      {
        _awarenessStreamBegan += value;
        if (_didReceiveFirstUpdate)
        {
          var args = new ContextAwarenessArgs<TBuffer>(this);
          value.Invoke(args);
        }
      }
      remove => _awarenessStreamBegan -= value;
    }
    private event ArdkEventHandler<ContextAwarenessArgs<TBuffer>> _awarenessStreamBegan;

    /// Alerts subscribers when either the contents of the awareness buffer
    /// or its sampler transform has changed.
    public event ArdkEventHandler<ContextAwarenessStreamUpdatedArgs<TBuffer>> AwarenessStreamUpdated; 

    /// The CPU copy of the latest awareness buffer
    public TBuffer AwarenessBuffer { get; private set; }

    /// The current interpolation setting.
    public InterpolationMode InterpolationMode { get; set; }

    /// The current setting whether to align with close (0.0f) or distant pixels (1.0f)
    /// during interpolation.
    public float InterpolationPreference
    {
      get => _interpolationPreference;
      set => _interpolationPreference = Mathf.Clamp(value, 0.0f, 1.0f);
    }
    
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// A composited matrix to fit the awareness buffer to the screen.
    /// This affine transform converts normalized screen coordinates to
    /// the buffer's coordinate frame while accounting for interpolation.
    public Matrix4x4 SamplerTransform { get; private set; }

    // Helper matrices
    private Matrix4x4 _displayTransform = Matrix4x4.identity;
    private Matrix4x4 _interpolationTransform = Matrix4x4.identity;
    private Matrix4x4 _intrinsicsFit = Matrix4x4.identity;

    /// This transform converts 2D normalized coordinates in buffer space to 3D points
    /// in camera space using the awareness buffer's orientation and intrinsics.
    /// This is primarily used to get camera space positions from depth,
    /// for example posInCamera = depth * BackProjectionTransform * screenUV.
    protected Matrix4x4 BackProjectionTransform { get; private set; }
    
    // The camera to world matrix is the inverted view matrix,
    // matching the orientation of the buffer. It is used
    // to transform points from camera space to world space.
    protected Matrix4x4 CameraToWorldTransform { get; private set; }

    // The back-projection plane distance (for interpolation)
    // between the preset near and far clipping planes.
    private float _interpolationPreference = 0.9f;
    
    // Cached state variables
    private ScreenOrientation _lastOrientation;
    private bool _didReceiveFirstUpdate;

    /// Updates the internal state of the context awareness stream.
    /// Calculates the transformation matrix used to map the buffer's
    /// contents to the target viewport. This is called the sampler transform.
    /// It's possible to call this API with a null buffer. In that case,
    /// only the sampler transform will be updated.
    /// @param buffer If provided, the contents of the buffer will be copied to an internal cache.
    /// @param arCamera The AR camera that captured the current frame.
    /// @param unityCamera The rendering Unity camera.
    [Obsolete("This method is deprecated, use ProcessFrame(TBuffer, IARCamera, Resolution, ScreenOrientation) instead.")]
    protected void ProcessFrame(TBuffer buffer, IARCamera arCamera, UnityEngine.Camera unityCamera)
    {
      ProcessFrame
      (
        buffer: buffer,
        arCamera: arCamera,
        targetResolution: new Resolution
        {
          width = unityCamera.pixelWidth, height = unityCamera.pixelHeight
        },
        targetOrientation: RenderTarget.ScreenOrientation
      );
    }
    
    /// Updates the internal state of the context awareness stream.
    /// Calculates the transformation matrix used to map the buffer's
    /// contents to the target viewport. This is called the sampler transform.
    /// It's possible to call this API with a null buffer. In that case,
    /// only the sampler transform will be updated.
    /// @param buffer If provided, the contents of the buffer will be copied to an internal cache.
    /// @param arCamera The AR camera that captured the current frame.
    /// @param targetResolution The resolution of the target viewport.
    /// @param targetOrientation The orientation of the target viewport.
    protected void ProcessFrame
    (
      TBuffer buffer,
      IARCamera arCamera,
      Resolution targetResolution,
      ScreenOrientation targetOrientation
    )
    {
      // Processing this frame can continue without having a new
      // awareness buffer available. In that case, we just update
      // the display and interpolation transformations.
      var didUpdateAwarenessBuffer = buffer != null;
      var isFirstUpdate = AwarenessBuffer == null && didUpdateAwarenessBuffer;

      // In case we have a new buffer available, we retain a CPU-side copy
      // This is necessary because the original buffer is owned by the
      // ARFrame and thus will get deallocated with it.
      if (didUpdateAwarenessBuffer)
      {
        // Release the previous buffer, if any
        AwarenessBuffer?.Dispose();
        
        // Cache a copy of the new buffer
        AwarenessBuffer = buffer.GetCopy() as TBuffer;
      }

      // We either have not received the first buffer or failed to make a copy
      if (AwarenessBuffer == null)
      {
        ARLog._Warn("No awareness buffer available to process.");
        return;
      }

      if (isFirstUpdate)
      {
        _didReceiveFirstUpdate = true;
        
        // Calculate an affine matrix that transforms the intrinsics from the 
        // AR image's aspect ratio and orientation to the buffer's coordinate space.
        _intrinsicsFit = AwarenessBuffer.CalculateDisplayTransform
        (
          arCamera.ImageResolution.width,
          arCamera.ImageResolution.height
        );
        
        // Propagate the event for when the context awareness feature initialized
        _awarenessStreamBegan?.Invoke(new ContextAwarenessArgs<TBuffer>(this));
      }

      // Check whether the viewport has been rotated and update the display transform
      var isDisplayTransformDirty = isFirstUpdate || (_lastOrientation != targetOrientation);
      if (isDisplayTransformDirty)
      {
        _lastOrientation = targetOrientation;

        // Calculate an affine matrix that transforms normalized coordinates from the 
        // viewport's aspect ratio and orientation to the buffer's coordinate space.
        _displayTransform = AwarenessBuffer.CalculateDisplayTransform
        (
          targetResolution.width,
          targetResolution.height,
          _lastOrientation,
          invertVertically: true
        );
      }

      // Note: Neural network results are asynchronous, therefore when
      // interpolation is on, we calculate a matrix to correct for the
      // displacement that occured between the beginning of inference
      // and the current pose of the AR driven camera.
      // We only need to update the interpolation transform either if 
      // there is a new buffer or if it's set to be updated every frame
      var isInterpolationTransformDirty =
        (InterpolationMode == InterpolationMode.Balanced && didUpdateAwarenessBuffer) ||
        InterpolationMode == InterpolationMode.Smooth;

      if (isInterpolationTransformDirty)
      {
        _interpolationTransform = AwarenessBuffer.CalculateInterpolationTransform
        (
          arCamera,
          targetOrientation,
          _interpolationPreference
        );
      }

      // We need to update the transform used for sampling
      // if either the display transform or the interpolation
      // transform has been changed
      var isSamplerTransformDirty = isDisplayTransformDirty || isInterpolationTransformDirty;
      if (isSamplerTransformDirty)
      {
        // Compose final matrix used for sampling depth
        SamplerTransform = InterpolationMode != InterpolationMode.None
          ? _interpolationTransform * _displayTransform
          : _displayTransform;
      }

      if (didUpdateAwarenessBuffer)
      {
        // The back projection transform converts normalized
        // viewport coordinates to 3D points in camera space.
        // To calculate it, we normalize the camera intrinsics.
        var intrinsics = NormalizeIntrinsics(arCamera.Intrinsics, arCamera.ImageResolution);

        // Then, we adjust the normalized intrinsics to the buffer's aspect.
        // The inverse of this matrix can be used to back project 2D points in 3D.
        // Additionally, we multiply with the depth buffer's display transform to
        // allow the matrix to be used with normalized viewport coordinates
        BackProjectionTransform =
          Matrix4x4.Inverse(_intrinsicsFit * intrinsics) * _displayTransform;
      }
      
      // This state variable represents whether the current representation of the 
      // context awareness buffer is altered, i.e. the contents of the buffer 
      // changes or it needs to be mapped to the screen differently.
      var isRepresentationDirty = didUpdateAwarenessBuffer || isSamplerTransformDirty;
      
      #if CONTEXT_AWARENESS_USE_INFERENCE_TIME_CAMERA
      // When CONTEXT_AWARENESS_USE_INFERENCE_TIME_CAMERA is defined,
      // the CameraToWorldTransform is only updated when there was a
      // change in the buffer's representation. As a result, when 
      // back-projecting values (e.g. depth) from a screen point,
      // the result will be calculated from where the screen point
      // was the last time when the buffer was updated instead of
      // where the screen point is currently.
      if (isRepresentationDirty) 
      #endif
      
      // The camera to world matrix is the inverted view matrix,
      // matching the orientation of the buffer. It is used
      // to transform points from camera space to world space.
      CameraToWorldTransform = AwarenessBuffer.CalculateCameraToWorldTransform(arCamera);

      if (isRepresentationDirty)
      {
        AwarenessStreamUpdated?.Invoke
        (
          new ContextAwarenessStreamUpdatedArgs<TBuffer>
          (
            sender: this,
            isKeyFrame: didUpdateAwarenessBuffer
          )
        );
      }
    }
    
    private Color[] _pixelBufferColor;
    private float[] _pixelBufferFloat;

    protected void CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      ScreenOrientation orientation,
      Func<Vector2, Color> sampler
    )
    {
      var resolution = CalculateContainerResolution
      (
        forBuffer: AwarenessBuffer,
        usingOrientation: orientation
      );

      var width = resolution.x;
      var height = resolution.y;
      var length = width * height;

      if (!PrepareTexture(ref texture, width, height, TextureFormat.ARGB32))
        return;

      // Allocate CPU buffer to cache the transformed image
      if (_pixelBufferColor == null || _pixelBufferColor.Length != length)
        _pixelBufferColor = new Color[length];

      for (int x = 0; x < width; x++)
      {
        var adjustedX = x + 0.5f;
        for (int y = 0; y < height; y++)
        {
          // Calculate normalized texture coordinates
          var uv = new Vector2(adjustedX / width, (y + 0.5f) / height);

          // Sample the buffer
          _pixelBufferColor[x + width * y] = sampler(uv);
        }
      }

      // Push to GPU
      texture.SetPixels(_pixelBufferColor);
      texture.Apply(updateMipmaps: false);
    }

    protected void CreateOrUpdateTextureRFloat
    (
      ref Texture2D texture,
      ScreenOrientation orientation,
      Func<Vector2, float> sampler
    )
    {
      var resolution = CalculateContainerResolution
      (
        forBuffer: AwarenessBuffer,
        usingOrientation: orientation
      );

      var width = resolution.x;
      var height = resolution.y;
      var length = width * height;

      if (!PrepareTexture(ref texture, width, height, TextureFormat.RFloat))
        return;

      // Allocate CPU buffer to cache the transformed image
      if (_pixelBufferFloat == null || _pixelBufferFloat.Length != length)
        _pixelBufferFloat = new float[length];

      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          // Calculate normalized texture coordinates
          var uv = new Vector2((x + 0.5f) / width, (y + 0.5f) / height);

          // Sample the buffer
          _pixelBufferFloat[x + width * y] = sampler(uv);
        }
      }

      // Push to GPU
      texture.SetPixelData(_pixelBufferFloat, 0);
      texture.Apply(updateMipmaps: false);
    }

    protected virtual void Dispose(bool disposing)
    {
      if(disposing)
        AwarenessBuffer?.Dispose();
    }
    
    ~AwarenessBufferProcessor()
    {
      Dispose(false);
    }

    /// Normalizes the intrinsics matrix using the specified resolution.
    /// @param intrinsics The original intrinsics for the AR image.
    /// @param resolution The resolution of the image related to the provided intrinsics.
    private static Matrix4x4 NormalizeIntrinsics(CameraIntrinsics intrinsics, Resolution resolution)
    {
      var widthMinusOne = resolution.width - 1;
      var heightMinusOne = resolution.height - 1;
      var result = Matrix4x4.identity;

      // Calculate normalized intrinsics
      result[0, 0] = intrinsics.FocalLength.x / widthMinusOne; // fx
      result[0, 3] = intrinsics.PrincipalPoint.x / widthMinusOne; // cx
      result[1, 1] = intrinsics.FocalLength.y / heightMinusOne; // fy
      result[1, 3] = intrinsics.PrincipalPoint.y / heightMinusOne; // cy

      return result;
    }
    
    private static Vector2Int CalculateContainerResolution(IAwarenessBuffer forBuffer, ScreenOrientation usingOrientation)
    {
      // Inspect the buffer
      var bufferWidth = (int)forBuffer.Width;
      var bufferHeight = (int)forBuffer.Height;

      var bufferOrientation = bufferWidth > bufferHeight
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
      var rotateContainer = bufferOrientation != usingOrientation;
      var width = rotateContainer ? bufferHeight : bufferWidth;
      var height = rotateContainer ? bufferWidth : bufferHeight;

      return new Vector2Int(width, height);
    }

    private static bool PrepareTexture(ref Texture2D texture, int width, int height, TextureFormat format)
    {
      if (texture == null)
      {
        // Alloc new texture
        texture = new Texture2D(width, height, format, false, false)
        {
          filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }
      else if (texture.format != format)
      {
        ARLog._Error("This texture has already been allocated with a different format.");
        return false;
      }
      
      if (texture.width != width || texture.height != height)
        texture.Reinitialize(width, height);

      return true;
    }
  }
}