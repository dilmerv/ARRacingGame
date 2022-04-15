// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  /// A Unity component that manages an ARSession's lifetime. The session can either be created and
  /// run automatically through Unity lifecycle events, or can be controlled programatically.
  /// Any outstanding sessions are  always cleaned up on destruction. Integrates with the
  /// CapabilityChecker to ensure the device supports AR.
  ///
  /// If ManageUsingUnityLifecycle is true:
  ///   OnAwake(): an ARSession will be initialized
  ///   OnEnable(): the ARSession will be run
  ///   OnDisable(): the ARSession will be paused
  ///   OnDestroy(): the ARSession will be disposed
  /// Else:
  ///   Call Initialize() to initialize an ARSession
  ///   Call EnableFeatures() to run the ARSession
  ///   Call DisableFeatures() to pause the ARSession
  ///   Call Deinitialize() to dispose the ARSession
  ///
  /// @note
  ///   Because the CapabilityChecker's method for checking device support is async, the above
  ///   events (i.e. initialization of an ARSession) may not happen on the exact frame as
  ///   the method (OnAwake or Initialize) is invoked.
  [DisallowMultipleComponent]
  [RequireComponent(typeof(CapabilityChecker))]
  public class ARSessionManager:
    ARConfigChanger
  {
    /// If unspecified, will default to trying to create a live device session, then remote, then mock.
    /// If specified, will throw an exception if the source is not supported on the current platform.
    [SerializeField]
    private RuntimeEnvironment RuntimeEnvironment = RuntimeEnvironment.Default;

    /// Options used to transition the AR session's current state if you re-run it.
    [SerializeField]
    private ARSessionRunOptions _runOptions = ARSessionRunOptions.None;

    /// Should be true if this ARSessionManager is being used in conjunction with an ARNetworkingManager.
    [SerializeField]
    private bool _useWithARNetworkingSession;

    [SerializeField]
    [Tooltip("A boolean specifying whether or not camera images are analyzed to estimate scene lighting.")]
    private bool _isLightEstimationEnabled = false;

    [SerializeField]
    [Tooltip("A value specifying whether the camera should use autofocus or not when running.")]
    private bool _isAutoFocusEnabled = false;

    [SerializeField]
    [Tooltip("An iOS-only value specifying how the session maps the real-world device motion into a coordinate system.")]
    private WorldAlignment _worldAlignment = WorldAlignment.Gravity;

    private CapabilityChecker _capabilityChecker;

    // Variables used to track when their Inspector-public counterparts are changed in OnValidate
    private bool _prevLightEstimationEnabled;
    private bool _prevAutoFocusEnabled;
    private WorldAlignment _prevWorldAlignment;

    private Guid _stageIdentifier = default;

    private IARSession _arSession;
    private bool _shouldBeRunning;

    public IARSession ARSession
    {
      get
      {
        return _arSession;
      }
    }

    public bool IsLightEstimationEnabled
    {
      get
      {
        return _isLightEstimationEnabled;
      }
      set
      {
        if (value != _isLightEstimationEnabled)
        {
          _isLightEstimationEnabled = value;
          RaiseConfigurationChanged();
        }
      }
    }

    public bool IsAutoFocusEnabled
    {
      get
      {
        return _isAutoFocusEnabled;
      }
      set
      {
        if (value != _isAutoFocusEnabled)
        {
          _isAutoFocusEnabled = value;
          RaiseConfigurationChanged();
        }
      }
    }

    public WorldAlignment WorldAlignment
    {
      get
      {
        return _worldAlignment;
      }
      set
      {
        if (value != _worldAlignment)
        {
          _worldAlignment = value;
          RaiseConfigurationChanged();
        }
      }
    }

    protected override bool _CanReinitialize
    {
      get { return true; }
    }

    protected override void InitializeImpl()
    {
      base.InitializeImpl();
      
      _prevLightEstimationEnabled = _isLightEstimationEnabled;
      _prevAutoFocusEnabled = _isAutoFocusEnabled;
      _prevWorldAlignment = _worldAlignment;

      _capabilityChecker = GetComponent<CapabilityChecker>();

      if (_useWithARNetworkingSession)
        MultipeerNetworkingFactory.NetworkingInitialized += ListenForStage;

      if (_capabilityChecker.HasSucceeded)
        ScheduleCreateAndRunOnNextUpdate();
      else
        _capabilityChecker.Success.AddListener(ScheduleCreateAndRunOnNextUpdate);
    }

    // Queues a callback to create an ARSession and, if AreFeaturesEnabled is true, run the session
    // on the next Unity Update tick
    private void ScheduleCreateAndRunOnNextUpdate()
    {
      ARSessionManager manager = this;

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (manager != null && manager.Initialized)
          {
            manager.CreateAndRun();
          }
        }
      );
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      MultipeerNetworkingFactory.NetworkingInitialized -= ListenForStage;

      if (_arSession == null)
        return;

      _arSession.Dispose();
      _arSession = null;
    }

    private void ListenForStage(AnyMultipeerNetworkingInitializedArgs args)
    {
      // If multiple networkings were created, the ARSessionManager will use the stage of the
      // most recently created networking.
      _stageIdentifier = args.Networking.StageIdentifier;
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      _shouldBeRunning = true;

      if (_arSession != null)
        Run();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      _shouldBeRunning = false;
      _capabilityChecker.Success.RemoveListener(CreateAndRun);

      if (_arSession != null)
        _arSession.Pause();
    }

    /// Creates the session so that Run can be called later.
    /// This will only create the session if the capability checker was successful.
    private void Create()
    {
      if (_arSession != null)
      {
        ARLog._Warn("Did not create an ARSession because one already exists.");
        return;
      }

      if (!_capabilityChecker.HasSucceeded)
      {
        ARLog._Error("Failed to initialize ARSession because capability check has not yet passed.");
        return;
      }

      if (_useWithARNetworkingSession && _stageIdentifier != Guid.Empty)
        _arSession = ARSessionFactory.Create(RuntimeEnvironment, _stageIdentifier);
      else
        _arSession = ARSessionFactory.Create(RuntimeEnvironment);

      ARLog._DebugFormat("Created {0} ARSession: {1}.", false, _arSession.RuntimeEnvironment, _arSession.StageIdentifier);

      // Just in case the dev disposes the ARSession themselves instead of through this manager
      _arSession.Deinitialized += (_) => _arSession = null;
    }

    /// Runs an already created session with the provided options.
    private void Run()
    {
      if (_arSession == null)
      {
        ARLog._Error("Failed to run ARSession because one was not initialized.");
        return;
      }

      // Config changes are made later in the ApplyARConfigurationChange method. That way,
      // this class is able to intercept and alter the ARConfiguration every ARSession is run with,
      // even if the session is run outside of this method.
      var worldConfig = ARWorldTrackingConfigurationFactory.Create();
      _arSession.Run(worldConfig, _runOptions);
    }

    /// Initializes and runs the session.
    private void CreateAndRun()
    {
      Create();

      if (_shouldBeRunning)
        Run();
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      var config = properties.ARConfiguration;

      config.IsLightEstimationEnabled = _isLightEstimationEnabled;
      config.WorldAlignment = _worldAlignment;
      properties.RunOptions |= _runOptions;

      if (config is IARWorldTrackingConfiguration worldConfig)
        worldConfig.IsAutoFocusEnabled = _isAutoFocusEnabled;
    }

    private void OnValidate()
    {
      var configChanged = false;

      if (_isLightEstimationEnabled != _prevLightEstimationEnabled)
      {
        _prevLightEstimationEnabled = _isLightEstimationEnabled;
        configChanged = true;
      }

      if (_isAutoFocusEnabled != _prevAutoFocusEnabled)
      {
        _prevAutoFocusEnabled = _isAutoFocusEnabled;
        configChanged = true;
      }

      if (_worldAlignment != _prevWorldAlignment)
      {
        _prevWorldAlignment = _worldAlignment;
        configChanged = true;
      }

      if (configChanged)
        RaiseConfigurationChanged();
    }
  }
}
