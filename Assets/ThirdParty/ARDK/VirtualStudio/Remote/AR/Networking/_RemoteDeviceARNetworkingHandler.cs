// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.AR.Networking.NetworkAnchors;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Marker;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  internal sealed class _RemoteDeviceARNetworkingHandler
  {
    private readonly IARNetworking _arNetworking;

    internal IARNetworking InnerARNetworking
    {
      get { return _arNetworking; }
    }

    internal _RemoteDeviceARNetworkingHandler(IARSession arSession, IMultipeerNetworking networking)
    {
      _arNetworking = ARNetworkingFactory.Create(arSession, networking);

      _arNetworking.PeerStateReceived += HandlePeerStateReceived;
      _arNetworking.PeerPoseReceived += HandlePeerPoseReceived;
      _arNetworking.Deinitialized += HandleDeinitialized;

      _EasyConnection.Register<ARNetworkingDestroyMessage>(message => Dispose());
    }

    ~_RemoteDeviceARNetworkingHandler()
    {
      ARLog._Error("_RemoteDeviceARNetworkingHandler should be destroyed by an explicit call to Dispose().");
    }

    private bool _isDestroyed;

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      if (_isDestroyed)
        return;

      _isDestroyed = true;

      _arNetworking.PeerStateReceived -= HandlePeerStateReceived;
      _arNetworking.PeerPoseReceived -= HandlePeerPoseReceived;
      _arNetworking.Deinitialized -= HandleDeinitialized;

      _EasyConnection.Unregister<ARNetworkingDestroyMessage>();

      _arNetworking?.Dispose();
    }

    private void HandlePeerStateReceived(PeerStateReceivedArgs args)
    {
      var message =
        new ARNetworkingPeerStateReceivedMessage
        {
          PeerState = args.State,
          PeerIdentifier = args.Peer.Identifier
        };

      _EasyConnection.Send(message);
    }

    private void HandlePeerPoseReceived(PeerPoseReceivedArgs args)
    {
      var message =
        new ARNetworkingPeerPoseReceivedMessage
        {
          PeerIdentifier = args.Peer.Identifier, Pose = args.Pose
        };

      _EasyConnection.Send(message);
    }

    private void HandleDeinitialized(ARNetworkingDeinitializedArgs args)
    {
      _EasyConnection.Send(new ARNetworkingDeinitializedMessage());
    }
  }
}
