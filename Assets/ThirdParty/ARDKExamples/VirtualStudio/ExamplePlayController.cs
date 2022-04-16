// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.VirtualStudio.AR.Networking;
using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDKExamples.VirtualStudio
{
  /// <summary>
  /// Example script for running script-only testing of game flow using the VirtualStudio mock
  /// ARDK objects. Script-only control can be used while in UnityEditor's PlayMode as an alternative
  /// to using the VirtualStudio window, or it can be used to write PlayMode tests.
  /// </summary>
  public class ExamplePlayController : MonoBehaviour
  {
    [SerializeField]
    private MockPlayConfiguration _playConfiguration = null;

    private IARSession _arSession;
    private IARNetworking _arNetworking;
    private IMultipeerNetworking _networking;

    private byte[] _sessionIdentifier;
    private IARWorldTrackingConfiguration _arConfiguration;

    private void Start()
    {
      ARLog.EnableLogFeature("Niantic.ARDK.VirtualStudio");
      // These static actions will only be invoked for "real," i.e. local, ARSession,
      // MultipeerNetworking, and ARNetworking objects. Their behaviour during a mock playthrough
      // with multiple mock ARDK objects active is the same as during a live device playthrough.
      //
      // Note: References to the session objects can simply be set when constructing the object,
      // but the static method is used here just to exemplify the above point.
      ARSessionFactory.SessionInitialized +=
        (args) =>
        {
          _arSession = args.Session;
          Debug.Log("Local ARSession initialized");
        };

      MultipeerNetworkingFactory.NetworkingInitialized +=
        (args) =>
        {
          _networking = args.Networking;
          Debug.Log("Local Networking initialized");
        };

      ARNetworkingFactory.ARNetworkingInitialized +=
        (args) =>
        {
          _arNetworking = args.ARNetworking;
          Debug.Log("Local ARNetworking initialized");
        };

      _sessionIdentifier = Encoding.UTF8.GetBytes("Quickstart");
      _arConfiguration = ARWorldTrackingConfigurationFactory.Create();
      _arConfiguration.PlaneDetection = PlaneDetection.Horizontal | PlaneDetection.Vertical;
      _arConfiguration.IsSharedExperienceEnabled = true;


      // Comment out one of the below 3 lines to play through the selected scenario.
      StartCoroutine("PlaythroughConnects");
      // StartCoroutine("PlaythroughDisconnect");
      // StartCoroutine("PlaythroughMessageHandling");
    }

    // Example of testing how the local player handles other players joining the
    // ARNetworking session and localizing.
    private IEnumerator PlaythroughConnects()
    {
      // First initialize, connect to, and run the local player's ARNetworking session.
      // This code looks the same whether using constructing to with a LiveDevice, Remote, or
      // Mock RuntimeEnvironment.
      var arNetworking = ARNetworkingFactory.Create();
      arNetworking.Networking.Join(_sessionIdentifier);
      arNetworking.ARSession.Run(_arConfiguration);

      // Listen for local events
      arNetworking.ARSession.MapsAdded += OnMapsAdded;
      arNetworking.Networking.PeerAdded += OnPeerAdded;
      arNetworking.PeerStateReceived += OnPeerStateReceived;
      arNetworking.PeerPoseReceived += OnPeerPoseReceived;

      // And then every 2 seconds, connect a mock player's networking session and run their
      // AR session (if they are enabled in the play configuration). If the mock player also has
      // ARNetworking enabled, they will localize and then receive/broadcast peer poses.
      foreach (var profile in _playConfiguration.ActiveProfiles)
      {
        if (profile.UsingNetwork)
        {
          yield return new WaitForSeconds(2f);

          var player = profile.GetPlayer();
          if (player.Networking != null)
            player.Networking.Join(_sessionIdentifier);

          if (player.ARSession != null)
            player.ARSession.Run(_arConfiguration);
        }
      }

      yield return new WaitForSeconds(2f);

      foreach (var profile in _playConfiguration.ActiveProfiles)
      {
        if (profile.UsingARNetworking)
        {
          var player = profile.GetPlayer();
          if (player.ARNetworking != null)
            player.SetPeerState(PeerState.Stable);
        }
      }
    }

    // Example of testing how the local player handles a player disconnecting from the
    // ARNetworking session.
    private IEnumerator PlaythroughDisconnect()
    {
      // First initialize, connect to, and run the local player's ARNetworking session
      var arNetworking = ARNetworkingFactory.Create();
      arNetworking.Networking.Join(_sessionIdentifier);
      arNetworking.ARSession.Run(_arConfiguration);

      // Listen for when local networking removes a peer
      arNetworking.Networking.PeerRemoved += OnPeerRemoved;

      // Connect all the mock player's networking sessions.
      _playConfiguration.ConnectAllPlayersNetworkings(_sessionIdentifier);
      _playConfiguration.RunAllPlayersARSessions(_arConfiguration);

      yield return new WaitForSeconds(2f);

      // Disconnect one of the mock players.
      foreach (var profile in _playConfiguration.ActiveProfiles)
      {
        var player = profile.GetPlayer();
        if (player.Networking != null && player.Networking.IsConnected)
        {
          Debug.LogFormat("Player {0} is disconnecting.", player);
          player.Networking.Leave();
          yield break;
        }
      }
    }

    // Example of testing how the local player handles network messages, and how to set up
    // custom message handling for mock players.
    private void PlaythroughMessageHandling()
    {
      // First initialize and connect the local player's MultipeerNetworking session
      var networking = MultipeerNetworkingFactory.Create();
      networking.Join(_sessionIdentifier);

      // Listen for when local networking receives data
      networking.PeerDataReceived += OnPeerDataReceived;

      // Connect all the mock player's networking sessions
      _playConfiguration.ConnectAllPlayersNetworkings(_sessionIdentifier);

      // Set up custom message handling for mock players
      foreach (var profile in _playConfiguration.ActiveProfiles)
      {
        var player = profile.GetPlayer();
        player.SetMessageHandler(new ExampleMessageHandler());
      }

      // Send a message from each mock player to the local player
      foreach (var profile in _playConfiguration.ActiveProfiles)
      {
        var player = profile.GetPlayer();
        if (player.Networking == null)
          continue;

        // Send a message from the mock player to the local player
        player.Networking.SendDataToPeer
        (
          (uint) ExampleMessageTag.AppleCount,
          BitConverter.GetBytes(UnityEngine.Random.Range(2, 100)),
          networking.Self,
          TransportType.ReliableOrdered
        );

        // Send a message from the local player to the mock player
        _networking.SendDataToPeer
        (
          (uint) ExampleMessageTag.OrangeCount,
          BitConverter.GetBytes(UnityEngine.Random.Range(2, 100)),
          player.Networking.Self,
          TransportType.ReliableOrdered
        );
      }
    }

    private void OnDestroy()
    {
      if (_arSession != null)
        _arSession.Dispose();

      if (_networking != null)
        _networking.Dispose();

      if (_arNetworking != null)
        _arNetworking.Dispose();
    }

    private void OnMapsAdded(MapsArgs args)
    {
      Debug.LogFormat
      (
        "Added {0} maps (total = {1})",
        args.Maps.Count,
        _arSession.CurrentFrame.Maps.Count
      );
    }
    private void OnPeerAdded(PeerAddedArgs args)
    {
      Debug.LogFormat("Added player: {0}", _playConfiguration.GetPlayerWithPeer(args.Peer));
    }

    private void OnPeerRemoved(PeerRemovedArgs args)
    {
      // Cannot call _playConfiguration.GetPlayerWithPeer(peer) because the peer no longer exists
      // outside of the instance here
      Debug.LogFormat("Removed peer: {0}", args.Peer);
    }

    private void OnPeerStateReceived(PeerStateReceivedArgs args)
    {
      Debug.LogFormat
      (
        "Received state {0} from player {1})",
        args.State,
        _playConfiguration.GetPlayerWithPeer(args.Peer)
      );
    }

    private void OnPeerPoseReceived(PeerPoseReceivedArgs args)
    {
      Debug.LogFormat
      (
        "Received pose (T: {0} R: {1}) from player {2})",
        args.Pose.ToPosition(),
        args.Pose.ToRotation(),
        _playConfiguration.GetPlayerWithPeer(args.Peer)
      );
    }

    private void OnPeerDataReceived(PeerDataReceivedArgs args)
    {
      var messageTag = (ExampleMessageTag) args.Tag;
      var number = BitConverter.ToInt32(args.CopyData(), 0);;

      Debug.LogFormat
      (
        "Peer {0} wants to buy {1} {2} from the local peer",
        args.Peer.Identifier,
        number,
        messageTag == ExampleMessageTag.AppleCount ? "apples" : "oranges"
      );
    }
  }
}