using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Extensions.Depth;
using Niantic.ARDK.Extensions.Localization;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Localization
{
  public class LocalizationExampleManager: MonoBehaviour
  {
    [Header("Control Buttons")]
    [SerializeField]
    private Button _runARButton;
    
    [SerializeField]
    private Button _stopARButton;
    
    [SerializeField]
    private Button _attemptLocalizationButton;
    
    [SerializeField]
    private Button _stopLocalizationButton;

    [Header("Loading Depth Popup")]
    [SerializeField]
    private GameObject _depthLoadingPopup;

    [Header("Status Bar")]
    [SerializeField]
    private GameObject _statusBar;
    
    [SerializeField]
    private Text _statusLabel;
    
    [SerializeField]
    private RawImage _depthVisualization;

    [Header("VPS Options")]
    [SerializeField]
    private InputField _vpsServiceURLField;

    [Header("Map Id Localization")]
    [SerializeField]
    private InputField _localizationTimeoutField;

    [SerializeField]
    private Toggle _depthToggle;

    [SerializeField]
    private InputField _mapIdField;

    [SerializeField]
    private InputField _requestTimeLimitField;

    [Header("Manual Localization")]
    [SerializeField]
    private InputField _txField;

    [SerializeField]
    private InputField _tyField;

    [SerializeField]
    private InputField _tzField;

    [SerializeField]
    private InputField _rxField;

    [SerializeField]
    private InputField _ryField;

    [SerializeField]
    private InputField _rzField;

    [SerializeField]
    private InputField _confidenceField;

    [Header("Scene Helpers")]
    [SerializeField]
    private LocalizationEventManager _localizationEventManager;
    
    [SerializeField]
    private LocalizationAttemptManager _localizationAttemptManager;

    [SerializeField]
    private ARDepthManager _depthManager;

    private IARSession _session;
    private int _counter = 0;
    private Texture2D _disparityTexture;
    
    private void Awake()
    {
      // DEBUG: enable all logs
      ARLog.EnableLogFeature("Niantic.ARDK");

      UpdateControlButtons(false);

      ARSessionFactory.SessionInitialized += OnSessionInitialized;

      if (_localizationEventManager)
      {
        _localizationEventManager.LocalizationUpdated += OnLocalizationUpdated;
        _localizationEventManager.LocalizationSucceeded += OnLocalizationSucceeded;
        _localizationEventManager.LocalizationFailed += OnLocalizationFailed;
        _localizationEventManager.LocalizationCleared += OnLocalizationCleared;
      }
      else
      {
        Debug.LogError("LocalizationExampleManager needs a LocalizationEventManager!");
      }

      if (_localizationAttemptManager)
      {
        if (_vpsServiceURLField)
        {
          _vpsServiceURLField.text = _localizationAttemptManager.LocalizationEndpoint;

          _vpsServiceURLField.onValueChanged.AddListener
          (
            value => { _localizationAttemptManager.LocalizationEndpoint = value; }
          );
        }

        if (_localizationTimeoutField)
        {
          _localizationTimeoutField.text = _localizationAttemptManager.LocalizationTimeout.ToString();
          _localizationTimeoutField.onValueChanged.AddListener
          (
            value =>
            {
              if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float localizationTimeout))
                _localizationAttemptManager.LocalizationTimeout = localizationTimeout;
            }
          );
        }

        if (_mapIdField)
        {
          _mapIdField.text = _localizationAttemptManager.MapIdentifier;
          _mapIdField.onValueChanged.AddListener
          (
            value => { _localizationAttemptManager.MapIdentifier = value; }
          );
        }
        if (_requestTimeLimitField)
        {
          _requestTimeLimitField.text = _localizationAttemptManager.RequestTimeLimit.ToString();
          _requestTimeLimitField.onValueChanged.AddListener
          (
            value =>
            {
              if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float requestTimeLimit))
                _localizationAttemptManager.RequestTimeLimit = requestTimeLimit;
            }
          );
        }

      }
      else
      {
        Debug.LogError("LocalizationExampleManager needs a LocalizationAttemptManager!");
      }

      if (_depthManager)
      {
        _depthManager.DepthBufferUpdated +=
          args =>
          {
            // Only update the texture if we are actually looking at it 
            if(!_depthVisualization.isActiveAndEnabled)
              return;
            
            _depthManager.
              DepthBufferProcessor.
              CopyToAlignedTextureRFloat(ref _disparityTexture, Screen.orientation);

            _depthVisualization.texture = _disparityTexture;
          };
      }
      else
      {
        Debug.LogError("LocalizationExampleManager needs an ARDepthManager!");
      }
    }

    private void UpdateControlButtons(bool isSessionRunning)
    {
      if (_statusBar)
        _statusBar.SetActive(isSessionRunning);

      var depthActive = _depthManager && _depthToggle && _depthToggle.isOn;
      if (_depthLoadingPopup && (!isSessionRunning || !depthActive))
        _depthLoadingPopup.gameObject.SetActive(false);
      
      if (_runARButton)
        _runARButton.gameObject.SetActive(!isSessionRunning);

      if (_stopARButton)
        _stopARButton.gameObject.SetActive(isSessionRunning);

      if (_attemptLocalizationButton)
        _attemptLocalizationButton.interactable = isSessionRunning;

      if (_stopLocalizationButton)
        _stopLocalizationButton.interactable = isSessionRunning;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= OnSessionInitialized;
      if(_disparityTexture != null)
        Destroy(_disparityTexture);
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        return;
      
      _session = args.Session;
      _session.Deinitialized += OnSessionDeinitialized;
      _session.Ran += OnSessionRan;
      _session.Paused += OnSessionPaused;
      Debug.Log("Session was initialized: " + _session.StageIdentifier);

      var depthActive = _depthManager && _depthToggle && _depthToggle.isOn;
      if (_depthLoadingPopup && depthActive)
      {
        _session.FrameUpdated += OnFrameUpdated;
        _depthLoadingPopup.SetActive(true);
      }

      UpdateControlButtons(true);
    }

    private void OnSessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      if (_session != null)
      {
        Debug.Log("Session was deinitialized: " + _session.StageIdentifier);
        _session.Deinitialized -= OnSessionDeinitialized;
        _session.Ran -= OnSessionRan;
        _session.Paused -= OnSessionPaused;
        _session.FrameUpdated -= OnFrameUpdated;
        _session = null;
      }
      
      UpdateControlButtons(false);
    }

    private void OnSessionRan(ARSessionRanArgs args)
    {
      UpdateControlButtons(true);
    }

    private void OnSessionPaused(ARSessionPausedArgs args)
    {
      UpdateControlButtons(false);
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      if (_depthLoadingPopup && args.Frame.Depth != null)
      {
        _depthLoadingPopup.SetActive(false);
        _session.FrameUpdated -= OnFrameUpdated;
      }
    }
    
    private void OnLocalizationUpdated(LocalizationEventArgs args)
    {
      if (_statusLabel)
      {
        var dotdotdot = new string('.', _counter);
        _counter = (_counter + 1) % 4;
        _statusLabel.text = "Localization attempt underway" + dotdotdot;
      }
    }

    private void OnLocalizationSucceeded(LocalizationEventArgs args)
    {
      if (_statusLabel)
      {
        Vector3 offset = Vector3.zero;
        Vector3 euler = Vector3.zero;

        if (args.CoordinateSpace != null)
        {
          var transform = args.CoordinateSpace.Transform;
          offset = transform.ToPosition();
          euler = transform.ToRotation().eulerAngles;
        }

        _statusLabel.text = string.Format
        (
          "Localization Updated\noffset: {0}, angles: {1}\nconfidence: {2}\nguid: {3}",
          offset,
          euler,
          args.Confidence,
          args.CoordinateSpace?.Id
        );
      }
    }

    private void OnLocalizationFailed(LocalizationEventArgs args)
    {
      if (_statusLabel)
      {
        _statusLabel.text = string.Format
        (
          "Localization Failed\nreason: {0}",
          args.FailureReason
        );
      }
    }

    private void OnLocalizationCleared(LocalizationEventArgs args)
    {
      if (_statusLabel)
      {
        _statusLabel.text = "Localization Cleared";
      }
    }
    
    public void CopyLatestMapId()
    {
      if (_localizationEventManager == null)
      {
        Debug.LogWarning("CopyLatestMapId without a localization event manager!");
        return;
      }
      
      if (_mapIdField == null)
      {
        Debug.LogWarning("CopyLatestMapId without a map ID field!");
        return;
      }

      _mapIdField.text = _localizationEventManager.LatestLocalization != null
        ? _localizationEventManager.LatestLocalization.ToString()
        : string.Empty;
    }
    
    public void ApplyManualLocalization()
    {
      if (_localizationEventManager == null)
      {
        Debug.LogWarning("ApplyManualLocalization without a localization event manager!");
        return;
      }

      float tx = 0.0f;
      float ty = 0.0f;
      float tz = 0.0f;
      float rx = 0.0f;
      float ry = 0.0f;
      float rz = 0.0f;

      if (_txField && !string.IsNullOrEmpty(_txField.text))
        float.TryParse(_txField.text, out tx);

      if (_tyField && !string.IsNullOrEmpty(_tyField.text))
        float.TryParse(_tyField.text, out ty);

      if (_tzField && !string.IsNullOrEmpty(_tzField.text))
        float.TryParse(_tzField.text, out tz);

      if (_rxField && !string.IsNullOrEmpty(_rxField.text))
        float.TryParse(_rxField.text, out rx);

      if (_ryField && !string.IsNullOrEmpty(_ryField.text))
        float.TryParse(_ryField.text, out ry);

      if (_rzField && !string.IsNullOrEmpty(_rzField.text))
        float.TryParse(_rzField.text, out rz);

      Matrix4x4 transform = Matrix4x4.TRS
      (
        new Vector3(tx, ty, tz),
        Quaternion.Euler(rx, ry, rz),
        Vector3.one
      );

      float confidence = 1.0f;
      if (_confidenceField && !string.IsNullOrEmpty(_confidenceField.text))
        float.TryParse(_confidenceField.text, out confidence);

      string mapId = "manual";
      if (_mapIdField && !string.IsNullOrEmpty(_mapIdField.text))
        mapId = _mapIdField.text;

      Debug.LogFormat
      (
        "Apply Manual Localization (id={0}, tx={1}, ty={2}, tz={3}, rx={4}, ry={5}, rz={6}, c={7})",
        mapId,
        tx,
        ty,
        tz,
        rx,
        ry,
        rz,
        confidence
      );

      _localizationEventManager.DebugLocalizationUpdated(mapId, transform, confidence);
    }

    public void ResetLocalization()
    {
      if (_localizationEventManager == null)
      {
        Debug.LogWarning("ResetLocalization without a localization manager!");
        return;
      }

      Debug.Log("Reset Localization");

      _localizationEventManager.DebugLocalizationCleared();
    }

    public void FailManualLocalization()
    {
      if (_localizationEventManager == null)
      {
        Debug.LogWarning("FailManualLocalization without a localization manager!");
        return;
      }

      Debug.Log("Fail Manual Localization");

      _localizationEventManager.DebugLocalizationFailed();
    }

    public void HardCodedLocalization()
    {
      Debug.Log("Hard Coded Localization");
    }

    public void HardCodedLocalizationFail()
    {
      Debug.Log("Hard Coded Localization Fail");
    }

    public void EnableDepthFeatures(bool enabled)
    {
      if (_depthManager)
      {
        if (enabled) 
        {
          _depthManager.EnableFeatures();
          _depthVisualization.enabled = true;
        }
        else
        {
          _depthManager.DisableFeatures();
          _depthVisualization.enabled = false;
        }
      }
    }
  }
}
