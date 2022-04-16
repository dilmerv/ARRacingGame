using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Internals.EditorUtilities;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR.Mock;

using UnityEngine;

namespace Niantic.ARDK.Helpers
{
  [DisallowMultipleComponent]
  public sealed class ARRenderingManager: 
    UnityLifecycleDriver
  {
    [SerializeField]
    [HideInInspector]
    private int _renderTargetId;
    
    [SerializeField]
    [_Autofill]
    private Camera _camera = null;

    [SerializeField]
    private RenderTexture _targetTexture = null;

    [SerializeField]
    private float _nearClippingPlane = 0.1f;
    
    [SerializeField]
    private float _farClippingPlane = 100.0f;
    
    /// Event for when the underlying frame renderer initialized.
    public event ArdkEventHandler<FrameRenderedArgs> RendererInitialized; 

    /// Prevents us from writing back the values _we_ set in case we accidentally set the
    /// previous rates twice,
    /// i.e. we set the application's previous frame rate and sleep timeout in
    /// `SetupRendering`, next time it gets called (users can call Run multiple times) we then
    /// set the frame rate and sleep _again_ except this time it's not truly the applications's,
    /// it's the ones we set last time.
    private SavedRenderingSettings _savedRenderingSettings;

    private IARSession _session;
    private ARFrameRenderer _renderer;

    // Lazily allocated textures for snapshot images
    private RenderTexture _gpuTexture;
    private Texture2D _cpuTexture;

    // The texture properties can be accessed multiple times during a frame.
    // We only render a new snapshot if the main render target has
    // been updated. If not, we just return the most recent snapshot.
    private bool _isGPUTextureDirty = true;
    private bool _isCPUTextureDirty = true;
    private bool _isFrameDirty = true;
    private bool _releaseTargetTexture = false;

    /// Returns the renderer instance used by this manager
    public IARFrameRenderer Renderer
    {
      get => _renderer;
    }

    /// A GPU texture of the latest AR background.
    /// This texture is aligned (cropped, rotated) to the render target.
    /// If the renderer is set to target a render texture, this property
    /// will return that texture. Otherwise, a new render texture will be
    /// allocated and updated lazily. It is guaranteed that no more than
    /// a single Blit command will be issued during the same ARFrame.
    public RenderTexture GPUTexture
    {
      get
      {
        if (_renderer == null)
          return null;

        // If the renderer is already targeting a texture
        if (_renderer.Target.IsTargetingTexture)
          return _targetTexture;

        if (_isGPUTextureDirty)
        {
          // We only allow blitting a new frame, if the renderer is actively being
          // updated to make sure its internal state reflects the latest ARFrame.
          if (!isActiveAndEnabled)
            return null;

          // Allocate the GPU texture if this is the first time the property is called
          if (_gpuTexture == null)
          {
            _gpuTexture = new RenderTexture
            (
              _renderer.Resolution.width,
              _renderer.Resolution.height,
              24,
              RenderTextureFormat.ARGB32,
              RenderTextureReadWrite.Default
            );

            _gpuTexture.Create();
          }

          // Blit the current render state to the GPU texture
          _renderer.BlitToTexture(ref _gpuTexture);
          _isGPUTextureDirty = false;
        }

        return _gpuTexture;
      }
    }

    /// A CPU texture of the latest AR background.
    /// This texture is aligned (cropped, rotated) to the render
    /// target. It is guaranteed that only a single ReadPixels call
    /// will be performed during the same ARFrame.
    /// @note This is a fairly expensive call.
    public Texture2D CPUTexture
    {
      get
      {
        if (!_isCPUTextureDirty)
          return _cpuTexture;

        // Acquire a new GPU snapshot
        var source = GPUTexture;
        if (source == null)
          return null;

        // Push active target
        var activeRenderTexture = RenderTexture.active;
        RenderTexture.active = source;

        // Copy GPU snapshot to the CPU texture
        if (_cpuTexture == null)
          _cpuTexture = new Texture2D(source.width, source.height, TextureFormat.RGB24, true);

        _cpuTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        _cpuTexture.Apply(false);

        // Pop active target
        RenderTexture.active = activeRenderTexture;
        _isCPUTextureDirty = false;

        return _cpuTexture;
      }
    }

    protected override void InitializeImpl()
    {
      base.InitializeImpl();
      
      if (_camera != null)
      {
        // Copy clipping plane distances from the camera if available
        _nearClippingPlane = _camera.nearClipPlane;
        _farClippingPlane = _camera.farClipPlane;
      }

      // Assign the current session 
      ARSessionFactory.SessionInitialized += ARSessionFactory_SessionInitialized;
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();
      ARSessionFactory.SessionInitialized -= ARSessionFactory_SessionInitialized;
      
      // Release the renderer
      _renderer?.Dispose();
      _renderer = null;

      // Release textures
      if (_gpuTexture != null)
        Destroy(_gpuTexture);

      if (_cpuTexture != null)
        Destroy(_cpuTexture);

      if (_releaseTargetTexture && _targetTexture != null)
        Destroy(_targetTexture);
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();
      if (_renderer == null)
        return;

      _renderer.Enable();
      AdjustFrameRate(_renderer);
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();
      _renderer?.Disable();
      
      // Revert application settings
      _savedRenderingSettings?.Apply();
      _savedRenderingSettings = null;
    }

    // We drive rendering from late update, to
    // wait for render state providers to update 
    private void LateUpdate()
    {
      if (!_isFrameDirty)
        return;
      
      if (_renderer == null)
        return;

      var frame = _session?.CurrentFrame;
      if (frame == null)
        return;

      _renderer.UpdateState(withFrame: frame);

      // Invalidate snapshot textures
      _isCPUTextureDirty = true;
      _isGPUTextureDirty = true;

      // Validate frame
      _isFrameDirty = false;
    }

    private void ARSessionFactory_SessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
      {
        _session.FrameUpdated -= InvalidateFrame;
        _session.Ran -= OnSessionRan;
        _session.Paused -= OnSessionPaused;
        _session.Deinitialized -= OnSessionDeinitialized;
      }

      _session = args.Session;
      _session.FrameUpdated += InvalidateFrame;
      _session.Ran += OnSessionRan;
      _session.Paused += OnSessionPaused;
      _session.Deinitialized += OnSessionDeinitialized;

      if (_camera == null)
        return;

      // If it is a mock session, attempt to disable the layer all mock objects are on so "real"
      // objects aren't doubly rendered by mock device camera and the scene camera.
      if (_session.RuntimeEnvironment == RuntimeEnvironment.Mock)
      {
        _MockFrameBufferProvider.RemoveLayerFromCamera(_camera, _MockFrameBufferProvider.MOCK_LAYER_NAME);
      }
    }

    private void OnSessionRan(ARSessionRanArgs args)
    {
      if (_renderer != null)
      {
        // If the renderer is already created, and this is enabled, enable the renderer.
        if (AreFeaturesEnabled)
          EnableFeaturesImpl();
        
        return;
      }

      // Create the renderer
      _renderer = CreateRenderer(_session.RuntimeEnvironment);
      if (_renderer == null)
      {
        ARLog._Error("Failed to create a renderer for the running platform.");
        return;
      }
      
      // Initialize the renderer
      _renderer.Initialize();
      
      // Propagate the initialization of the renderer
      RendererInitialized?.Invoke(new FrameRenderedArgs(_renderer));

      // Attach any render state providers
      var stateProviders = GetComponents<IRenderFeatureProvider>();
      foreach (var provider in stateProviders)
        _renderer.AddFeatureProvider(provider);
      
      // Enable renderer
      if (AreFeaturesEnabled)
      {
        _renderer.Enable();
        AdjustFrameRate(_renderer);
      }
    }

    private void AdjustFrameRate(IARFrameRenderer withRenderer)
    {
      // Push application settings
      _savedRenderingSettings = new SavedRenderingSettings
      (
        Application.targetFrameRate,
        Screen.sleepTimeout,
        QualitySettings.vSyncCount
      );
      
      // Configure target frame rate
      QualitySettings.vSyncCount = 0;
      Application.targetFrameRate = withRenderer.RecommendedFrameRate;
    }

    private void InvalidateFrame(FrameUpdatedArgs arg)
    {
      _isFrameDirty = true;
    }

    private ARFrameRenderer CreateRenderer(RuntimeEnvironment environment)
    {
      // Is the render target set to the screen backbuffer or to a texture?
      var isOffscreen = _renderTargetId > 0;

      // In case the target texture was not provided
      if (isOffscreen && _targetTexture == null)
      {
        // Allocate a new render texture
        _targetTexture = new RenderTexture(Screen.width, Screen.height, depth: 24)
        {
          useMipMap = false, autoGenerateMips = false, filterMode = FilterMode.Point, anisoLevel = 0
        };

        _targetTexture.Create();
        
        // The rendering manager owns the target texture and needs to release it
        _releaseTargetTexture = true;
      }

      var renderTarget = isOffscreen
        ? new RenderTarget(_targetTexture)
        : new RenderTarget(_camera);

      var result = ARFrameRendererFactory.Create
      (
        renderTarget,
        environment,
        _nearClippingPlane,
        _farClippingPlane
      );

      if (result != null)
        result.IsOrientationLocked = false;

      return result;
    }

    private void OnSessionPaused(ARSessionPausedArgs args)
    {
      DisableFeaturesImpl();
    }

    private void OnSessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      if (_session != null)
      {
        _session.FrameUpdated -= InvalidateFrame;
        _session.Ran -= OnSessionRan;
        _session.Paused -= OnSessionPaused;
        _session.Deinitialized -= OnSessionDeinitialized;
        _session = null;
      }

      _renderer?.Dispose();
      _renderer = null;

      DisableFeaturesImpl();
    }
  }
}