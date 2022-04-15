using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Localization;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public class MockLocalizationUpdate:
    MockDetectableBase
  {
    private sealed class _SessionHelper: _ISessionHelper
    {
      public _IMockARSession Session { get; }
      public bool IsLocal { get; }
      public _MockLocalizer Localizer { get; private set; }

      private readonly MockLocalizationUpdate _owner;
      private bool _isLocalizing;

      private Coroutine _discoveryCoroutine;

      internal _SessionHelper(MockLocalizationUpdate owner, _IMockARSession session, bool isLocal)
      {
        _owner = owner;
        Session = session;
        IsLocal = isLocal;

        Session.Deinitialized += OnSessionDeinitialized;
        ILocalizer localizer = null;

        if (session is ILocalizableARSession localizableARSession)
          localizer = localizableARSession.Localizer;
        
        if (localizer is _MockLocalizer mockLocalizer)
        {
          Localizer = mockLocalizer;
          Localizer.LocalizationStarted += OnLocalizationStarted;
          Localizer.LocalizationStopped += OnLocalizationStopped;
        }
        else
        {
          ARLog._Error("Must use a mock localizer for mock localization, updates won't occur");
        }
      }
      
      public void Dispose()
      {
        Localizer.Dispose();
      }

      private void StopDiscoveryCoroutine()
      {
        if (_discoveryCoroutine != null)
        {
          if (_owner != null)
            _owner.StopCoroutine(_discoveryCoroutine);
      
          _discoveryCoroutine = null;
        }
      }
      
      private void OnLocalizationStarted(ARWorldCoordinateSpace.Identifier? identifier, float timeout)
      {
        if (identifier.HasValue && !identifier.Equals(_owner._identifier))
        {
          // Localization is for a different space than this one, so it can be ignored
          ARLog._DebugFormat
          (
            "LocalizationUpdate for {0} ignoring request for {1}",
            false,
            _owner._identifier,
            identifier.Value
          );
      
          return;
        }

        if (_isLocalizing)
        {
          _owner.OnSessionRanAgain(Session);
        }

        _isLocalizing = true;
      
        StopDiscoveryCoroutine();
        _discoveryCoroutine = _owner.StartCoroutine(nameof(WaitToBeDiscovered), this);
      }

      private void OnLocalizationStopped()
      {
        StopDiscoveryCoroutine();
      }
      
      private void OnSessionDeinitialized(ARSessionDeinitializedArgs args)
      {
        StopDiscoveryCoroutine();
      }
    }

    /// _mapId is the string identifying the coordinate space.
    [SerializeField]
    [Tooltip("Identifier for the coordinate space")]
    protected string _mapId = null;

    /// _state is the flag for the localization update status.
    [SerializeField]
    [Tooltip("State of the localization update")]
    private LocalizationState _state = LocalizationState.Localized;

    /// _failureReason is the flag for the localization failure reason.
    /// Ignored if _state is not Failed.
    [SerializeField]
    [Tooltip("Reason for the localization failure. Will be overridden if the state is not Failed")]
    private LocalizationFailureReason _failureReason = LocalizationFailureReason.None;

    /// _confidence is the localization confidence value between 0.0 (low) to 1.0 (high).
    /// Ignored if _state is Failed.
    [SerializeField]
    [Tooltip("Localization confidence from 0.0 to 1.0. Will be overridden if the state is Failed")]
    [Range(0.0f, 1.0f)]
    private float _confidence = 1.0f;

    private Coroutine _coroutine;
    private ARWorldCoordinateSpace.Identifier _identifier;

    private const string DefaultIdentifier = "6ddb7401-71f1-4f71-8e61-d0a2ee0caabf";
    private _MockLocalizer _localizer;

    protected override bool Initialize()
    {
      if (string.IsNullOrEmpty(_mapId))
      {
        ARLog._Debug("No map identifier assigned. Will use the default identifier.");
        _mapId = DefaultIdentifier;
      }

      _identifier = new ARWorldCoordinateSpace.Identifier(_mapId);

      return base.Initialize();
    }

    internal override _ISessionHelper _CreateSessionHelper(_IMockARSession mockSession, bool isLocal)
    {
      _SessionHelper helper;
      if(mockSession is _IMockARSession castedSession)
        helper = new _SessionHelper(this, castedSession, isLocal);
      else
      {
        var exception = "Must use an _IMockARSession for mock localization base";
        throw new InvalidCastException(exception);
      }
      
      _localizer = helper.Localizer;
      return helper;
    }

    internal override void BeDiscovered(_IMockARSession arSession, bool sessionIsLocal)
    {
      var failureReason = _failureReason;
      var confidence = _confidence;
      ARWorldCoordinateSpace coordinateSpace = null;

      switch (_state)
      {
        case LocalizationState.Initializing:
        case LocalizationState.Localizing:
          ARLog._DebugFormat("PROGRESS: " + _state);
          failureReason = LocalizationFailureReason.None;
          break;

        case LocalizationState.Localized:
          ARLog._Debug("SUCCESS: Localized against " + _mapId);
          coordinateSpace = new ARWorldCoordinateSpace(_identifier, transform.localToWorldMatrix);
          failureReason = LocalizationFailureReason.None;
          break;

        case LocalizationState.Failed:
          ARLog._Debug("FAILED: " + _failureReason);
          confidence = 0.0f;
          break;
      }

      _localizer.UpdateLocalizationProgress(_state, failureReason, confidence, coordinateSpace);
    }

    internal override void OnSessionRanAgain(_IMockARSession arSession)
    {
      // Do nothing
    }
  }
}
