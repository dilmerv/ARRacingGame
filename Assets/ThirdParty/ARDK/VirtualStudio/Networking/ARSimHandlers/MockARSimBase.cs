// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Networking.Mock;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Networking.ARSimHandlers
{
  /// <summary>
  /// Extend this to mock an ARSim server. It will receive messages sent by clients, and provide
  ///   hooks to invoke ARM messages, statuses, and results.
  /// Networking sessions that interact with this mock server need to be explicitly registered,
  ///   rather than having this tool automatically handle all created sessions.
  /// Also adds a hook for invoking a `PersistentKeyValueUpdated` for registered networkings only,
  ///   since ARSim uses the Key-Value store for some functionality
  /// </summary>
  public abstract class MockARSimBase : IDisposable
  {
    private readonly List<_MockMultipeerNetworking> _routers =
      new List<_MockMultipeerNetworking>();

    public void RegisterNetworking(IMultipeerNetworking networking)
    {
      var router = networking as _MockMultipeerNetworking;
      if (router == null)
      {
        ARLog._Error("Cannot register networking sessions that are not mock sessions");
        return;
      }

      _routers.Add(router);
      router.ArmDataReceivedFromClient += HandleDataReceivedFromClient;
    }

    public void UnregisterNetworking(IMultipeerNetworking networking)
    {
      var router = networking as _MockMultipeerNetworking;
      if (router == null)
      {
        ARLog._Error("Cannot unregister networking sessions that are not mock sessions");
        return;
      }

      _routers.Remove(router);
      router.ArmDataReceivedFromClient -= HandleDataReceivedFromClient;
    }

    public void SendMessageToClients(uint tag, byte[] data)
    {
      if(_routers.Count == 0)
        return;

      var args = new DataReceivedFromArmArgs(tag, data);
      foreach (var router in _routers)
      {
        router._ReceiveDataFromArm(args);
      }
    }

    public void SendStatusToClients(uint status)
    {
      if(_routers.Count == 0)
        return;

      var args = new SessionStatusReceivedFromArmArgs(status);
      foreach (var router in _routers)
      {
        router._ReceiveStatusFromArm(args);
      }
    }

    public void SendResultToClients(uint outcome, byte[] details)
    {
      if(_routers.Count == 0)
        return;

      var args = new SessionResultReceivedFromArmArgs(outcome, details);
      foreach (var router in _routers)
      {
        router._ReceiveResultFromArm(args);
      }
    }

    // Only executes the event for the routers explicitly registered to this handler, since
    //   trying to execute on every connected session may lead to duplicate events. For
    //   the sake of mocking, just considering the explicit sessions should be enough.
    public void SetKeyValuePair(string key, byte[] value)
    {
      foreach (var router in _routers)
      {
        router.ReceivePersistentKeyValue(key, value);
      }
    }

    public void Dispose()
    {
      foreach (var router in _routers)
      {
        router.ArmDataReceivedFromClient -= HandleDataReceivedFromClient;
      }

      _routers.Clear();
    }

    protected abstract void DataReceivedFromClient(PeerDataReceivedArgs args);

    private void HandleDataReceivedFromClient(PeerDataReceivedArgs args)
    {
      DataReceivedFromClient(args);
    }
  }
}
