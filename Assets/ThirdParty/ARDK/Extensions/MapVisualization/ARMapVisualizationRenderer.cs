// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

using UnityEngine;

namespace Niantic.ARDK.Extensions.MapVisualization
{
  /// @brief Helper class that visualizes scanned maps used for AR localization.
  ///
  /// Listens for new AR localization maps and instantiates visualization prefabs
  /// for each new map. To use, add an instance to your scene.
  /// Map visualizations are only valid when the AR localization is
  /// in a stable state where 
  /// [PeerState](@ref Niantic.ARDK.AR.Networking.PeerState) is 
  /// ```Stable``` or ```Stabilizing```.
  public class ARMapVisualizationRenderer: 
    MonoBehaviour
  {
    /// The object to spawn and manage when a map is detected.
    public ARMapController ARMapPrefab;

    private Dictionary<Guid, IMapVisualizationController> _mapLookup =
      new Dictionary<Guid, IMapVisualizationController>();

    private bool _mapVisibility = false;
    private IPeer _self;

    private IARSession _session;
    private IARNetworking _arNetworking;
    private IMultipeerNetworking _multipeerNetworking;

    private void Awake()
    {
      // Listen for maps
      ARSessionFactory.SessionInitialized += _SessionInitialized;
      
      ARNetworkingFactory.ARNetworkingInitialized += _NetworkingInitialized;
      MultipeerNetworkingFactory.NetworkingInitialized += _MultipeerNetworkingInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= _SessionInitialized;
      _RemoveSessionEvents(_session);
      
      ARNetworkingFactory.ARNetworkingInitialized -= _NetworkingInitialized;

      var oldNetworking = _arNetworking;
      if (oldNetworking != null)
        oldNetworking.PeerStateReceived -= _PeerStateReceived;

      var oldMultipeerNetworking = _multipeerNetworking;
      if (oldMultipeerNetworking != null)
        oldMultipeerNetworking.Connected -= _MultipeerNetworkingConnected;
    }

    private void _RemoveSessionEvents(IARSession session)
    {
      if (session == null)
        return;
      
      session.MapsAdded -= OnAnyMapsAdded;
      session.MapsUpdated -= OnAnyMapsUpdated;
      session.Deinitialized -= SessionDeinitialized;
    }

    private void _SessionInitialized(AnyARSessionInitializedArgs args)
    {
      _session = args.Session;
      _session.MapsAdded += OnAnyMapsAdded;
      _session.MapsUpdated += OnAnyMapsUpdated;
      _session.Deinitialized += SessionDeinitialized;
    }
    
    private void SessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      foreach (var map in _mapLookup.Values)
      {
        var mapBehaviour = map as ARMapController;
        if (mapBehaviour != null)
          Destroy(mapBehaviour.gameObject);
      }

      _mapLookup.Clear();
      _mapVisibility = false;
      _self = null;
      _session = null;
    }

    private void _NetworkingInitialized(AnyARNetworkingInitializedArgs args)
    {
      var oldNetworking = _arNetworking;
      if (oldNetworking != null)
        oldNetworking.PeerStateReceived -= _PeerStateReceived;

      _arNetworking = args.ARNetworking;
      _arNetworking.PeerStateReceived += _PeerStateReceived;
    }
    
    private void _MultipeerNetworkingInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      var oldMultipeerNetworking = _multipeerNetworking;
      if (oldMultipeerNetworking != null)
        oldMultipeerNetworking.Connected -= _MultipeerNetworkingConnected;

      var multipeerNetworking = args.Networking;
        _multipeerNetworking = multipeerNetworking;
      multipeerNetworking.Connected += _MultipeerNetworkingConnected;
    }
    
    private void OnAnyMapsAdded(MapsArgs args)
    {
      foreach (var map in args.Maps)
      {
        // Spawn a new map prefab
        _mapLookup.Add(map.Identifier, Instantiate(ARMapPrefab));
        RefreshMap(map, _mapVisibility);
      }
    }

    private void OnAnyMapsUpdated(MapsArgs args)
    {
      foreach (var map in args.Maps)
      {
        // Update existing map prefab
        RefreshMap(map, _mapVisibility);
      }
    }

    private void _PeerStateReceived(PeerStateReceivedArgs args)
    {
      if (args.Peer.Identifier != _self.Identifier || args.State != PeerState.Stable)
        return;

      // Show all maps
      _mapVisibility = true;
      foreach (var mapLookup in _mapLookup)
        mapLookup.Value.SetVisibility(_mapVisibility);
    }

    private void _MultipeerNetworkingConnected(ConnectedArgs args)
    {
      _self = args.Self;
    }

    private void RefreshMap(IARMap map, bool visibility)
    {
      var mapVisualizationController = _mapLookup[map.Identifier];
      mapVisualizationController.VisualizeMap(map);
      mapVisualizationController.SetVisibility(visibility);
    }
  }
}
