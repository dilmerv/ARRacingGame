// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Helpers
{
  /// <summary>
  /// A helper class that maintains session IDs and attempts to provide users with fresh IDs
  /// on each run of the scene. Designed for use on a prefab, to provide functionality
  /// by just being placed in a scene.
  /// </summary>
  public class SessionIDField:
    InputField
  {
    private IMultipeerNetworking _networking = null;

    /// <summary>
    /// Retrieves the entered session ID. If it's blank, populates it with a default value.
    /// </summary>
    /// <returns>The session ID stored in the InputField</returns>
    public string GetSessionID()
    {
      return text;
    }

    protected override void Start()
    {
      base.Start();

      MultipeerNetworkingFactory.NetworkingInitialized += _NetworkingInitialized;
    }

    protected override void OnDestroy()
    {
      var args = new DisconnectedArgs();
      OnWillDeinitialize(args);

      base.OnDestroy();
    }

    private void _NetworkingInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      // Only supports a single network
      if (_networking != null)
        return;

      _networking = args.Networking;
      _networking.Disconnected += OnWillDeinitialize;
    }

    private void OnWillDeinitialize(DisconnectedArgs args)
    {
      if (_networking == null)
        return;

      _networking.Disconnected -= OnWillDeinitialize;

      _networking = null;
    }
  }
}
