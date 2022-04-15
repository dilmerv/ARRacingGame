// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Networking.HLAPI.Object
{
  /// <summary>
  /// Concrete implementation of replicator for sending messages over the network.
  /// </summary>
  /// <typeparam name="TMessage"></typeparam>
  public sealed class MessageStreamReplicator<TMessage>:
    NetworkedDataHandlerBase,
    IMessageStreamReplicator<TMessage>
  {
    // Number of frames peer messages can be queued without being a valid receiver.
    // This gives a buffer for peers to correctly set up authority/networking agreements
    //   on joining
    private const int FRAMES_TO_DROP_MESSAGES = 60;
    private readonly NetworkedDataDescriptor _descriptor;

    private readonly Dictionary<IPeer, Queue<TMessage>> _peerMessageQueues =
      new Dictionary<IPeer, Queue<TMessage>>();
    
    private readonly Dictionary<IPeer, int> _peerMessageDropCounter = new Dictionary<IPeer, int>();

    /// <inheritdoc />
    public event ArdkEventHandler<MessageReceivedEventArgs<TMessage>> MessageReceived;

    public MessageStreamReplicator
    (
      string identifier,
      NetworkedDataDescriptor descriptor,
      INetworkGroup group
    )
    {
      Identifier = identifier;
      _descriptor = descriptor;

      if (group == null)
        return;

      group.RegisterHandler(this);
    }

    protected override object GetDataToSend
    (
      ICollection<IPeer> targetPeers,
      ReplicationMode replicationMode
    )
    {
      if (replicationMode.Transport != TransportType.ReliableOrdered)
        return NothingToWrite;

      var targetPeerList = targetPeers.ToList();
      if (targetPeerList.Count > 1)
        return NothingToWrite;

      var peer = targetPeerList.First();
      
      // If this peer is not a valid receiver, don't immediately drop the message
      // Instead, count up to FRAMES_TO_DROP_MESSAGES before clearing the queued messages
      if (!_descriptor.GetReceivers().Contains(peer))
      {
        int frameCount;
        // This will only succeed if we have tried to send a message to the peer, but it has not
        //   been sent or dropped yet.
        var doesPeerHaveMessage = _peerMessageDropCounter.TryGetValue(peer, out frameCount);
        
        if (!doesPeerHaveMessage)
          return NothingToWrite;

        if (frameCount == FRAMES_TO_DROP_MESSAGES)
        {
          var warningString =
            "MessageStreamReplicator {0} on group {1} attempted to send a message " +
            "to peer {2}, but that peer was never registered as a valid receiver." +
            " Dropping message.";
          ARLog._WarnFormat(warningString, false, Identifier, Group.NetworkId.RawId, peer);
          _peerMessageDropCounter.Remove(peer);
          _peerMessageQueues.Remove(peer);
        }
        else
          _peerMessageDropCounter[peer] = frameCount + 1;

        return NothingToWrite;
      }

      var messages = _peerMessageQueues.GetOrDefault(peer);
      if (messages == null || messages.Count == 0)
        return NothingToWrite;

      var messagesAsArray = messages.ToArray();
      messages.Clear();
      _peerMessageDropCounter.Remove(peer);

      var debugString = "MessageStreamReplicator {0} on group {1} sending message to peer {2}";
      ARLog._DebugFormat(debugString, false, Identifier, Group.NetworkId.RawId, peer);
      return messagesAsArray;
    }

    protected override void HandleReceivedData
    (
      object data,
      IPeer sender,
      ReplicationMode replicationMode
    )
    {
      if (!_descriptor.GetSenders().Contains(sender))
      {
        var warningString = 
          "MessageStreamReplicator {0} on group {1} got a message from an invalid sender {2}";

        ARLog._WarnFormat(warningString, false, Identifier, Group.NetworkId.RawId, sender);
        return;
      }

      var handler = MessageReceived;
      if (handler == null)
        return;
      
      var debugString = "MessageStreamReplicator {0} on group {1} got message from peer {2}";
      ARLog._DebugFormat(debugString, false, Identifier, Group.NetworkId.RawId, sender);

      var array = (TMessage[])data;
      foreach (var message in array)
      {
        var args = new MessageReceivedEventArgs<TMessage>(sender, message);
        handler(args);
      }
    }

    /// <inheritdoc />
    public void SendMessage(TMessage message, IEnumerable<IPeer> targets)
    {
      var self = GetSelfOrNull();

      foreach (var peerInfo in targets)
      {
        if (peerInfo.Equals(self))
        {
          var handler = MessageReceived;
          if (handler != null)
          {
            var args = new MessageReceivedEventArgs<TMessage>(peerInfo, message);
            handler(args);
          }
        }
        else
        {
          var queue = _peerMessageQueues.GetOrInsertNew(peerInfo);
          // Add a new entry for the peer, but don't overwrite a previously existing counter
          // This ensures that we don't prolong the drop buffer by sending messages too often
          _peerMessageDropCounter.GetOrInsertNew(peerInfo);
          queue.Enqueue(message);
        }
      }
    }

    /// <inheritdoc />
    public void SendMessage(TMessage message, params IPeer[] targets)
    {
      SendMessage(message, new HashSet<IPeer>(targets));
    }
  }
}
