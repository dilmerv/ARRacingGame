using System;
using System.Collections.Generic;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.VirtualStudio.Networking.Mock;

using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Editor
{
  [Serializable]
  internal sealed class _MockPlayConfigurationEditor
  {
    private const string PLAY_CONFIGURATION_KEY = "ARDK_PlayConfiguration";
    private const string INPUT_SESSION_ID_KEY = "ARDK_Input_Session_Identifier";

    [SerializeField]
    private MockPlayConfiguration _playConfiguration;

    [SerializeField]
    private string _inputSessionIdentifier;

    private byte[] _detectedSessionMetadata;

    [SerializeField]
    private int _fps;

    [SerializeField]
    private float _moveSpeed;

    [SerializeField]
    private int _lookSpeed;

    [SerializeField]
    private bool _scrollDirection;

    private Dictionary<string, bool> _foldoutStates =
      new Dictionary<string, bool>();

    private bool _listeningForJoin;
    private bool _isSelected;

    public _MockPlayConfigurationEditor()
    {
      if (_playConfiguration != null)
        _playConfiguration._Initialize();

      EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
      ListenForInitialize();
      _detectedSessionMetadata = null;
    }

    ~_MockPlayConfigurationEditor ()
    {
      EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
      MultipeerNetworkingFactory.NetworkingInitialized -= ListenForJoin;
      _listeningForJoin = false;
      _detectedSessionMetadata = null;
    }

    public void OnSelectionChange(bool isSelected)
    {
      _isSelected = isSelected;
    }

    private void OnEditorPlayModeStateChanged(PlayModeStateChange stateChange)
    {
      _detectedSessionMetadata = null;

      if (!_isSelected)
        return;

      switch (stateChange)
      {
        case PlayModeStateChange.EnteredPlayMode:
          ListenForInitialize();

          var existingConfig = _VirtualStudioManager.Instance.PlayConfiguration;
          if (existingConfig == null)
            _playConfiguration._Initialize();
          else
            _playConfiguration = existingConfig;

          break;

        case PlayModeStateChange.ExitingPlayMode:
          MultipeerNetworkingFactory.NetworkingInitialized -= ListenForJoin;
          _listeningForJoin = false;
          break;
      }
    }

    private void ListenForInitialize()
    {
      if (!_listeningForJoin)
      {
        MultipeerNetworkingFactory.NetworkingInitialized += ListenForJoin;
        _listeningForJoin = true;
      }
    }

    private void ListenForJoin(AnyMultipeerNetworkingInitializedArgs args)
    {
      args.Networking.Connected +=
        connectedArgs =>
        {
          if (args.Networking is _MockMultipeerNetworking mockNetworking)
            _detectedSessionMetadata = mockNetworking.JoinedSessionMetadata;
        };
    }

    public void LoadPreferences()
    {
      var playConfigurationName = PlayerPrefs.GetString(PLAY_CONFIGURATION_KEY, null);

      if (!string.IsNullOrEmpty(playConfigurationName))
        _playConfiguration = GetPlayConfiguration(playConfigurationName);

      _inputSessionIdentifier = PlayerPrefs.GetString(INPUT_SESSION_ID_KEY, "ABC");

      _fps = _MockCameraConfiguration.FPS;
      _moveSpeed = _MockCameraConfiguration.MoveSpeed;
      _lookSpeed = _MockCameraConfiguration.LookSpeed;
      _scrollDirection = _MockCameraConfiguration.ScrollDirection == -1;
    }

    private static MockPlayConfiguration GetPlayConfiguration(string name)
    {
      var filter = string.Format("{0} t:MockPlayConfiguration", name);
      var guids = AssetDatabase.FindAssets(filter);

      if (guids.Length == 0)
      {
        ARLog._WarnFormat("Could not load MockPlayConfiguration named: {0}", objs: name);
        return null;
      }

      var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
      var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(MockPlayConfiguration));

      ARLog._DebugFormat("Loaded MockPlayConfiguration named: {0}", objs: name);
      return asset as MockPlayConfiguration;
    }

    public void DrawGUI()
    {
      DrawCameraConfigurationGUI();

      GUILayout.Space(20);

      DrawPlayConfigurationSelector();
      DrawSessionMetadataGUI();

      GUILayout.Space(20);

      EditorGUILayout.LabelField("Players", VirtualStudioConfigurationEditor._HeaderStyle);
      GUILayout.Space(10);

      EditorGUI.BeginDisabledGroup(_playConfiguration == null);

      EditorGUI.BeginDisabledGroup(!Application.isPlaying);
      if (GUILayout.Button("Connect and Run All", GUILayout.Width(200)))
      {
        _playConfiguration.ConnectAllPlayersNetworkings(GetSessionMetadata());
        _playConfiguration.RunAllPlayersARSessions();
      }

      DrawLocalPlayer();
      EditorGUI.EndDisabledGroup();

      if (_playConfiguration != null)
      {
        foreach (var profile in _playConfiguration.Profiles)
        {
          DrawPlayerProfile(profile);
          GUILayout.Space(10);
        }
      }

      EditorGUI.EndDisabledGroup();
    }

    private void DrawCameraConfigurationGUI()
    {
      GUILayout.Label("Mock Camera Controls", EditorStyles.boldLabel);

      EditorGUI.BeginDisabledGroup(Application.isPlaying);

      var newFps = EditorGUILayout.IntField("FPS", _fps);
      if (newFps != _fps)
      {
        _fps = newFps;
        _MockCameraConfiguration.FPS = _fps;
      }

      EditorGUI.EndDisabledGroup();

      var newMovespeed = EditorGUILayout.Slider("Move Speed", _moveSpeed, 0.1f, 10f);
      if (newMovespeed != _moveSpeed)
      {
        _moveSpeed = newMovespeed;
        _MockCameraConfiguration.MoveSpeed = _moveSpeed;
      }

      var newLookSpeed = EditorGUILayout.IntSlider("Look Speed", _lookSpeed, 1, 180);
      if (newLookSpeed != _lookSpeed)
      {
        _lookSpeed = newLookSpeed;
        _MockCameraConfiguration.LookSpeed = _lookSpeed;
      }
      
      var newScrollDirection = EditorGUILayout.Toggle("Scroll Direction: Natural", _scrollDirection);
      if (newScrollDirection != _scrollDirection)
      {
        _scrollDirection = newScrollDirection;
        _MockCameraConfiguration.ScrollDirection = _scrollDirection ? -1 : 1;
      }
    }

    private void DrawPlayConfigurationSelector()
    {
      var newPlayConfiguration =
        (MockPlayConfiguration) EditorGUILayout.ObjectField
        (
          "Play Configuration",
          _playConfiguration,
          typeof(MockPlayConfiguration),
          false
        );

      if (_playConfiguration != newPlayConfiguration)
      {
        _playConfiguration = newPlayConfiguration;
        PlayerPrefs.SetString
        (
          PLAY_CONFIGURATION_KEY,
          _playConfiguration == null ? null : _playConfiguration.name
        );
      }
    }

    private void DrawLocalPlayer()
    {
      var localName = _VirtualStudioManager.LOCAL_PLAYER_NAME;
      if (!_foldoutStates.ContainsKey(localName))
        _foldoutStates.Add(localName, true);

      var showFoldout  = EditorGUILayout.Foldout(_foldoutStates[localName], localName);
      _foldoutStates[localName] = showFoldout;

      if (Application.isPlaying)
      {
        var localPlayer = _VirtualStudioManager.Instance.LocalPlayer;
        if (localPlayer == null)
          return;

        var arNetworking = localPlayer.ARNetworking;
        if (arNetworking != null && arNetworking.Networking.IsConnected)
        {
          var currState = arNetworking.LocalPeerState;
          var newState = (PeerState)EditorGUILayout.EnumPopup(currState);
          if (newState != currState)
            localPlayer.SetPeerState(newState);
        }
      }
    }

    private void DrawPlayerProfile(MockArdkPlayerProfile profile)
    {
      var playerName = profile.PlayerName;
      if (!_foldoutStates.ContainsKey(playerName))
        _foldoutStates.Add(playerName, true);

      var showFoldout = EditorGUILayout.Foldout(_foldoutStates[playerName], playerName);
      _foldoutStates[playerName] = showFoldout;

      if (!showFoldout)
        return;

      using (var horizontal = new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(300)))
      {
        GUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        using (var col1 = new GUILayout.VerticalScope())
        {
          var style = new GUILayoutOption[]
          {
            GUILayout.Width(150)
          };

          if (EditorGUILayout.ToggleLeft("Active", profile.IsActive, style) != profile.IsActive)
            profile.IsActive = !profile.IsActive;

          if (EditorGUILayout.ToggleLeft("Create AR", profile.UsingAR, style) != profile.UsingAR)
            profile.UsingAR = !profile.UsingAR;

          var newUsingNetworking =
            EditorGUILayout.ToggleLeft("Create Network", profile.UsingNetwork, style);

          if (newUsingNetworking != profile.UsingNetwork)
            profile.UsingNetwork = newUsingNetworking;

          var newUsingARNetworking =
            EditorGUILayout.ToggleLeft("Create ARNetworking", profile.UsingARNetworking, style);

          if (newUsingARNetworking != profile.UsingARNetworking)
            profile.UsingARNetworking = newUsingARNetworking;
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(20);

        if (!Application.isPlaying || !_playConfiguration._Initialized || !profile.IsActive)
          return;

        using (var col2 = new EditorGUILayout.VerticalScope())
        {
          var style = new[] { GUILayout.Width(100) };

          GUILayout.Space(18);
          EditorGUI.BeginDisabledGroup(!profile.UsingAR);

          var player = profile.GetPlayer();

          var arSession = player.ARSession;
          if (arSession != null && arSession.State == ARSessionState.Running)
          {
            if (GUILayout.Button("Dispose", style))
              arSession.Dispose();
          }
          else
          {
            if (GUILayout.Button("Run", style))
              arSession.Run(_playConfiguration.GetARConfiguration(profile));
          }

          EditorGUI.EndDisabledGroup();

          GUILayout.Space(2);
          EditorGUI.BeginDisabledGroup(!profile.UsingNetwork);
          var networking = player.Networking;
          if (networking != null && networking.IsConnected)
          {
            if (GUILayout.Button("Dispose", style))
              networking.Dispose();
          }
          else
          {
            if (GUILayout.Button("Join", style))
              networking.Join(GetSessionMetadata());
          }

          GUILayout.Space(2);
          var arNetworking = player.ARNetworking;
          if (arNetworking != null && arNetworking.Networking.IsConnected)
          {
            var currState = arNetworking.LocalPeerState;
            var newState = (PeerState)EditorGUILayout.EnumPopup(currState);
            if (newState != currState)
              player.SetPeerState(newState);
          }

          EditorGUI.EndDisabledGroup();
        }
      }
    }

    private void DrawSessionMetadataGUI()
    {
      if (_detectedSessionMetadata != null && _detectedSessionMetadata.Length > 0)
      {
        EditorGUILayout.LabelField("Session Identifier", "Detected");
        return;
      }

      var newInputSessionIdentifier =
        EditorGUILayout.TextField("Session Identifier", _inputSessionIdentifier);

      if (_inputSessionIdentifier != newInputSessionIdentifier)
      {
        _inputSessionIdentifier = newInputSessionIdentifier;
        PlayerPrefs.SetString(INPUT_SESSION_ID_KEY, _inputSessionIdentifier);
      }
    }

    private byte[] GetSessionMetadata()
    {
      if (_detectedSessionMetadata != null)
        return _detectedSessionMetadata;

      if (string.IsNullOrWhiteSpace(_inputSessionIdentifier))
      {
        ARLog._Error
        (
          "Must enter a non-empty session identifier in the Virtual Studio window " +
          "in order to join a networking session."
        );

        return null;
      }

      return Encoding.UTF8.GetBytes(_inputSessionIdentifier);
    }
  }
}