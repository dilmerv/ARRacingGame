// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDKExamples.Pong
{
  /// <summary>
  /// Class that handles the ball's behaviour
  /// Only the host can affect the ball's properties, all other players must listen
  /// </summary>
  public class BallBehaviour:
    MonoBehaviour
  {
    internal GameController Controller = null;

    private Vector3 _pos;

    // Left and right boundaries of the field, in meters
    private float _lrBound = 2.5f;

    // Forward and backwards boundaries of the field, in meters
    private float _fbBound = 2.5f;

    // Initial velocity, in meters per second
    private float _initialVelocity = 1.0f;
    private Vector3 _velocity;

    // Cache the floor level, so the ball is reset properly
    private Vector3 _initialPosition;

    // Flags for whether the game has started and if the local player is the host
    private bool _isGameStarted;
    private bool _isHost;

    /// Reference to the messaging manager
    private MessagingManager _messagingManager;

    // Store the start location of the ball
    private void Start()
    {
      _initialPosition = transform.position;
    }

    // Set up the initial conditions
    internal void GameStart(bool isHost, MessagingManager messagingManager)
    {
      _isHost = isHost;
      _isGameStarted = true;
      _initialPosition = transform.position;

      if (!_isHost)
        return;

      _messagingManager = messagingManager;
      _velocity = new Vector3(_initialVelocity, 0, _initialVelocity);
    }

    // Signal that the ball has been hit, with a unit vector representing the new direction
    internal void Hit(Vector3 direction)
    {
      if (!_isGameStarted || !_isHost)
        return;

      _velocity = direction * _initialVelocity;
      _initialVelocity *= 1.1f;
    }

    // Perform movement, send position to non-host player
    private void Update()
    {
      if (!_isGameStarted || !_isHost)
        return;

      _pos = gameObject.transform.position;
      _pos.x += _velocity.x * Time.deltaTime;
      _pos.z += _velocity.z * Time.deltaTime;

      transform.position = _pos;

      _messagingManager.BroadcastBallPosition(_pos);

      if (_pos.x > _initialPosition.x + _lrBound)
        _velocity.x = -_initialVelocity;
      else if (_pos.x < _initialPosition.x - _lrBound)
        _velocity.x = _initialVelocity;

      if (_pos.z > _initialPosition.z + _fbBound)
        _velocity.z = -_initialVelocity;
      else if (_pos.z < _initialPosition.z - _fbBound)
        _velocity.z = _initialVelocity;
    }

    // Signal to host that a goal has been scored
    private void OnTriggerEnter(Collider other)
    {
      if (!_isGameStarted || !_isHost)
        return;

      _initialVelocity = 1.0f;
      _velocity = new Vector3(0, 0, _initialVelocity);
      transform.position = _initialPosition;

      switch (other.gameObject.tag)
      {
        case "RedGoal":
          Controller.GoalScored("red");
          break;

        case "BlueGoal":
          Controller.GoalScored("blue");
          break;
      }
    }
  }
}
