// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  /// A Unity component that manages an ARNetworking's lifetime. The session can either be started
  /// automatically through Unity lifecycle events, or can be controlled programatically.
  /// Any outstanding sessions are always cleaned up on destruction. Integrates with the
  /// ARSessionManager and NetworkSessionManager to make sure all components are set up correctly.
  ///
  /// If ManageUsingUnityLifecycle is true:
  ///   OnAwake():
  ///     An ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///     will be initialized
  ///   OnEnable():
  ///     The ARSession will be run and the MultipeerNetworking will join a session
  ///   OnDisable():
  ///     The ARSession will be paused and the MultipeerNetworking will leave the session
  ///   OnDestroy():
  ///     The ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///     will be disposed
  /// Else:
  ///   Call Initialize to:
  ///     Initialize an ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///   Call EnableFeatures to:
  ///     Run the ARSession and join the MultipeerNetworking
  ///   Call DisableFeatures to:
  ///     Pause the ARSession and leave the MultipeerNetworking session
  ///   Call Destroy to:
  ///     Dispose the ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///
  /// @note
  ///   Because the CapabilityChecker's method for checking device support is async, the above
  ///   events (i.e. initialization of ARNetworking) may not happen on the exact frame as
  ///   the method (OnAwake or Initialize) is invoked.
  [RequireComponent(typeof(ARSessionManager))]
  [RequireComponent(typeof(NetworkSessionManager))]
  [DisallowMultipleComponent]
  public class ARNetworkingManager: ARConfigChanger
  {
    private IARNetworking _arNetworking;
    private ARSessionManager _arSessionManager;
    private NetworkSessionManager _networkSessionManager;

    // Because the Initialized event is raised before ARSessionManager and NetworkSessionManager
    // are set their ARSession and MultipeerNetworking references, respectively, we have to keep
    // a reference here to use them to create the ARNetworking.
    private IARSession _arSession;
    private IMultipeerNetworking _networking;

    private bool _shouldBeRunning;
    private bool _needToRecreate;

    public IARNetworking ARNetworking
    {
      get { return _arNetworking; }
    }

    public ARSessionManager ARSessionManager
    {
      get { return _arSessionManager; }
    }

    public NetworkSessionManager NetworkSessionManager
    {
      get { return _networkSessionManager; }
    }

    protected override bool _CanReinitialize
    {
      get { return true; }
    }

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
      MultipeerNetworkingFactory.NetworkingInitialized += OnNetworkingInitialized;

      _arSessionManager = GetComponent<ARSessionManager>();
      _arSessionManager.Initialize();

      _networkSessionManager = GetComponent<NetworkSessionManager>();
      _networkSessionManager.Initialize();
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
      MultipeerNetworkingFactory.NetworkingInitialized -= OnNetworkingInitialized;

      if (_arNetworking == null)
        return;

      _arNetworking.Dispose();
      _arNetworking = null;

      _arSessionManager.Deinitialize();
      _networkSessionManager.Deinitialize();
    }

    private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      _arSession = args.Session;
      _arSession.Deinitialized += (_) => _arSession = null;

      if (_arNetworking == null && NetworkSessionManager.Networking != null)
      {
        Create();

        if (_shouldBeRunning)
          EnableSessionManagers();
      }
    }

    private void OnNetworkingInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      _networking = args.Networking;
      _networking.Deinitialized += (_) => _networking = null;

      if (_arNetworking == null && ARSessionManager.ARSession != null)
      {
        Create();

        if (_shouldBeRunning)
          EnableSessionManagers();
      }
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      _shouldBeRunning = true;

      RaiseConfigurationChanged();

      // A networking, once left, is useless because it cannot be used to join/re-join a session.
      // So _arNetworking.Networking will be destroyed by the NetworkSessionManager.DisableFeatures
      // call, meaning if this component is enabled again, a new ARNetworking instance has to be
      // created
      if (_arNetworking != null && _needToRecreate)
      {
        _arNetworking.Dispose();
        _arNetworking = null;
      }

      EnableSessionManagers();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      _shouldBeRunning = false;

      _arSessionManager.DisableFeatures();
      _networkSessionManager.DisableFeatures();
    }

    private void Create()
    {
      if (_arNetworking != null)
      {
        ARLog._Error("Failed to create an ARNetworking session because one already exists.");
        return;
      }

      // If the component was re-enabled, a new networking reference will have been set but
      // not a new ARSession reference, since the same ARSession is still being used.
      _arNetworking = ARNetworkingFactory.Create(_arSession ?? ARSessionManager.ARSession, _networking);

      _arNetworking.Networking.Connected += _ => _needToRecreate = true;

      // Just in case the dev disposes the ARNetworking themselves instead of through this manager
      _arNetworking.Deinitialized +=
        _ =>
        {
          _arNetworking = null;
          _needToRecreate = false;
        };
    }

    private void EnableSessionManagers()
    {
      _arSessionManager.EnableFeatures();
      _networkSessionManager.EnableFeatures();
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration worldConfig)
      {
        worldConfig.IsSharedExperienceEnabled = AreFeaturesEnabled;
      }
    }
  }
}
