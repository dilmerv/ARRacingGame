// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

using UnityEngine;

namespace Niantic.ARDK.Networking.ARSim 
{
  public sealed class ArmMessageStream : IArmMessageStream
  {
    private readonly object _executorsLock = new object();
    private readonly Dictionary<Type, _IMessageExecutor> _executors =
      new Dictionary<Type, _IMessageExecutor>(_ReferenceComparer<Type>.Instance);

    private readonly object _typeTagDictLock = new object();
    private readonly Dictionary<uint, Type> _tagToTypeDict = new Dictionary<uint, Type>();
    private readonly Dictionary<Type, uint> _typeToTagDict = new Dictionary<Type, uint>();
    private readonly MemoryStream _cachedMemoryStream = new MemoryStream(100);
    private IMultipeerNetworking _networking;

    [Flags]
    public enum MessageSource : ushort
    {
      Unknown = 0,
      Server = 1,
      Peer = 2,
    }

    public ArmMessageStream(IMultipeerNetworking networking) 
    {
      Initialize(networking);
    }

    public void Initialize(IMultipeerNetworking networking)
    {
      if(_networking != null)
        return;
      
      _networking = networking;
      networking.PeerDataReceived += _MessageReceived;
      networking.DataReceivedFromArm += _MessageReceivedFromArm;
    }

    public void RegisterTypeToTag(uint tag, Type type) 
    {
      lock (_typeTagDictLock)
      {
        if (_tagToTypeDict.ContainsKey(tag))
          throw new Exception("Already registered tag: " + tag);

        if (_typeToTagDict.ContainsKey(type))
          throw new Exception("Already registered type: " + type);

        _tagToTypeDict[tag] = type;
        _typeToTagDict[type] = tag;
      }
    }

    // Returns an object that, when disposed, unregisters the actual registration.
    public IDisposable RegisterHandler<T>(Action<T> messageExecutor, MessageSource sender)
    {
      if (!typeof(T).IsSealed)
        throw new InvalidOperationException(typeof(T).FullName + " must be a sealed type.");

      if (messageExecutor == null)
        throw new ArgumentNullException(nameof(messageExecutor));

      lock (_executorsLock)
      {
        if (_executors.ContainsKey(typeof(T)))
        {
          string message =
            "There's another message executor already registered for:" + typeof(T).FullName;

          throw new ArgumentException(message, nameof(messageExecutor));
        }

        var messageExecutorHelper = new _MessageExecutor<T>(messageExecutor, sender);
        _executors.Add(typeof(T), messageExecutorHelper);
        return new _Disposer(typeof(T), messageExecutorHelper, _executorsLock, _executors);
      }
    }

    // Unregisters any handler for type T. Usually it is preferred to store the IDisposable
    // object returned from Register and invoke it instead (in that case you only unregister
    // the handler if it is your handler).
    // Returns whether there was a registration for type T (which was unregistered) or not.
    public bool Unregister<T>()
    {
      lock (_executorsLock)
        return _executors.Remove(typeof(T));
    }

    public void Send(object message, List<IPeer> targets, bool sendToArmServer)
    {
      if (message == null)
        throw new ArgumentNullException(nameof(message));

      var type = message.GetType();
      if (!type.IsSealed)
      {
        string errorMessage =
          "message must be from a sealed type, but type " + type.FullName + " is not sealed.";

        throw new ArgumentException(errorMessage, nameof(message));
      }

      uint tag;
      
      lock (_typeTagDictLock)
      {
        if (!_typeToTagDict.ContainsKey(type))
          throw new Exception("Have not registered a tag for type: " + type);

        tag = _typeToTagDict[type];
      }

      var serializer = GlobalSerializer.GetItemSerializerOrThrow(type);

      _cachedMemoryStream.Position = 0;
      _cachedMemoryStream.SetLength(0);

      using (var binarySerializer = new BinarySerializer(_cachedMemoryStream)) 
        serializer.Serialize(binarySerializer, message);

      var data = _cachedMemoryStream.ToArray();
      if (targets != null && targets.Count > 0)
      {
        _networking.SendDataToPeers
        (
          tag,
          data,
          targets,
          TransportType.UnreliableUnordered
        );
      }

      if (sendToArmServer)
        _networking.SendDataToArm(tag, data);
    }  
    
    public void Dispose()
    {
      if (_networking != null)
      {
        _networking.PeerDataReceived -= _MessageReceived;
        _networking.DataReceivedFromArm -= _MessageReceivedFromArm;
      }
    }
    
    // Private implementation details
    private void _MessageReceived(PeerDataReceivedArgs args)
    {
      Type type;
      lock (_typeTagDictLock)
      {
        if (!_tagToTypeDict.ContainsKey(args.Tag))
          return;

        type = _tagToTypeDict[args.Tag];
      }

      var deserializer = GlobalSerializer.GetItemSerializerOrThrow(type);

      object message;
      using (var binaryDeserializer = new BinaryDeserializer(args.CreateDataReader())) 
        message = deserializer.Deserialize(binaryDeserializer);

      if (message == null)
        throw new ArgumentException("args had a null message. That shouldn't happen.", nameof(args));

      _InvokeExecutor(message, MessageSource.Peer);
    }
    
    // Private implementation details
    private void _MessageReceivedFromArm(DataReceivedFromArmArgs args)
    {
      Type type;
      lock (_typeTagDictLock)
      {
        if (!_tagToTypeDict.ContainsKey(args.Tag))
          return;

        type = _tagToTypeDict[args.Tag];
      }

      var deserializer = GlobalSerializer.GetItemSerializerOrThrow(type);

      object message;
      using (var binaryDeserializer = new BinaryDeserializer(args.CreateDataReader())) 
        message = deserializer.Deserialize(binaryDeserializer);

      if (message == null)
        throw new ArgumentException("args had a null message. That shouldn't happen.", nameof(args));

      _InvokeExecutor(message, MessageSource.Server);
    }

    private void _InvokeExecutor(object message, MessageSource sender)
    {
      var type = message.GetType();

      _IMessageExecutor executor;
      lock (_executorsLock)
        _executors.TryGetValue(type, out executor);

      if (executor != null)
      {
        if((executor.Source & sender) != MessageSource.Unknown)
          executor.Execute(message);
        else
        {
          var warning =
            "Received a message of type: {0} from source: {1}, which is not a valid sender";
          Debug.LogWarningFormat(warning, type, sender);
        }
      }
      else
        Debug.LogWarning("No executor found for message of type: " + type.FullName + ".");
    }
    
    // To avoid using a slow DynamicInvoke on an Action<T>, we use this interface + helper class.
    private interface _IMessageExecutor
    {
      void Execute(object message);
      MessageSource Source { get; }
    }
    private sealed class _MessageExecutor<T>:
      _IMessageExecutor
    {
      private readonly Action<T> _action;

      private _MessageExecutor()
      {
      }

      internal _MessageExecutor(Action<T> action, MessageSource source) 
        : this()
      {
        _action = action;
        Source = source;
      }

      public void Execute(object message)
      {
        T typedMessage = (T)message;
        _action(typedMessage);
      }

      public MessageSource Source
      {
        get;
        private set;
      }
    }

    private sealed class _Disposer:
      IDisposable
    {
      private readonly Type _type;
      private readonly _IMessageExecutor _executor;
      private readonly object _sharedExecutorLock;
      private readonly Dictionary<Type, _IMessageExecutor> _sharedExecutors;

      internal _Disposer
      (
        Type type,
        _IMessageExecutor executor,
        object sharedExecutorLock,
        Dictionary<Type, _IMessageExecutor> sharedExecutors
      )
      {
        _type = type;
        _executor = executor;
        _sharedExecutorLock = sharedExecutorLock;
        _sharedExecutors = sharedExecutors;
      }

      // Only removes the existing registration if it is the same that was registered.
      public void Dispose()
      {
        lock (_sharedExecutorLock)
        {
          _IMessageExecutor existingExecutor;
          _sharedExecutors.TryGetValue(_type, out existingExecutor);

          if (_executor == existingExecutor)
            _sharedExecutors.Remove(_type);
        }
      }
    }
  }
}
