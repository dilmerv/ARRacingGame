// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Pong
{
  /// Controls the game logic and creation of objects
  public class GameController:
    MonoBehaviour
  {
    /// Reference to AR Camera, used for hit test
    [SerializeField]
    private Camera _camera = null;

    [SerializeField]
    private FeaturePreloadManager preloadManager = null;

    [Header("UI")]
    [SerializeField]
    private Button joinButton = null;

    [SerializeField]
    private GameObject startGameButton = null;

    public Text scoreText;

    /// Prefabs to be instantiated when the game starts
    [Header("Gameplay Prefabs")]
    [SerializeField]
    private GameObject playingFieldPrefab = null;

    [SerializeField]
    private GameObject ballPrefab = null;

    [SerializeField]
    private GameObject playerPrefab = null;

    /// References to game objects after instantiation
    private GameObject _ball;
    private GameObject _player;
    private GameObject _playingField;
    private GameObject _opponent;

    internal int RedScore;
    internal int BlueScore;

    /// Cache your location every frame
    private Vector3 _location;

    /// Some fields to provide a lockout upon hitting the ball, in case the hit message is not
    /// processed in a single frame
    private bool _recentlyHit;

    private int _hitLockout;

    private bool _objectsSpawned;

    private IARNetworking _arNetworking;

    private BallBehaviour _ballBehaviour;
    private MessagingManager _messagingManager;

    private IPeer _host;
    private IPeer _self;
    private bool _isHost;

    private bool _isGameStarted;
    private bool _isSynced;

    private void Start()
    {
      startGameButton.SetActive(false);
      ARNetworkingFactory.ARNetworkingInitialized += OnAnyARNetworkingSessionInitialized;
      preloadManager.ProgressUpdated += PreloadProgressUpdated;
    }

    private void PreloadProgressUpdated(FeaturePreloadManager.PreloadProgressUpdatedArgs args)
    {
      if (args.PreloadAttemptFinished)
      {
        if (args.FailedPreloads.Count > 0)
        {
          Debug.LogError("Failed to download resources needed to run AR Multiplayer");
          return;
        }

        joinButton.interactable = true;
        preloadManager.ProgressUpdated -= PreloadProgressUpdated;
      }
    }

    // When all players are ready, create the game. Only the host will have the option to call this
    public void StartGame()
    {
      if (!_objectsSpawned)
        InstantiateObjects(_location);

      startGameButton.SetActive(false);
      _isGameStarted = true;
      _ballBehaviour.GameStart(_isHost, _messagingManager);
    }

    // Instantiate game objects
    internal void InstantiateObjects(Vector3 position)
    {
      if (_playingField != null)
      {
        Debug.Log("Relocating the playing field!");
        _playingField.transform.position = position;

        var offset = _isHost ? new Vector3(0, 0, -2) : new Vector3(0, 0, 2);

        // Instantiate the player and opponent avatars at opposite sides of the field
        _player.transform.position = position + offset;
        offset.z *= -1;
        _opponent.transform.position = position + offset;
        _ball.transform.position = position;

        if (_isHost)
          _messagingManager.SpawnGameObjects(position);

        return;
      }

      scoreText.text = "Score: 0 - 0";

      // Instantiate the playing field at floor level
      Debug.Log("Instantiating the playing field!");
      _playingField = Instantiate(playingFieldPrefab, position, Quaternion.identity);

      // Determine the starting location for the local player based on whether or not it is host
      var startingOffset = _isHost ? new Vector3(0, 0, -2) : new Vector3(0, 0, 2);

      // Instantiate the player and opponent avatars at opposite sides of the field
      _player = Instantiate(playerPrefab, position + startingOffset, Quaternion.identity);
      startingOffset.z *= -1;
      _opponent = Instantiate(playerPrefab, position + startingOffset, Quaternion.identity);

      // Instantiate the ball at floor level, and hook up all references correctly
      _ball = Instantiate(ballPrefab, position, Quaternion.identity);
      _ballBehaviour = _ball.GetComponent<BallBehaviour>();
      _messagingManager.SetBallReference(_ballBehaviour);
      _ballBehaviour.Controller = this;

      _objectsSpawned = true;

      if (!_isHost)
        return;

      _messagingManager.SpawnGameObjects(position);
    }

    // Reset the ball when a goal is scored, increase score for player that scored
    // Only the host should call this method
    internal void GoalScored(string color)
    {
      // color param is the color of the goal that the ball went into
      // we score points by getting the ball in our opponent's goal
      if (color == "red")
      {
        Debug.Log("Point scored for team blue");
        BlueScore += 1;
      }
      else
      {
        Debug.Log("Point scored for team red");
        RedScore += 1;
      }

      scoreText.text = string.Format("Score: {0} - {1}", RedScore, BlueScore);

      _messagingManager.GoalScored(color);
    }

    // Set the ball location for non-host players
    internal void SetBallLocation(Vector3 position)
    {
      if (!_isGameStarted)
        _isGameStarted = true;

      _ball.transform.position = position;
    }

    // Every frame, detect if you have hit the ball
    // If so, either bounce the ball (if host) or tell host to bounce the ball
    private void Update()
    {
      if (_isSynced && !_isGameStarted && _isHost)
      {
        if (PlatformAgnosticInput.touchCount <= 0)
          return;

        var touch = PlatformAgnosticInput.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
          var startGameDistance =
            Vector2.Distance
            (
              touch.position,
              new Vector2(startGameButton.transform.position.x, startGameButton.transform.position.y)
            );

          if (startGameDistance > 80)
            FindFieldLocation(touch);
        }
      }

      if (!_isGameStarted)
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

      var ballDistance = Vector3.Distance(_player.transform.position, _ball.transform.position);
      if (ballDistance > .5 || _recentlyHit)
        return;

      Debug.Log("We hit the ball!");
      var bounceDirection = _ball.transform.position - _player.transform.position;
      bounceDirection = Vector3.Normalize(bounceDirection);
      _recentlyHit = true;

      if (_isHost)
        _ballBehaviour.Hit(bounceDirection);
      else
        _messagingManager.BallHitByPlayer(_host, bounceDirection);
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
      if (_self.Identifier == args.Peer.Identifier)
        UpdateOwnState(args);
      else
        UpdatePeerState(args);
    }

    private void UpdatePeerState(PeerStateReceivedArgs args)
    {
      if (args.State == PeerState.Stable)
      {
        _isSynced = true;

        if (_isHost)
          startGameButton.SetActive(true);
      }
    }

    private void UpdateOwnState(PeerStateReceivedArgs args)
    {
      string message = args.State.ToString();
      scoreText.text = message;
      Debug.Log("We reached state " + message);
    }

    // Upon receiving a peer's location data, take its location and move its avatar
    private void OnPeerPoseReceived(PeerPoseReceivedArgs args)
    {
      if (_opponent == null)
        return;

      var peerLocation = MatrixUtils.PositionFromMatrix(args.Pose);

      var opponentPosition = _opponent.transform.position;
      opponentPosition.x = peerLocation.x;
      _opponent.transform.position = opponentPosition;
    }

    private void OnDidConnect(ConnectedArgs args)
    {
      _self = args.Self;
      _host = args.Host;
      _isHost = args.IsHost;
    }

    private void OnAnyARNetworkingSessionInitialized(AnyARNetworkingInitializedArgs args)
    {
      _arNetworking = args.ARNetworking;
      _arNetworking.PeerPoseReceived += OnPeerPoseReceived;
      _arNetworking.PeerStateReceived += OnPeerStateReceived;

      _arNetworking.ARSession.FrameUpdated += OnFrameUpdated;
      _arNetworking.Networking.Connected += OnDidConnect;

      _messagingManager = new MessagingManager();
      _messagingManager.InitializeMessagingManager(args.ARNetworking.Networking, this);
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnAnyARNetworkingSessionInitialized;

      if (_arNetworking != null)
      {
        _arNetworking.PeerPoseReceived -= OnPeerPoseReceived;
        _arNetworking.PeerStateReceived -= OnPeerStateReceived;
        _arNetworking.ARSession.FrameUpdated -= OnFrameUpdated;
        _arNetworking.Networking.Connected -= OnDidConnect;
      }

      if (_messagingManager != null)
      {
        _messagingManager.Destroy();
        _messagingManager = null;
      }
    }
  }
}
