// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.ComponentModel;

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDK.Extensions
{
  /// A helper class to quickly visualize the Peer state of the local peer via color.
  /// - PeerState.Unknown = Gray
  /// - PeerState.Synchronizing = Red
  /// - PeerState.Synchronized = Green
  public class ARNetworkingStateVisualizer:
    MonoBehaviour
  {
    /// The image of whose color will be set depending upon the local peer's state.
    public Image SyncIndicator;

    private IARNetworking _networking;

    private void Awake()
    {
      ARNetworkingFactory.ARNetworkingInitialized += _OnNetworkingInitialized;
    }
    
    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= _OnNetworkingInitialized;
      
      var oldNetworking = _networking;
      if (oldNetworking != null)
        oldNetworking.PeerStateReceived -= _PeerStateReceived;
    }
    
    private void _OnNetworkingInitialized(AnyARNetworkingInitializedArgs args)
    {
      var oldNetworking = _networking;
      if (oldNetworking != null)
        oldNetworking.PeerStateReceived -= _PeerStateReceived;

      _networking = args.ARNetworking;
      _networking.PeerStateReceived += _PeerStateReceived;
    }

    private void _PeerStateReceived(PeerStateReceivedArgs args)
    {
      var networking = _networking;
      if (networking == null || !args.Peer.Equals(networking.Networking.Self))
        return;

      switch (args.State)
      {
        case PeerState.WaitingForLocalizationData:
          SyncIndicator.color = Color.yellow;
          break;

        case PeerState.Localizing:
          SyncIndicator.color = Color.blue;
          break;

        case PeerState.Stabilizing:
          SyncIndicator.color = Color.magenta;
          break;

        case PeerState.Failed:
          SyncIndicator.color = Color.red;
          break;

        case PeerState.Stable:
          SyncIndicator.color = Color.green;
          break;

        case PeerState.Unknown:
          SyncIndicator.color = Color.gray;
          break;

        default:
          throw new InvalidEnumArgumentException("state", (int)args.State, typeof(PeerState));
      }
    }
  }
}
