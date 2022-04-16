// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

using UnityEngine;

namespace Niantic.ARDKExamples.Pong
{
  /// <summary>
  /// A manager that handles outgoing and incoming messages
  /// </summary>
  public class MessagingManager
  {
    // Reference to the networking object
    private IMultipeerNetworking _networking;

    // References to the local game controller and ball
    private GameController _controller;
    private BallBehaviour _ball;

    private readonly MemoryStream _builderMemoryStream = new MemoryStream(24);

    // Enums for the various message types
    private enum _MessageType:
      uint
    {
      BallHitMessage = 1,
      GoalScoredMessage,
      BallPositionMessage,
      SpawnGameObjectsMessage
    }

    // Initialize manager with relevant data and references
    internal void InitializeMessagingManager
    (
      IMultipeerNetworking networking,
      GameController controller
    )
    {
      _networking = networking;
      _controller = controller;

      _networking.PeerDataReceived += OnDidReceiveDataFromPeer;
    }

    // After the game is created, give a reference to the ball
    internal void SetBallReference(BallBehaviour ball)
    {
      _ball = ball;
    }

    // Signal to host that a non-host has hit the ball, host should handle logic
    internal void BallHitByPlayer(IPeer host, Vector3 direction)
    {
      _networking.SendDataToPeer
      (
        (uint)_MessageType.BallHitMessage,
        SerializeVector3(direction),
        host,
        TransportType.UnreliableUnordered
      );
    }

    // Signal to non-hosts that a goal has been scored, reset the ball and update score
    internal void GoalScored(String color)
    {
      var message = new byte[1];

      if (color == "red")
        message[0] = 0;
      else
        message[0] = 1;

      _networking.BroadcastData
      (
        (uint)_MessageType.GoalScoredMessage,
        message,
        TransportType.ReliableUnordered
      );
    }

    // Signal to non-hosts the ball's position every frame
    internal void BroadcastBallPosition(Vector3 position)
    {
      _networking.BroadcastData
      (
        (uint)_MessageType.BallPositionMessage,
        SerializeVector3(position),
        TransportType.UnreliableUnordered
      );
    }

    // Spawn game objects with a position and rotation
    internal void SpawnGameObjects(Vector3 position)
    {
      _networking.BroadcastData
      (
        (uint)_MessageType.SpawnGameObjectsMessage,
        SerializeVector3(position),
        TransportType.ReliableUnordered
      );
    }

    private void OnDidReceiveDataFromPeer(PeerDataReceivedArgs args)
    {
      //Debug.Log("Received message with tag: " + tag);

      var data = args.CopyData();
      switch ((_MessageType)args.Tag)
      {
        case _MessageType.BallHitMessage:
          _ball.Hit(DeserializeVector3(data));
          break;

        case _MessageType.GoalScoredMessage:
          if (data[0] == 0)
          {
            Debug.Log("Point scored for team blue");
            _controller.BlueScore += 1;
          }
          else
          {
            Debug.Log("Point scored for team red");
            _controller.RedScore += 1;
          }

          _controller.scoreText.text =
            string.Format
            (
              "Score: {0} - {1}",
              _controller.RedScore,
              _controller.BlueScore
            );

          break;

        case _MessageType.BallPositionMessage:
          _controller.SetBallLocation(DeserializeVector3(data));
          break;

        case _MessageType.SpawnGameObjectsMessage:
          Debug.Log("Creating game objects");
          _controller.InstantiateObjects(DeserializeVector3(data));
          break;

        default:
          throw new ArgumentException("Received unknown tag from message");
      }
    }

    // Remove callback from networking object on destruction
    internal void Destroy()
    {
      _networking.PeerDataReceived -= OnDidReceiveDataFromPeer;
    }

    // Helper to serialize a Vector3 into a byte[] to be passed over the network
    private byte[] SerializeVector3(Vector3 vector)
    {
      _builderMemoryStream.Position = 0;
      _builderMemoryStream.SetLength(0);

      using (var binarySerializer = new BinarySerializer(_builderMemoryStream))
        Vector3Serializer.Instance.Serialize(binarySerializer, vector);

      return _builderMemoryStream.ToArray();
    }

    // Helper to deserialize a byte[] received from the network into a Vector3
    private Vector3 DeserializeVector3(byte[] data)
    {
      using(var readingStream = new MemoryStream(data))
        using (var binaryDeserializer = new BinaryDeserializer(readingStream))
          return Vector3Serializer.Instance.Deserialize(binaryDeserializer);
    }
  }
}
