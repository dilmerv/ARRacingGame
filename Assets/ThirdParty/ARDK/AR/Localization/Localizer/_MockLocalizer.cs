// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.VirtualStudio.AR;

using UnityEngine;

namespace Niantic.ARDK.AR.Localization
{
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  internal sealed class _MockLocalizer:
    ILocalizer
  {
    internal _MockLocalizer(IARSession session)
    {
      _arSession = session;
    }

    public event ArdkEventHandler<LocalizationProgressArgs> LocalizationProgressUpdated;
    public event Action<ARWorldCoordinateSpace.Identifier?, float> LocalizationStarted;
    
    /// Localization was stopped.
    public event Action LocalizationStopped;
    private bool _isLocalizing;
    private float _localizationEndTime;
    private readonly IARSession _arSession;

    public void UpdateLocalizationProgress
    (
      LocalizationState state,
      LocalizationFailureReason failureReason,
      float confidence,
      ARWorldCoordinateSpace coordinateSpace
    )
    {
      if (!_isLocalizing)
      {
        var error = "Tried to update localization progress but localization has not been" +
                    " started. This update will be ignored.";

        ARLog._Error(error);
        return;
      }

      if (state == LocalizationState.Failed || state == LocalizationState.Localized)
        _isLocalizing = false;

      if (LocalizationProgressUpdated != null)
      {
        var args = new LocalizationProgressArgs
        (
          state,
          failureReason,
          confidence,
          coordinateSpace
        );

        LocalizationProgressUpdated(args);
      }
    }

    public void StartLocalization(ILocalizationConfiguration config)
    {
      if (_arSession is _MockARSession mockARSession)
      {
        var isLocationServiceInitializedForNoId =
          !mockARSession._HasSetupLocationService &&
          string.IsNullOrEmpty(config.MapIdentifier);

        if (isLocationServiceInitializedForNoId)
        {
          ARLog._Error
          (
            "SetupLocationService(locationService) must be called before attempting to localize" +
            " against any available world coordinate space."
          );
        
          return;
        }
      }

      if (string.IsNullOrEmpty(config.MapIdentifier))
      {
        StartMockLocalization(null, config.LocalizationTimeout);
      }
      else
      {
        var identifier = new ARWorldCoordinateSpace.Identifier(config.MapIdentifier);
        StartMockLocalization(identifier, config.LocalizationTimeout);
      }
    }
    
    public void StopLocalization()
    {
      if (_isLocalizing)
      {
        _isLocalizing = false;
        _UpdateLoop.Tick -= CheckLocalizationTimeout;
        LocalizationStopped?.Invoke();
      }
    }

    private void StartMockLocalization
    (
      ARWorldCoordinateSpace.Identifier? identifier,
      float timeout
    )
    {
      if (timeout <= 0)
      {
        ARLog._Warn("Timeout value less than 0 will trigger infinite wait");
        _localizationEndTime = float.MaxValue;
      } 
      else
      {
        _localizationEndTime = Time.time + timeout;
      }

      if (_arSession.State != ARSessionState.Running)
      {
        ARLog._Error("Cannot start localization when ARSession is not running.");
        return;
      }

      _isLocalizing = true;
      LocalizationStarted?.Invoke(identifier, Time.time);

      _UpdateLoop.Tick += CheckLocalizationTimeout;
    }

    private void CheckLocalizationTimeout()
    {
      if (Time.time >= _localizationEndTime && _isLocalizing)
      {
        StopLocalization();
      }
    }

    public void Dispose()
    {
      // Do nothing. This implementation of ILocalizer is fully managed.
    }
  }
}
