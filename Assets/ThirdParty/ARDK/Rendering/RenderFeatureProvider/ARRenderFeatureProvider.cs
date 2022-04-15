using System.Collections.Generic;

using Niantic.ARDK.Extensions;
using Niantic.ARDK.Helpers;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Rendering
{
  /// Base class for render state providers.
  /// Render state provider components maintain a two sets:
  ///   - A list of all possible render features they may be enabled
  ///   - A list of the render features that are actually need to be enabled
  /// A render feature is just a shader keyword that triggers the compilation
  /// of additional code to the shader that renders the background.
  public abstract class ARRenderFeatureProvider : 
    ARConfigChanger, 
    IRenderFeatureProvider,
    ITargetableRenderFeatureProvider
  {
    private event ArdkEventHandler<RenderFeaturesChangedArgs> _activeFeaturesChanged;

    /// Invoked when the provider reconfigures its active features.
    public event ArdkEventHandler<RenderFeaturesChangedArgs> ActiveFeaturesChanged
    {
      add
      {
        _activeFeaturesChanged += value;
        if (AreFeaturesEnabled)
          value.Invoke(new RenderFeaturesChangedArgs(OnEvaluateConfiguration()));
      }

      remove => _activeFeaturesChanged -= value;
    }

    private HashSet<string> _features;

    /// A set of all render features this provider may enable or disable.
    public ISet<string> Features
    {
      get => _features ?? (_features = OnAcquireFeatureSet());
    }
    
    private RenderTarget? _renderTarget;

    /// The active render target of this provider
    public RenderTarget? Target
    {
      get => _renderTarget;
      set
      {
        if (!value.HasValue)
        {
          if (_renderTarget.HasValue)
          {
            // Reset target
            _renderTarget = null;
            OnRenderTargetChanged(null);  
          }
          
          return;
        }
        
        // Make sure the new target is different
        if (_renderTarget.HasValue && _renderTarget.Value.Equals(value.Value))
          return;
        
        _renderTarget = value;
        OnRenderTargetChanged(value);
      }
    }

    // When the component gets enabled, the set of enabled render features are re-evaluated
    // This is because the developer might change the state of the component while it is disabled
    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();
      
      // Propagate AR session config changes
      RaiseConfigurationChanged();

      // Propagate active render features
      _activeFeaturesChanged?.Invoke(new RenderFeaturesChangedArgs(OnEvaluateConfiguration()));
    }

    // When the component gets disabled, every render feature managed by this
    // state provider gets deactivated. E.g. Disabling the provider of depth
    // will result in a shader that doesn't handle occlusions.
    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();
      
      // Propagate AR session config changes
      RaiseConfigurationChanged();

      // Disable all render features
      _activeFeaturesChanged?.Invoke
      (
        new RenderFeaturesChangedArgs(new RenderFeatureConfiguration(null, Features))
      );
    }

    // Manually triggers the re-evaluation of active render features.
    // This should be called from children, when there is an explicit
    // request to enable or disable a specific render feature.
    protected void InvalidateActiveFeatures()
    {
      // Propagate active render features
      _activeFeaturesChanged?.Invoke(new RenderFeaturesChangedArgs(OnEvaluateConfiguration()));
    }
    
    /// Invoked when this component is asked about the render features
    /// it is may be responsible for.
    /// @note: The implementation needs to include all features that is
    /// possible to manipulate with this component.
    protected abstract HashSet<string> OnAcquireFeatureSet();
    
    /// Invoked when it is time to calculate the actual features
    /// that this component currently manages.
    protected abstract RenderFeatureConfiguration OnEvaluateConfiguration();
    
    /// Invoked when the active render target has changed for this feature provider
    /// @param target The new render target. If this is null, the provider should
    ///               set its target to the default option.
    protected abstract void OnRenderTargetChanged(RenderTarget? target);

    /// Called when it is time to copy the current render state to the main rendering material.
    /// @param material Material used to render the frame.
    public abstract void UpdateRenderState(Material material);
  }
}
