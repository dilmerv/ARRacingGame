// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using Niantic.ARDK.Networking.HLAPI.Routing;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.PongHLAPI
{
  /// <summary>
  /// Controls the game logic and creation of objects
  /// </summary>
  public class GameController:
    MonoBehaviour
  {
    /// Prefabs to be instantiated when the game starts
    [SerializeField]
    private NetworkedUnityObject playingFieldPrefab = null;

    [SerializeField]
    private NetworkedUnityObject ballPrefab = null;

    [SerializeField]
    private NetworkedUnityObject playerPrefab = null;

    /// Reference to the StartGame button
    [SerializeField]
    private GameObject startGame = null;

    [SerializeField]
    private Button joinButton = null;

    [SerializeField]
    private FeaturePreloadManager preloadManager = null;

    /// Reference to AR Camera, used for hit test
    [SerializeField]
    private Camera _camera = null;

    /// References to game objects after instantiation
    private GameObject _ball;

    private GameObject _player;
    private GameObject _playingField;
    private GameObject _opponent;

    /// The score
    public Text score;

    /// HLAPI Networking objects
    private IHlapiSession _manager;

    private IAuthorityReplicator _auth;
    private MessageStreamReplicator<Vector3> _hitStreamReplicator;

    private INetworkedField<string> _scoreText;
    private int _redScore;
    private int _blueScore;
    private INetworkedField<Vector3> _fieldPosition;
    private INetworkedField<byte> _gameStarted;

    /// Cache your location every frame
    private Vector3 _location;

    /// Some fields to provide a lockout upon hitting the ball, in case the hit message is not
    /// processed in a single frame
    private bool _recentlyHit = false;

    private int _hitLockout = 0;

    private IARNetworking _arNetworking;
    private BallBehaviour _ballBehaviour;

    private bool _isHost;
    private IPeer _self;

    private bool _gameStart;
    private bool _synced;

    private void Start()
    {
      startGame.SetActive(false);
      ARNetworkingFactory.ARNetworkingInitialized += OnAnyARNetworkingSessionInitialized;

      if (preloadManager.AreAllFeaturesDownloaded())
        OnPreloadFinished(true);
      else
        preloadManager.ProgressUpdated += PreloadProgressUpdated;
    }

    private void PreloadProgressUpdated(FeaturePreloadManager.PreloadProgressUpdatedArgs args)
    {
      if (args.PreloadAttemptFinished)
      {
        preloadManager.ProgressUpdated -= PreloadProgressUpdated;
        OnPreloadFinished(args.FailedPreloads.Count == 0);
      }
    }

    private void OnPreloadFinished(bool success)
    {
      if (success)
        joinButton.interactable = true;
      else
        Debug.LogError("Failed to download resources needed to run AR Multiplayer");
    }

    // When all players are ready, create the game. Only the host will have the option to call this
    public void StartGame()
    {
      startGame.SetActive(false);

      //_gameStart = true;
      _gameStarted.Value = Convert.ToByte(true);
      _ballBehaviour.GameStart(_isHost);
    }

    // Instantiate game objects
    private void InstantiateObjects(Vector3 position)
    {
      if (_playingField != null && _isHost)
      {
        Debug.Log("Relocating the playing field!");
        _fieldPosition.Value = new Optional<Vector3>(position);
        _player.transform.position = position + new Vector3(0, 0, -1);
        _playingField.transform.position = position;
        _ball.transform.position = position;

        return;
      }

      Debug.Log("Instantiating the playing field!");

      // Both players want to spawn an avatar that they are the Authority of
      var startingOffset =
        _isHost ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);

      _player =
        playerPrefab.NetworkSpawn
        (
          _arNetworking.Networking,
          position + startingOffset,
          Quaternion.identity,
          Role.Authority
        )
        .gameObject;

      // Only the host should spawn the remaining objects
      if (!_isHost)
        return;

      // Instantiate the playing field at floor level
      _playingField =
        playingFieldPrefab.NetworkSpawn
        (
          _arNetworking.Networking,
          position,
          Quaternion.identity
        )
        .gameObject;

      // Set the score text for all players
      _scoreText.Value = "Score: 0 - 0";

      // Spawn the ball and set up references
      _ballBehaviour = ballPrefab.NetworkSpawn
        (
          _arNetworking.Networking,
          position,
          Quaternion.identity
        )
        .DefaultBehaviour as BallBehaviour;

      _ball = _ballBehaviour.gameObject;

      _ballBehaviour.Controller = this;
    }

    // Reset the ball when a goal is scored, increase score for player that scored
    // Only the host should call this method
    internal void GoalScored(string color)
    {
      // color param is the color of the goal that the ball went into
      // we score points by getting the ball in our opponent's goal
      if (color == "red")
      {
        Debug.Log
        (
          "Point scored for team blue. " +
          "Setting score via HLAPI. Only host will receive this log entry."
        );

        _blueScore += 1;
      }
      else
      {
        Debug.Log
        (
          "Point scored for team red. " +
          "Setting score via HLAPI. Only host will receive this log entry."
        );

        _redScore += 1;
      }

      _scoreText.Value = string.Format("Score: {0} - {1}", _redScore, _blueScore);
    }

    // Every frame, detect if you have hit the ball
    // If so, either bounce the ball (if host) or tell host to bounce the ball
    private void Update()
    {
      if (_manager != null)
        _manager.SendQueuedData();

      if (_synced && !_gameStart && _isHost)
      {
        if (PlatformAgnosticInput.touchCount <= 0)
          return;

        var touch = PlatformAgnosticInput.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
          var distance =
            Vector2.Distance
            (
              touch.position,
              new Vector2(startGame.transform.position.x, startGame.transform.position.y)
            );

          if (distance <= 80)
            return;

          FindFieldLocation(touch);
        }
      }

      if (!_gameStart)
        return;

      if (_recentlyHit)
      {
        _hitLockout += 1;

        if (_hitLockout >= 15)
        {
          _recentlyHit = false;
          _hitLockout = 0;
        }
      }

      var distance2 = Vector3.Distance(_player.transform.position, _ball.transform.position);
      if (distance2 > .25 || _recentlyHit)
        return;

      var bounceDirection = _ball.transform.position - _player.transform.position;
      bounceDirection = Vector3.Normalize(bounceDirection);
      _recentlyHit = true;

      if (_isHost)
        _ballBehaviour.Hit(bounceDirection);
      else
        _hitStreamReplicator.SendMessage(bounceDirection, _auth.PeerOfRole(Role.Authority));
    }

    private void FindFieldLocation(Touch touch)
    {
      var currentFrame = _arNetworking.ARSession.CurrentFrame;

      if (currentFrame == null)
        return;

      var results =
        currentFrame.HitTest
        (
          _camera.pixelWidth,
          _camera.pixelHeight,
          touch.position,
          ARHitTestResultType.ExistingPlaneUsingExtent
        );

      if (results.Count <= 0)
      {
        Debug.Log("Unable to place the field at the chosen location. Can't find a valid surface");

        return;
      }

      // Get the closest result
      var result = results[0];

      var hitPosition = result.WorldTransform.ToPosition();

      InstantiateObjects(hitPosition);
    }

    // Every updated frame, get our location from the frame data and move the local player's avatar
    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      _location = MatrixUtils.PositionFromMatrix(args.Frame.Camera.Transform);

      if (_player == null)
        return;

      var playerPos = _player.transform.position;
      playerPos.x = _location.x;
      _player.transform.position = playerPos;
    }

    private void OnPeerStateReceived(PeerStateReceivedArgs args)
    {
      if (_self.Identifier != args.Peer.Identifier)
      {
        if (args.State == PeerState.Stable)
        {
          _synced = true;

          if (_isHost)
          {
            startGame.SetActive(true);
            InstantiateObjects(_location);
          }
          else
          {
            InstantiateObjects(_arNetworking.LatestPeerPoses[args.Peer].ToPosition());
          }
        }

        return;
      }

      string message = args.State.ToString();
      score.text = message;
      Debug.Log("We reached state " + message);
    }

    private void OnDidConnect(ConnectedArgs connectedArgs)
    {
      _isHost = connectedArgs.IsHost;
      _self = connectedArgs.Self;

      _manager = new HlapiSession(19244);

      var group = _manager.CreateAndRegisterGroup(new NetworkId(4321));
      _auth = new GreedyAuthorityReplicator("pongHLAPIAuth", group);

      _auth.TryClaimRole(_isHost ? Role.Authority : Role.Observer, () => {}, () => {});

      var authToObserverDescriptor =
        _auth.AuthorityToObserverDescriptor(TransportType.ReliableUnordered);

      _fieldPosition =
        new NetworkedField<Vector3>("fieldReplicator", authToObserverDescriptor, group);

      _fieldPosition.ValueChangedIfReceiver += OnFieldPositionDidChange;

      _scoreText = new NetworkedField<string>("scoreText", authToObserverDescriptor, group);
      _scoreText.ValueChanged += OnScoreDidChange;

      _gameStarted = new NetworkedField<byte>("gameStarted", authToObserverDescriptor, group);

      _gameStarted.ValueChanged +=
        value =>
        {
          _gameStart = Convert.ToBoolean(value.Value.Value);

          if (_gameStart)
            _ball = FindObjectOfType<BallBehaviour>().gameObject;
        };

      _hitStreamReplicator =
        new MessageStreamReplicator<Vector3>
        (
          "hitMessageStream",
          _arNetworking.Networking.AnyToAnyDescriptor(TransportType.ReliableOrdered),
          group
        );

      _hitStreamReplicator.MessageReceived +=
        (args) =>
        {
          Debug.Log("Ball was hit");

          if (_auth.LocalRole != Role.Authority)
            return;

          _ballBehaviour.Hit(args.Message);
        };
    }

    private void OnFieldPositionDidChange(NetworkedFieldValueChangedArgs<Vector3> args)
    {
      var value = args.Value;
      if (!value.HasValue)
        return;

      var offsetPos = value.Value + new Vector3(0, 0, 1);
      _player.transform.position = offsetPos;
    }

    private void OnScoreDidChange(NetworkedFieldValueChangedArgs<string> args)
    {
      score.text = args.Value.GetOrDefault();
    }

    private void OnAnyARNetworkingSessionInitialized(AnyARNetworkingInitializedArgs args)
    {
      _arNetworking = args.ARNetworking;
      _arNetworking.PeerStateReceived += OnPeerStateReceived;

      _arNetworking.ARSession.FrameUpdated += OnFrameUpdated;
      _arNetworking.Networking.Connected += OnDidConnect;
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnAnyARNetworkingSessionInitialized;

      if (_arNetworking != null)
      {
        _arNetworking.PeerStateReceived -= OnPeerStateReceived;
        _arNetworking.ARSession.FrameUpdated -= OnFrameUpdated;
        _arNetworking.Networking.Connected -= OnDidConnect;
      }
    }
  }
}
