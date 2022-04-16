// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

/// @namespace Niantic.ARDK.Networking.ARSim
/// @brief A set of tools to support server-authoritative AR gameplay.
/// @note Currently in internal development, and not useable
namespace Niantic.ARDK.Networking.ARSim {
  /// <summary>
  /// A message stream that handles tagging and serialization of objects, and automatically executing
  ///   an Action upon receiving the correct object type. Handles messages from peers in the session
  ///   as well as the server.
  /// @note Currently in internal development, and not useable
  /// </summary>
  public interface IArmMessageStream : 
    IDisposable
  {
    IDisposable RegisterHandler<T>(Action<T> handler, ArmMessageStream.MessageSource sender);

    bool Unregister<T>();

    void Send(object message, List<IPeer> targets, bool sendToArmServer);

    void Initialize(IMultipeerNetworking networking);

    void RegisterTypeToTag(uint tag, Type type);
  }
}
