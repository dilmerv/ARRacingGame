using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Localization;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Localization
{
  /// @cond ARDK_VPS_BETA
  /// Event args for events from Visual Positioning System (VPS) localization requests
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public struct LocalizationEventArgs :
    IArdkEventArgs
  {
    internal LocalizationEventArgs(ARWorldCoordinateSpace coordinateSpace, float confidence)
    {
      CoordinateSpace = coordinateSpace;
      FailureReason = LocalizationFailureReason.None;
      Confidence = confidence;
    }

    internal LocalizationEventArgs(LocalizationFailureReason failureReason)
    {
      CoordinateSpace = null;
      FailureReason = failureReason;
      Confidence = 0.0f;
    }

    public ARWorldCoordinateSpace CoordinateSpace { get; }
    public LocalizationFailureReason FailureReason { get; }

    public float Confidence { get; }
  }

  /// @cond ARDK_VPS_BETA
  /// This helper is used to handle events from Visual Positioning System (VPS) localization requests
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public class LocalizationEventManager: ARSessionListener
  {
    private ILocalizer _localizer = null;
    public ILocalizer Localizer
    {
      get => _localizer;
    }

    public event ArdkEventHandler<LocalizationEventArgs> LocalizationUpdated;
    public event ArdkEventHandler<LocalizationEventArgs> LocalizationSucceeded;
    public event ArdkEventHandler<LocalizationEventArgs> LocalizationFailed;
    public event ArdkEventHandler<LocalizationEventArgs> LocalizationCleared;

    public List<ARWorldCoordinateSpace> Localizations
    {
      get
      {
        return new List<ARWorldCoordinateSpace>(_localizations.Values);
      }
    }

    public ARWorldCoordinateSpace.Identifier? LatestLocalization
    {
      get
      {
        return _latestLocalizationId;
      }
    }
    
    private Dictionary<ARWorldCoordinateSpace.Identifier, ARWorldCoordinateSpace> _localizations =
      new Dictionary<ARWorldCoordinateSpace.Identifier, ARWorldCoordinateSpace>();

    private ARWorldCoordinateSpace.Identifier? _latestLocalizationId = null;

    protected override void ListenToSession()
    {
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

      _localizer.LocalizationProgressUpdated += OnLocalizationProgress;
    }

    protected override void StopListeningToSession()
    {
      if (_localizer == null)
      {
        return;
      }

      _localizer.LocalizationProgressUpdated -= OnLocalizationProgress;
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      // Do nothing.
    }

    private void OnLocalizationProgress(LocalizationProgressArgs args)
    {
      switch (args.State)
      {
        case LocalizationState.Initializing:
        case LocalizationState.Localizing:
          if (LocalizationUpdated != null)
          {
            var eventArgs = new LocalizationEventArgs();
            LocalizationUpdated(eventArgs);
          }
          break;

        case LocalizationState.Localized:
          _latestLocalizationId = args.WorldCoordinateSpace.Id;
          _localizations[args.WorldCoordinateSpace.Id] = args.WorldCoordinateSpace;
          if (LocalizationSucceeded != null)
          {
            var eventArgs = new LocalizationEventArgs(args.WorldCoordinateSpace, args.Confidence);
            LocalizationSucceeded(eventArgs);
          }
          break;

        case LocalizationState.Failed:
          if (LocalizationFailed != null)
          {
            var eventArgs = new LocalizationEventArgs(args.FailureReason);
            LocalizationFailed(eventArgs);
          }
          break;
      }
    }

// TODO: Remove these debug methods, or move them to Mock only
#region Debug
    /// @note This debug feature will be moved or removed in a future update
    public void DebugLocalizationUpdated(string mapID, Matrix4x4 transform, float confidence)
    {
      var identifier = new ARWorldCoordinateSpace.Identifier(mapID);
      var coordinateSpace = new ARWorldCoordinateSpace(identifier, transform);
      _localizations[identifier] = coordinateSpace;
      if (LocalizationSucceeded != null)
      {
        var args = new LocalizationEventArgs(coordinateSpace, confidence);
        LocalizationSucceeded(args);
      }
    }

    /// @note This debug feature will be moved or removed in a future update
    public void DebugLocalizationFailed()
    {
      if (LocalizationFailed != null)
      {
        var args = new LocalizationEventArgs(LocalizationFailureReason.Unknown);
        LocalizationFailed(args);
      }
    }

    /// @note This debug feature will be moved or removed in a future update
    public void DebugLocalizationCleared()
    {
      _localizations.Clear();
      if (LocalizationCleared != null)
      {
        var args = new LocalizationEventArgs();
        LocalizationCleared(args);
      }
    }
#endregion
  }
}
