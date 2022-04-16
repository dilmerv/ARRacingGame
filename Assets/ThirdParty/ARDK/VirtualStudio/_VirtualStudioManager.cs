// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.VirtualStudio.AR.Networking;
using Niantic.ARDK.VirtualStudio.AR.Networking.Mock;
using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.VirtualStudio.Networking.Mock;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio
{
  internal sealed class _VirtualStudioManager:
    _IVirtualStudioManager
  {
    public const string LOCAL_PLAYER_NAME = "LocalPlayer";

    public static _IVirtualStudioManager Instance
    {
      get
      {
        _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _instance);

        lock (_instanceLock)
        {
          if (_instance == null)
            _instance = new _VirtualStudioManager();

          return _instance;
        }
      }
    }

    internal static void _ResetInstance()
    {
      _VirtualStudioManager instance;

      lock (_instanceLock)
        instance = _instance;

      if (instance != null)
        instance.Dispose();  // The Dispose actually resets the instance.
    }

    public _IEditorARSessionMediator ArSessionMediator
    {
      get { return _arSessionMediator; }
    }

    public _IEditorMultipeerSessionMediator MultipeerMediator
    {
      get { return _multipeerMediator; }
    }

    public _IEditorARNetworkingSessionMediator ArNetworkingMediator
    {
      get { return _arNetworkingMediator; }
    }

    public MockPlayer LocalPlayer
    {
      get { return _localPlayer; }
    }

    public MockPlayConfiguration PlayConfiguration { get; private set; }

    private static _VirtualStudioManager _instance;
    private static object _instanceLock = new object();

    private readonly _IEditorARSessionMediator _arSessionMediator;
    private readonly _IEditorMultipeerSessionMediator _multipeerMediator;
    private readonly _IEditorARNetworkingSessionMediator _arNetworkingMediator;

    private readonly Dictionary<Guid, string> _arSessionIdentifierToPlayerName =
      new Dictionary<Guid, string>();

    private readonly Dictionary<Guid, string> _networkingIdentifierToPlayerName =
      new Dictionary<Guid, string>();

    private readonly Dictionary<string, MockPlayer> _mockPlayers =
      new Dictionary<string, MockPlayer>();

    private MockPlayer _localPlayer;
    private IMultipeerNetworking _networking;
    private IARNetworking _arNetworking;
    private IARSession _arSession;

    private _VirtualStudioManager()
    {
      _arSessionMediator = new _MockARSessionMediator(this);
      _multipeerMediator = new _MockNetworkingSessionsMediator(this);
      _arNetworkingMediator = new _MockARNetworkingSessionsMediator(this);

      _CallbackQueue.ApplicationWillQuit += Dispose;

      ARSessionFactory.SessionInitialized += SetLocalStage;
      MultipeerNetworkingFactory.NetworkingInitialized += SetLocalStage;
      ARNetworkingFactory.ARNetworkingInitialized += SetLocalStage;

      ARLog._Debug("VirtualStudioMaster Initialized.");
    }

    ~_VirtualStudioManager()
    {
      ARLog._Error("_VirtualStudioManager was not correctly disposed.");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      _instance = null;

      ARSessionFactory.SessionInitialized -= SetLocalStage;

      var oldSession = _arSession;
      if (oldSession != null)
        oldSession.Deinitialized -= RemoveLocalStage;

      MultipeerNetworkingFactory.NetworkingInitialized -= SetLocalStage;

      var oldNetworking = _networking;
      if (oldNetworking != null)
        oldNetworking.Deinitialized -= _RemoveLocalStageFromMultipeerNetworking;

      ARNetworkingFactory.ARNetworkingInitialized -= SetLocalStage;

      var arNetworking = _arNetworking;
      if (arNetworking != null)
      {
        _arNetworking = null;
        arNetworking.Deinitialized -= RemoveLocalStage;
      }

      // ARNetworking objects must be disposed before ARSession and MultipeerNetworking,
      // because methods subscribed to ARNetworking.WillDeinitialize often use that
      // ARNetworking's Networking reference
      _arNetworkingMediator.Dispose();

      _arSessionMediator.Dispose();
      _multipeerMediator.Dispose();

      ARLog._Debug("VirtualStudioMaster Disposed.");
    }

    private void SetLocalStage(AnyARSessionInitializedArgs args)
    {
      // Camera controller must be initialized for both remote and mock players,
      // so don't check for mock implementation here.

      var player = SetLocalPlayerIfNeeded();
      if (player == null)
        return;

      var oldSession = _arSession;
      if (oldSession != null)
        oldSession.Deinitialized -= RemoveLocalStage;

      var arSession = args.Session;
      _arSession = arSession;
      _arSessionIdentifierToPlayerName.Add(arSession.StageIdentifier, player.Name);
      player.SetARSession(arSession);
      arSession.Deinitialized += RemoveLocalStage;
    }

    private void SetLocalStage(AnyMultipeerNetworkingInitializedArgs args)
    {
      var mockNetworking = args.Networking as _MockMultipeerNetworking;
      if (mockNetworking == null)
        return;

      var player = SetLocalPlayerIfNeeded();
      if (player == null)
        return;

      var oldNetworking = _networking;
      if (oldNetworking != null)
        oldNetworking.Deinitialized -= _RemoveLocalStageFromMultipeerNetworking;

      _networking = mockNetworking;
      _networkingIdentifierToPlayerName.Add(_networking.StageIdentifier, player.Name);
      player.SetMultipeerNetworking(mockNetworking);
      _networking.Deinitialized += _RemoveLocalStageFromMultipeerNetworking;
    }

    private void SetLocalStage(AnyARNetworkingInitializedArgs args)
    {
      var mockARNetworking = args.ARNetworking as _MockARNetworking;
      if (mockARNetworking == null)
        return;

      var player = SetLocalPlayerIfNeeded();
      if (player == null)
        return;

      var oldARNetworking = _arNetworking;
      if (oldARNetworking != null)
        oldARNetworking.Deinitialized -= RemoveLocalStage;

      _arNetworking = mockARNetworking;
      mockARNetworking.Deinitialized += RemoveLocalStage;
      player.SetARNetworking(mockARNetworking);
    }

    private MockPlayer SetLocalPlayerIfNeeded()
    {
      if (_localPlayer == null)
        _localPlayer = CreatePlayer(null);

      return _localPlayer;
    }

    private void RemoveLocalStage(ARSessionDeinitializedArgs args)
    {
      if (_arSession == null)
        return;

      LocalPlayer.SetARSession(null);

      _arSessionIdentifierToPlayerName.Remove(_arSession.StageIdentifier);
      _arSession = null;
    }

    private void _RemoveLocalStageFromMultipeerNetworking(DeinitializedArgs args)
    {
      if (_networking == null)
        return;

      LocalPlayer.SetMultipeerNetworking(null);

      _networkingIdentifierToPlayerName.Remove(_networking.StageIdentifier);
      _networking = null;
    }

    private void RemoveLocalStage(ARNetworkingDeinitializedArgs args)
    {
      if (_arNetworking == null)
        return;

      LocalPlayer.SetARNetworking(null);
      _arNetworking = null;
    }

    public void InitializeForConfiguration(MockPlayConfiguration playConfiguration)
    {
      if (PlayConfiguration != null)
      {
        ARLog._Error("Virtual Studio was already initialized with a play configuration.");
        return;
      }

      PlayConfiguration = playConfiguration;
      foreach (var profile in playConfiguration.ActiveProfiles)
      {
        if (!InitializeProfile(profile))
        {
          ARLog._ErrorFormat
          (
            "Failed trying to start session for profile {0}.",
            profile.PlayerName
          );
        }
      }
    }

    // Constructs the MockPlayer and its required of ARSession, MultipeerNetworking,
    // and ARNetworking sessions for, as defined by the input MockArdkPlayerProfile.
    private bool InitializeProfile(MockArdkPlayerProfile playerProfile)
    {
      if (GetPlayer(playerProfile.PlayerName) != null)
      {
        ARLog._ErrorFormat
        (
          "A player with the name {0} has already been initialized.",
          playerProfile.PlayerName
        );
        return false;
      }

      var stageIdentifier = Guid.NewGuid();
      var player = CreatePlayer(playerProfile);

      if (playerProfile.UsingNetwork)
      {
        // Only mock networkings are compatible with mock players
        var networking =
          _multipeerMediator.CreateNonLocalSession(stageIdentifier, playerProfile.RuntimeEnvironment);

        player.SetMultipeerNetworking(networking);
        _networkingIdentifierToPlayerName.Add(stageIdentifier, player.Name);
      }

      if (playerProfile.UsingAR)
      {
        var arSession =
          _arSessionMediator.CreateNonLocalSession(stageIdentifier, playerProfile.RuntimeEnvironment);

        player.SetARSession(arSession);
        _arSessionIdentifierToPlayerName.Add(stageIdentifier, player.Name);
      }

      if (playerProfile.UsingARNetworking)
      {
        var arNetworking = _arNetworkingMediator.CreateNonLocalSession(stageIdentifier);

        player.SetARNetworking(arNetworking);
      }

      return true;
    }

    private MockPlayer CreatePlayer(MockArdkPlayerProfile playerProfile)
    {
      // Create player and add to map
      var mockPlayer = new MockPlayer(this, playerProfile);

      var playerName = mockPlayer.Name;
      _mockPlayers.Add(playerName, mockPlayer);

      return mockPlayer;
    }

    public MockPlayer GetPlayer(string playerName)
    {
      MockPlayer player;

      if (_mockPlayers.TryGetValue(playerName, out player))
        return player;

      return null;
    }

    public MockPlayer GetPlayer(Guid stageIdentifier)
    {
      string playerName;
      MockPlayer player = null;

      var foundPlayerName =
        _arSessionIdentifierToPlayerName.TryGetValue(stageIdentifier, out playerName) ||
        _networkingIdentifierToPlayerName.TryGetValue(stageIdentifier, out playerName);

      if (foundPlayerName)
        _mockPlayers.TryGetValue(playerName, out player);

      return player;
    }

    public MockPlayer GetPlayerWithPeer(IPeer peer)
    {
      var mockPeer = peer as _MockPeer;
      if (mockPeer == null)
      {
        ARLog._Error("This method is only valid while running MultipeerNetworking in Mock mode.");
        return null;
      }
      else
      {
        var peerStageIdentifier = mockPeer.StageIdentifier;
        var networking = MultipeerMediator.GetSession(peerStageIdentifier);

        if (networking != null)
        {
          var playerName = _arSessionIdentifierToPlayerName[networking.StageIdentifier];
          return _mockPlayers[playerName];
        }

        return null;
      }
    }
  }
}