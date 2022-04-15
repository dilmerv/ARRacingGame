// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;

#pragma warning disable 0649

namespace Niantic.ARDK.Extensions
{
  /// This helper can be placed in a scene to help visualize the position history of the camera as
  /// a colored line, for debug purposes. The history is not truncated, and this helpers renders a
  /// line going back to the beginning of the ARSession, or the last call to Clear, whichever
  /// is most recent.
  /// All data is expected to come from ARSession.
  [RequireComponent(typeof(LineRenderer))]
  public class ARCameraPositionHistoryHelper : MonoBehaviour
  {
    private IARSession _session;
    
    [SerializeField]
    /// How often (in seconds) a point should be added to the position history.
    private float _updateInterval = 0.5f;
    private float _lastUpdate = 0;
    private LineRenderer _lineRenderer;

    public bool Visible
    {
      get
      {
        return _lineRenderer && _lineRenderer.enabled;
      }
      set
      {
        if (_lineRenderer)
          _lineRenderer.enabled = value;
      }
    }

    private void Awake()
    {
      _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
      ARSessionFactory.SessionInitialized += OnSessionInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= OnSessionInitialized;
      Teardown();
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        return;

      _session = args.Session;
      _session.Deinitialized += OnDeinitialized;
      _session.FrameUpdated += OnFrameUpdated;
    }

    private void OnDeinitialized(ARSessionDeinitializedArgs args)
    {
      Teardown();
    }

    private void Teardown()
    {
      if (_session != null)
      {
        _session.Deinitialized -= OnDeinitialized;
        _session.FrameUpdated -= OnFrameUpdated;
        _session = null;
      }
      
      Clear();
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      if (args.Frame.Camera.TrackingState == TrackingState.Normal)
      {
        var worldTransform = args.Frame.Camera.GetViewMatrix(Screen.orientation).inverse;
        UpdateCameraPosition(worldTransform.ToPosition());
      }
    }

    private void UpdateCameraPosition(Vector3 position)
    {
      var now = Time.time;
      if (now > _lastUpdate + _updateInterval)
      {
        _lineRenderer.positionCount = _lineRenderer.positionCount + 1;
        _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, position);
        _lastUpdate = now;
      }
    }

    public void Clear()
    {
      _lastUpdate = 0;
      _lineRenderer.positionCount = 0;
    }
  }
}

#pragma warning restore 0649
