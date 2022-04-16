// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.External;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio
{
  [CreateAssetMenu(fileName = "MockPlayConfiguration", menuName = "ARDK/MockPlayConfiguration", order = 0)]
  public class MockPlayConfiguration:
    ScriptableObject
  {
    [SerializeField]
    private List<MockArdkPlayerProfile> _profiles;

    /// (Optional) Prefab that will spawn to represent active mock players. These spawned
    /// GameObjects can be moved in the Unity Editor to change the player's broadcasted pose.
    [SerializeField]
    private GameObject _mockPlayerPrefab;

    private MockArdkPlayerProfile[] _activeProfiles;

    private _IVirtualStudioManager _virtualStudioManager;

    [NonSerialized]
    internal bool _Initialized;

    public MockArdkPlayerProfile[] ActiveProfiles
    {
      get
      {
        if (!_Initialized)
          _Initialize();

        return _activeProfiles;
      }
    }

    private IReadOnlyCollection<MockArdkPlayerProfile> _readonlyProfiles;

    public IReadOnlyCollection<MockArdkPlayerProfile> Profiles
    {
      get
      {
        if (_readonlyProfiles == null)
          _readonlyProfiles = _profiles.AsArdkReadOnly();

        return _readonlyProfiles;
      }
    }

    /// <summary>
    /// Initialize method for when when a non-Inspector defined MockPlayConfiguration
    /// is needed.
    /// </summary>
    /// <param name="profiles"></param>
    /// <param name="playerPrefab"></param>
    public void SetInspectorValues
    (
      List<MockArdkPlayerProfile> profiles,
      GameObject playerPrefab
    )
    {
      _profiles = profiles;
      _mockPlayerPrefab = playerPrefab;
    }

    /// <summary>
    /// Constructs the required ARSession, MultipeerNetworking, and ARNetworking sessions for all
    /// the mock players as defined in the list of MockArdkPlayerProfiles.
    /// </summary>
    internal void _Initialize(_IVirtualStudioManager virtualStudioManager = null)
    {
      if (_Initialized)
        return;

      ARLog._DebugFormat("Initializing all mock players in {0}", objs: name);
      _Initialized = true;
      _virtualStudioManager = virtualStudioManager ?? _VirtualStudioManager.Instance;

      var activeProfiles = new List<MockArdkPlayerProfile>();

      foreach (var profile in _profiles)
      {
        if (!profile.IsActive)
          continue;

        profile.SpawnPlayerObjectDelegate = SpawnPlayerObject;
        activeProfiles.Add(profile);
      }

      _activeProfiles = activeProfiles.ToArray();
      _virtualStudioManager.InitializeForConfiguration(this);
    }

    /// <summary>
    /// Invokes the Join method on all the active players' IMultipeerNetworking components.
    /// </summary>
    /// <param name="sessionMetadata">Metadata of session to join.</param>
    public void ConnectAllPlayersNetworkings(byte[] sessionMetadata)
    {
      if (!_Initialized)
        _Initialize();

      foreach (var profile in ActiveProfiles)
      {
        var player = profile.GetPlayer();
        if (player.Networking != null)
          player.Networking.Join(sessionMetadata);
      }
    }

    /// <summary>
    /// Invokes the Run method on all the active player's IARSession components.
    /// </summary>
    /// <param name="arConfiguration">ARConfiguration to run with.</param>
    public void RunAllPlayersARSessions(IARConfiguration arConfiguration = null)
    {
      if (!_Initialized)
        _Initialize();

      foreach (var profile in ActiveProfiles)
      {
        var player = profile.GetPlayer();
        var config = arConfiguration ?? GetARConfiguration(profile);
        if (player.ARSession != null)
          player.ARSession.Run(config);
      }
    }

    /// <summary>
    /// Returns the MockPlayer that owns the MultipeerNetworking session that the input peer
    /// is the local ("self") peer of.
    /// </summary>
    /// <param name="peer"></param>
    public MockPlayer GetPlayerWithPeer(IPeer peer)
    {
      if (!_Initialized)
        _Initialize();

      return _virtualStudioManager.GetPlayerWithPeer(peer);
    }

    /// <summary>
    /// Invoked when a new MockPlayer is constructed. This base method simply spawns a
    /// pre-defined prefab, but it can be overriden by a child implementation of desired.
    /// This will only be invoked for players defined in this MockPlayConfiguration,
    /// ie only for remote mock players. A GameObject can be set for the local player through
    /// the MockPlayer.SetPlayerObject method.
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
    protected virtual GameObject SpawnPlayerObject(MockArdkPlayerProfile profile)
    {
      if (_mockPlayerPrefab == null)
        return null;

      var playerObject = Instantiate(_mockPlayerPrefab);
      playerObject.name = profile.PlayerName + "_mock";

      return playerObject;
    }

    internal IARConfiguration GetARConfiguration(MockArdkPlayerProfile profile)
    {
      var config = ARWorldTrackingConfigurationFactory.Create();
      config.PlaneDetection = PlaneDetection.Horizontal | PlaneDetection.Vertical;
      config.IsSharedExperienceEnabled = profile.UsingARNetworking;

      return config;
    }
  }
}