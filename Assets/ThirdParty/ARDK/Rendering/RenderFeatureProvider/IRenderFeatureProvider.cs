using System.Collections.Generic;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Rendering
{
  public interface ITargetableRenderFeatureProvider
  {
    /// The active render target of this provider
    RenderTarget? Target { get; set; }
  }

  /// A component that supplies the renderer with additional information to render an ARFrame.
  public interface IRenderFeatureProvider
  {
    /// Invoked when the provider reconfigured its active features.
    event ArdkEventHandler<RenderFeaturesChangedArgs> ActiveFeaturesChanged;
    
    /// A set of all render features this provider may enable or disable.
    ISet<string> Features { get; }
    
    /// Updates the provided material with additional information
    /// this component is responsible of providing.
    void UpdateRenderState(Material material);
  }
  
  /// Stores which render features should be enabled or disabled.
  public readonly struct RenderFeatureConfiguration
  {
    public readonly IEnumerable<string> FeaturesEnabled;
    public readonly IEnumerable<string> FeaturesDisabled;

    public RenderFeatureConfiguration
    (
      IEnumerable<string> featuresEnabled,
      IEnumerable<string> featuresDisabled
    )
    {
      FeaturesEnabled = featuresEnabled;
      FeaturesDisabled = featuresDisabled;
    }
  }
  
  /// Event args for when the provider reconfigured its active features.
  public class RenderFeaturesChangedArgs: IArdkEventArgs
  {
    public readonly RenderFeatureConfiguration Configuration;
    public RenderFeaturesChangedArgs(RenderFeatureConfiguration configuration)
    {
      Configuration = configuration;
    }
  }
}
