// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Networking.HLAPI.Data
{
  public struct NetworkedDataDescriptor
  {
    public Func<IEnumerable<IPeer>> GetSenders { get; private set; }
    public Func<IEnumerable<IPeer>> GetReceivers { get; private set; }
    public TransportType TransportType { get; private set; }

    public NetworkedDataDescriptor(
      Func<IEnumerable<IPeer>> getSenders,
      Func<IEnumerable<IPeer>> getReceivers,
      TransportType transportType)
      : this()
    {
      GetSenders = getSenders;
      GetReceivers = getReceivers;
      TransportType = transportType;
    }
  }

  public static class NetworkedDataDescriptorExtension
  {    
    public static NetworkedDataDescriptor AuthorityToObserverDescriptor(
      this IAuthorityReplicator auth,
      TransportType transportType)
    {
      return new NetworkedDataDescriptor(
        () => auth.PeersOfRole(Role.Authority),
        () => auth.PeersOfRole(Role.Observer),
        transportType);
    }

    public static NetworkedDataDescriptor ObserversToAuthorityDescriptor(
      this IAuthorityReplicator auth,
      TransportType transportType)
    {
      return new NetworkedDataDescriptor(
        () => auth.PeersOfRole(Role.Observer),
        () => auth.PeersOfRole(Role.Authority),
        transportType);
    }

    // TODO: unit tests
    public static NetworkedDataDescriptor AnyToAnyDescriptor(
      this IAuthorityReplicator auth,
      TransportType transportType)
    {
      return new NetworkedDataDescriptor(
        () => GetAllPeersFromAuth(auth),
        () => GetAllPeersFromAuth(auth),
        transportType
        );
    }

    public static NetworkedDataDescriptor AnyToAnyDescriptor(
      this IMultipeerNetworking networking,
      TransportType transportType)
    {
      return new NetworkedDataDescriptor(
        () => GetAllPeersFromNetworking(networking),
        () => GetAllPeersFromNetworking(networking),
        transportType);
    }

    private static IEnumerable<IPeer> GetAllPeersFromAuth(IAuthorityReplicator auth)
    {
      return auth.Group.Session.Networking == null ? 
               EmptyArray<IPeer>.Instance : 
               GetAllPeersFromNetworking(auth.Group.Session.Networking);
    }

    private static IEnumerable<IPeer> GetAllPeersFromNetworking(IMultipeerNetworking networking)
    {
      if (!networking.IsConnected)
        yield break;

      foreach (var peer in networking.OtherPeers)
        yield return peer;

      yield return networking.Self;
    }
  }
}
