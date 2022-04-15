using System;

using UnityEngine;
using System.Collections;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Localization;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Extensions.Localization
{
  /// @cond ARDK_VPS_BETA
  /// This helper is used to make Visual Positioning System (VPS) localization requests
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public class LocalizationAttemptManager: 
    ARSessionListener
  {
    // If empty, will default to mock server that always succeeds if localization request is valid.
    public string LocalizationEndpoint = string.Empty;

    public string MapIdentifier = string.Empty;
    public float LocalizationTimeout = 30;
    public float RequestTimeLimit = 10;

    private ILocalizationConfiguration _localizationConfiguration = null;

    private ILocalizer _localizer = null;
    private ILocationService _locationService;

    protected override void ListenToSession()
    {
      // Do nothing
    }

    protected override void StopListeningToSession()
    {
      // Do nothing
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      // Do nothing
    }
    
    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      StartLocalization();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      StopLocalization();
    }

    /// @cond ARDK_VPS_BETA
    /// Start the localization process
    /// @endcond
    public void StartLocalization()
    {
      if (ARSession == null)
      {
        ARLog._Warn("Did not start localization because an ARSession has not been initialized.");
        return;
      }

      if (_localizer == null)
      {
        if (ARSession is ILocalizableARSession localizableARSession)
        {
          _localizer = localizableARSession.Localizer;
        }
        else
        {
          var ex = "Could not cast the IARSession to an ILocalizableARSession, cannot localize";
          throw new InvalidCastException(ex);
        }
      }

      if (_locationService == null)
      {
        if (Application.isEditor)
        {
          _locationService = new SpoofLocationService();
        }
        else
        {
          var unityLocationService = new UnityLocationService();
          unityLocationService.StartSession(0.2f, 0.0f, 0.0f);
          _locationService = unityLocationService;
        }

        ARSession.SetupLocationService(_locationService);
      }

      if (_localizationConfiguration == null)
        _localizationConfiguration = LocalizationConfigurationFactory.Create();

      _localizationConfiguration.MapIdentifier = MapIdentifier;
      _localizationConfiguration.LocalizationTimeout = LocalizationTimeout;
      _localizationConfiguration.RequestTimeLimit = RequestTimeLimit;
      _localizationConfiguration.LocalizationEndpoint = LocalizationEndpoint;
      _localizer.StartLocalization(_localizationConfiguration);
      
      ARLog._DebugFormat
      (
        "Attempting Localization ( MapIdentifier={0} , LocalizationTimeout={1} , RequestTimeLimit={2} )",
        false,
        _localizationConfiguration.MapIdentifier,
        _localizationConfiguration.LocalizationTimeout,
        _localizationConfiguration.RequestTimeLimit
      );
    }

    /// @cond ARDK_VPS_BETA
    /// Stop the localization process
    /// @endcond
    public void StopLocalization()
    {
      if (_localizer == null)
      {
        ARLog._Debug
        (
          "Did not stop localization because a Localizer has not been initialized, " +
          "or was already destroyed."
        );

        return;
      }

      _localizer.StopLocalization();
    }

    protected override void DeinitializeImpl()
    {
      _localizer?.Dispose();
      _localizationConfiguration?.Dispose();
      
      if (_locationService is UnityLocationService unityLocationService)
        unityLocationService.StopSession();
      
      _locationService = null;

      base.DeinitializeImpl();
    }
  }
}
