// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.VersionUtilities;

using UnityEngine;

namespace Niantic.ARDKExamples
{
  /// A simple networking-only example using the low level networking API.
  public class LowLevelNetworkingExampleManager:
    MonoBehaviour
  {
    // A reference to a multipeer networking instance. The main interaction point for the low-level
    // multipeer API.
    private IMultipeerNetworking _networking;

    // Magic number to denote a debug message type
    private const int TRANSPORT_TAG = 3;

    private void Awake()
    {
      MultipeerNetworkingFactory.NetworkingInitialized += OnNetworkInitialized;
    }

    private void OnDestroy()
    {
      MultipeerNetworkingFactory.NetworkingInitialized -= OnNetworkInitialized;
      _networking = null;
    }

    private void OnNetworkInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      _networking = args.Networking;
      _networking.Connected += OnNetworkConnected;
      _networking.Deinitialized += OnDeinitialized;
    }

    private void OnNetworkConnected(ConnectedArgs args)
    {
      // Print ARBE version in the scrolling log. ARBE version is only available
      // after connected callback
      Debug.LogFormat("Connected to ARBE [{0}]", ARDKGlobalVersion.GetARBEVersion());
    }

    private void OnDeinitialized(DeinitializedArgs args)
    {
      _networking = null;
    }
  }
}
