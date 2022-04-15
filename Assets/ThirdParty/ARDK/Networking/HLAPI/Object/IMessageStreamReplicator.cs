// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.HLAPI.Object
{
  /// <summary>
  /// Replicator interface for sending messages over the network.
  /// </summary>
  /// <typeparam name="TMessage"></typeparam>
  public interface IMessageStreamReplicator<TMessage>:
    INetworkedDataHandler
  {
    /// <summary>
    /// Called when data is received from a peer (including from the local peer if they are in the set).
    /// </summary>
    event ArdkEventHandler<MessageReceivedEventArgs<TMessage>> MessageReceived;

    /// <summary>
    /// Sends a message to a set of peers.
    /// </summary>
    void SendMessage(TMessage message, IEnumerable<IPeer> targets);

    /// <summary>
    /// Sends a message to a set of peers.
    /// </summary>
    void SendMessage(TMessage message, params IPeer[] targets);
  }
}
