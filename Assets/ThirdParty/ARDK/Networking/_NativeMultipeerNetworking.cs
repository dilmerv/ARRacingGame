// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using AOT;

using Niantic.ARDK.AR;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Networking.Clock;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Networking
{
  //! Handles multipeer networking sessions
  /// <summary>
  /// Provides events for peers joining/leaving the session, receiving data from peers, setup status\n
  /// Provides fields for querying a list of peers, the host of the session, local player\n
  /// Provides methods for joining/leaving a session, sending data to one or more peers
  /// </summary>
  internal sealed class _NativeMultipeerNetworking:
    _ThreadCheckedObject,
    IMultipeerNetworking
  {
    /// <inheritdoc />
    public Guid StageIdentifier { get; private set; }

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    private byte[] _joinedSessionMetadata;

    // Privately handled lookup of peers. Peers are registered on DidAddPeer and removed on
    //   DidRemovePeer
    private readonly Dictionary<Guid, IPeer> _peers = new Dictionary<Guid, IPeer>();
    private readonly ARDKReadOnlyCollection<IPeer> _readOnlyPeers;

    // Keys used in the persistent key-value store prefixed by ! are reserved for the server
    private static readonly char KeyValueKeyReservedPrefix = '!';

    /// <inheritdoc />
    public IReadOnlyCollection<IPeer> OtherPeers
    {
      get
      {
        // Ideally all the inner methods should also be thread checked... but this basic check
        // should avoid really bad cases from happening.
        _CheckThread();

        return _readOnlyPeers;
      }
    }

    /// <inheritdoc />
    public IPeer Self { get; private set; }

    /// <inheritdoc />
    public IPeer Host { get; private set; }

    /// <summary>
    /// Get a copy of the session metadata that this _NativeMultipeerNetworking is currently
    ///   connected to.
    /// @note The output of this property should not be changed, but is returned as an array
    ///   for performance.
    /// @note Will return null if the session is not connected.
    /// </summary>
    public byte[] GetJoinedSessionMetadata()
    {
      _CheckThread();

      if (_joinedSessionMetadata == null)
        return null;

      var metadataCopy = new byte[_joinedSessionMetadata.Length];
      _joinedSessionMetadata.CopyTo(metadataCopy, 0);

      return metadataCopy;
    }

    private ICoordinatedClock _clock;

    public ICoordinatedClock CoordinatedClock
    {
      get
      {
        _CheckThread();

        if (_clock == null)
        {
          ARLog._Debug("Creating a new _NativeCoordinatedClock");
          _clock = new _NativeCoordinatedClock(StageIdentifier);
        }

        return _clock;
      }
    }

    /// <summary>
    /// Set to true if the _NativeMultipeerNetworking is destroyed, or if the underlying native code
    ///   is explicitly destroyed with Destroy. Prevents any currently queued callbacks from
    ///   processing
    /// </summary>
    internal bool IsDestroyed { get; private set; }

    /// <inheritdoc />
    public void Join(byte[] metadata, byte[] token = null, Int64 timestamp = 0)
    {
      if (metadata == null || metadata.Length == 0)
        throw new ArgumentException("Cannot be null or empty.", nameof(metadata));

      _CheckThread();

      if (_nativeHandle == IntPtr.Zero)
        throw new ObjectDisposedException("Multipeer networking was destroyed!", (Exception)null);

      if (IsConnected)
      {
        ARLog._Warn
        (
          metadata.SequenceEqual(_joinedSessionMetadata)
            ? "ARDK: Already joined this session."
            : "ARDK: Already connected to a different session."
        );

        return;
      }

      ARLog._DebugFormat("Attempting to join session: {0}", false, Encoding.UTF8.GetString(metadata));

      if (ServerConfiguration.AuthRequired)
      {
        if (string.IsNullOrEmpty(ServerConfiguration.ApiKey))
        {
          ARLog._Error("Cannot join session without ServerConfiguration.ApiKey set");
        }
        var apiKeyAsBytes = Encoding.UTF8.GetBytes(ServerConfiguration.ApiKey);

        var lengthOfMetadata = Math.Max(metadata.Length, apiKeyAsBytes.Length);
        var sessionMetadataWithAuth = new byte[lengthOfMetadata];

        Array.Copy(metadata, sessionMetadataWithAuth, metadata.Length);

        for (var i = 0; i < Math.Min(apiKeyAsBytes.Length, sessionMetadataWithAuth.Length); i++)
        {
          sessionMetadataWithAuth[i] ^= apiKeyAsBytes[i];
        }

        _joinedSessionMetadata = sessionMetadataWithAuth;

        _NARMultipeerNetworking_Join
        (
          _nativeHandle,
          sessionMetadataWithAuth,
          (ulong)sessionMetadataWithAuth.Length
        );
      }
      else
      {
        _joinedSessionMetadata = metadata;

        _NARMultipeerNetworking_Join
        (
          _nativeHandle,
          metadata,
          (ulong)metadata.Length
        );
      }
    }

    /// <inheritdoc />
    public void Leave()
    {
      _CheckThread();

      if (_nativeHandle == IntPtr.Zero)
        throw new ObjectDisposedException("Multipeer networking was destroyed!", (Exception)null);

      if (!IsConnected)
      {
        ARLog._Warn("ARDK: Cannot leave a multipeer networking session that is not connected.");

        return;
      }

      var metadata = _joinedSessionMetadata;

      ARLog._DebugFormat("Leaving session: {0}", false, Encoding.UTF8.GetString(metadata));

      _NARMultipeerNetworking_Leave(_nativeHandle, metadata, (ulong)metadata.Length);
      _peers.Clear();
      IsConnected = false;
      _joinedSessionMetadata = null;

      var handler = Disconnected;
      if (handler != null)
      {
        var args = new DisconnectedArgs();
        handler(args);
      }
    }

    /// <inheritdoc />
    public void SendDataToPeer
    (
      uint tag,
      byte[] data,
      IPeer peer,
      TransportType transportType,
      bool sendToSelf = false
    )
    {
      if (peer == null)
        throw new ArgumentNullException(nameof(peer));

      // TODO: stackalloc when unsafe?
      SendDataToPeers
      (
        tag,
        data,
        new[] { peer },
        transportType,
        sendToSelf
      );
    }

    /// <inheritdoc />
    public void SendDataToPeers
    (
      uint tag,
      byte[] data,
      IEnumerable<IPeer> peers,
      TransportType transportType,
      bool sendToSelf = false
    )
    {
      if (data == null)
        throw new ArgumentNullException(nameof(data));

      _CheckThread();

      if (_nativeHandle == IntPtr.Zero)
        throw new ObjectDisposedException("Multipeer networking was destroyed!", (Exception)null);

      if (!IsConnected)
      {
        ARLog._Warn
        (
          "ARDK: Cannot send data through a multipeer networking session " +
          "that is not connected."
        );

        return;
      }

      if (data.Length == 0)
      {
        ARLog._Warn("ARDK: Cannot send empty data array.");
        return;
      }

      var isUnreliableMessage =
        transportType == TransportType.UnreliableOrdered ||
        transportType == TransportType.UnreliableUnordered;

      if (isUnreliableMessage)
      {
        if (data.Length > NetworkMessageSizeLimits.MaxUnreliableMessageSize)
        {
          ARLog._ErrorFormat
          (
            "Attempting to send an unreliable message of {0} bytes, over the limit of {1} bytes",
            data.Length,
            NetworkMessageSizeLimits.MaxUnreliableMessageSize
          );

          return;
        }
      }
      else
      {
        if (data.Length > NetworkMessageSizeLimits.MaxReliableMessageSize)
        {
          ARLog._ErrorFormat
          (
            "Attempting to send a reliable message of {0} bytes, over the limit of {1} bytes",
            data.Length,
            NetworkMessageSizeLimits.MaxReliableMessageSize
          );

          return;
        }
      }

      if (peers == null)
        throw new ArgumentNullException(nameof(peers));

      var peerList = peers.ToList();

      ARLog._DebugFormat
      (
        "Sending {0} bytes with tag {1} to {2} peers",
        true,
        data.Length,
        tag,
        peerList.Count
      );

      var peerIdentifiers = new byte[peerList.Count * 16];
      for (var i = 0; i < peerList.Count; i++)
      {
        var peerGUID = peerList[i].Identifier.ToByteArray();

        for (var j = 0; j < 16; j++)
          peerIdentifiers[j + i * 16] = peerGUID[j];
      }

      _NARMultipeerNetworking_SendDataToPeers
      (
        _nativeHandle,
        tag,
        data,
        (ulong)data.Length,
        peerIdentifiers,
        (ulong)peerList.Count,
        (byte)transportType
      );

      if (sendToSelf)
      {
        ARLog._DebugFormat
        (
          "Sending {0} bytes with tag {1} to self",
          true,
          data.Length,
          tag
        );
        var handler = PeerDataReceived;
        if (handler != null)
        {
          var args = new PeerDataReceivedArgs(Self, tag, transportType, data);
          handler(args);
        }
      }
    }

    /// <inheritdoc />
    public void BroadcastData
    (
      uint tag,
      byte[] data,
      TransportType transportType,
      bool sendToSelf = false
    )
    {
      SendDataToPeers(tag, data, new List<IPeer>(), transportType, sendToSelf);
    }

    public void StorePersistentKeyValue(string key, byte[] value)
    {
      if (string.IsNullOrEmpty(key))
        throw new ArgumentException("Cannot store a key-value with a null or empty key");

      if (value == null)
        throw new ArgumentNullException(nameof(value));

      _CheckThread();

      if (_nativeHandle == IntPtr.Zero)
        throw new ObjectDisposedException("Multipeer networking was destroyed!", (Exception)null);

      if (key[0] == KeyValueKeyReservedPrefix)
      {
        ARLog._ErrorFormat
        (
          "The prefix `!` for keys is reserved for the server, not storing for key: {0}",
          key
        );
        return;
      }

      var sizeOfKey = Encoding.UTF8.GetByteCount(key);
      if (sizeOfKey > NetworkMessageSizeLimits.MaxPersistentKeyValueKeySize)
      {
        ARLog._ErrorFormat
        (
          "Attempting to set a persistent key value with key size {0}, over the limit of {1}",
          sizeOfKey,
          NetworkMessageSizeLimits.MaxPersistentKeyValueKeySize
        );

        return;
      }

      if (value.Length > NetworkMessageSizeLimits.MaxPersistentKeyValueValueSize)
      {
        ARLog._ErrorFormat
        (
          "Attempting to set a persistent key value with value size {0}, over the limit of {1}",
          value.Length,
          NetworkMessageSizeLimits.MaxPersistentKeyValueValueSize
        );

        return;
      }

      ARLog._DebugFormat
      (
        "Storing key: {0} with value of length: {1}",
        false,
        key,
        value.Length
      );

      _NARMultipeerNetworking_StorePersistentKeyValue
      (
        _nativeHandle,
        key,
        value,
        (UInt64)value.Length
      );
    }

    public void SendDataToArm(uint tag, byte[] data)
    {
      if (data == null)
        throw new ArgumentNullException(nameof(data));

      _CheckThread();

      if (_nativeHandle == IntPtr.Zero)
        throw new ArgumentException("Multipeer networking was destroyed!");

      if (!IsConnected)
      {
        ARLog._Warn
        (
          "ARDK: Cannot send data through a multipeer networking session " +
          "that is not connected."
        );

        return;
      }

      ARLog._DebugFormat
      (
        "Sending message to Arm with tag: {0} and data of length: {1}",
        true,
        tag,
        data.Length
      );

      _NARMultipeerNetworking_SendDataToARM
      (
        _nativeHandle,
        tag,
        data,
        (ulong)data.Length
      );
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return string.Format("StageID: {0}", StageIdentifier);
    }

    /// <inheritdoc />
    public string ToString(int count)
    {
      return string.Format("StageID: {0}", StageIdentifier.ToString().Substring(0, count));
    }

    static _NativeMultipeerNetworking()
    {
      Platform.Init();
    }

    /// <summary>
    /// Creates a multipeer networking client with a custom server configuration and a custom
    /// StageIdentifier.
    /// </summary>
    internal _NativeMultipeerNetworking
    (
      ServerConfiguration configuration,
      Guid stageIdentifier
    )
    {
      ARLog._DebugFormat("Creating _NativeMultipeerNetworking with stageIdentifier: {0}", false, stageIdentifier);
      _FriendTypeAsserter.AssertCallerIs(typeof(MultipeerNetworkingFactory));

      _readOnlyPeers = _peers.Values.AsArdkReadOnly();

      var arbeEndpoint = configuration.Endpoint;

      StageIdentifier = stageIdentifier;

#pragma warning disable 0162
      // This is required to differentiate between unit testing (does not require the native platform)
      //  and Native in Editor
      if (NativeAccess.Mode == NativeAccess.ModeType.Testing)
        return;
#pragma warning restore 0162

      // Only fill out the ClientMetadata if auth is not required. Otherwise, the server will handle it
      if (!ServerConfiguration.AuthRequired && configuration.ClientMetadata == null)
        configuration.GenerateRandomClientId();

      if (!ServerConfiguration.AuthRequired)
      {
        ARLog._Debug("Attempting to create _NativeMultipeerNetworking without auth");

        _nativeHandle = _NARMultipeerNetworking_Init
        (
          stageIdentifier.ToByteArray(),
          configuration.HeartbeatFrequency,
          arbeEndpoint,
          configuration.ClientMetadata,
          (ulong)configuration.ClientMetadata.LongLength,
          _applicationHandle,
          _ConnectionDidFailWithErrorNative
        );

        ARLog._DebugFormat("Created _NativeMultipeerNetworking with handle: {0}", false, _nativeHandle);
      }
      else
      {
        var apiKeyCopy = ServerConfiguration.ApiKey;
        var authUrlCopy = ServerConfiguration.AuthenticationUrl;

        var isMissingAuthComponents =
          string.IsNullOrEmpty(apiKeyCopy) || string.IsNullOrEmpty(authUrlCopy);

        if (isMissingAuthComponents)
        {
          var authErrorMessage =
            "Attempting to use networking without setting up your API key or Authentication URL. " +
            "Set those up through the ARDKAuthRegistrar, or at runtime on ServerConfiguration " +
            "before using networking!";

          throw new Exception(authErrorMessage);
        }

        ARLog._Debug("Attempting to create _NativeMultipeerNetworking with auth");
        _nativeHandle = _NARMultipeerNetworking_InitEx
        (
          stageIdentifier.ToByteArray(),
          configuration.HeartbeatFrequency,
          arbeEndpoint,
          configuration.ClientMetadata,
          0,
          apiKeyCopy,
          authUrlCopy,
          _applicationHandle,
          _ConnectionDidFailWithErrorNative
        );
        ARLog._DebugFormat("Created _NativeMultipeerNetworking with handle: {0}", false, _nativeHandle);
      }

      // We now subscribe to this callback in the _Init call above
      _connectionDidFailWithErrorInitialized = true;

      // Inform the GC that this class is holding a large native object, so it gets cleaned up fast
      // TODO(awang): Make an IReleasable interface that handles this for all native-related classes
      GC.AddMemoryPressure(GCPressure);

      _nativeARMSenderHandle = _NARMultipeerNetworking_GetARMMessageSender(_nativeHandle);

      SubscribeToInternalCallbacks();
    }

    /// Constructor used by the _NativeARNetworking class since the networking is constructed in native
    internal _NativeMultipeerNetworking(IntPtr nativeMultipeerNetworking)
    {
      _nativeHandle = nativeMultipeerNetworking;
      _nativeARMSenderHandle = _NARMultipeerNetworking_GetARMMessageSender(_nativeHandle);

      Guid stageIdentifier;
      _NARMultipeerNetworking_GetStageIdentifier(_nativeHandle, out stageIdentifier);
      ARLog._Debug("Constructed a _NativeMultipeerNetworking belonging to a _NativeARNetworking");

      // Inform the GC that this class is holding a large native object, so it gets cleaned up fast
      // TODO(awang): Make an IReleasable interface that handles this for all native-related classes
      GC.AddMemoryPressure(GCPressure);

      StageIdentifier = stageIdentifier;

      // We always subscribe to this callback in the _Init call in the other constructor.
      // So this native object should also have the callback already set.
      _connectionDidFailWithErrorInitialized = true;
      SubscribeToInternalCallbacks();
    }

    ~_NativeMultipeerNetworking()
    {
      Dispose(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
      _CheckThread();

      ARLog._Debug("Disposing _NativeMultipeerNetworking");
      GC.SuppressFinalize(this);
      Dispose(true);
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        var handler = Deinitialized;
        if (handler != null)
        {
          var args = new DeinitializedArgs();
          handler(args);
        }

        _connectionFailed.Dispose();
        _clock = null;
      }

      if (_nativeHandle != IntPtr.Zero)
      {
        _NARMultipeerNetworking_ReleaseARMMessageSender(_nativeARMSenderHandle);
        _nativeARMSenderHandle = IntPtr.Zero;
        _NARMultipeerNetworking_Release(_nativeHandle);
        _nativeHandle = IntPtr.Zero;

        ARLog._Debug("Successfully released native multipeer object");
      }

      _cachedHandle.Free();

      IsConnected = false;
      IsDestroyed = true;
    }

    private ArdkEventHandler<ConnectedArgs> _connected;

    /// <inheritdoc />
    public event ArdkEventHandler<ConnectedArgs> Connected
    {
      add
      {
        _CheckThread();

        _connected += value;

        if (IsConnected)
        {
          var args = new ConnectedArgs(Self, Host);
          value(args);
        }
      }
      remove { _connected -= value; }
    }

    private ArdkEventHandler<PersistentKeyValueUpdatedArgs> _persistentKeyValueUpdated;

    /// <inheritdoc />
    public event ArdkEventHandler<PersistentKeyValueUpdatedArgs> PersistentKeyValueUpdated
    {
      add
      {
        _CheckThread();

        SubscribeToDidUpdatePersistentKeyValue();
        _persistentKeyValueUpdated += value;
      }
      remove { _persistentKeyValueUpdated -= value; }
    }

    private readonly _CatchUpEvent<ConnectionFailedArgs> _connectionFailed =
      new _CatchUpEvent<ConnectionFailedArgs>();

    /// <inheritdoc />
    public event ArdkEventHandler<ConnectionFailedArgs> ConnectionFailed
    {
      add { _connectionFailed.Register(value); }
      remove { _connectionFailed.Unregister(value); }
    }

    /// <inheritdoc />
    public event ArdkEventHandler<DisconnectedArgs> Disconnected;

    /// <inheritdoc />
    public event ArdkEventHandler<PeerDataReceivedArgs> PeerDataReceived;

    /// <inheritdoc />
    public event ArdkEventHandler<PeerAddedArgs> PeerAdded;

    /// <inheritdoc />
    public event ArdkEventHandler<PeerRemovedArgs> PeerRemoved;

    /// <inheritdoc />
    public event ArdkEventHandler<DeinitializedArgs> Deinitialized;

    /// <inheritdoc />
    public event ArdkEventHandler<DataReceivedFromArmArgs> DataReceivedFromArm = (args) => {};

    /// <inheritdoc />
    public event ArdkEventHandler<SessionStatusReceivedFromArmArgs> SessionStatusReceivedFromArm
    {
      add { _sessionStatusReceivedFromArm += value; }
      remove { _sessionStatusReceivedFromArm -= value; }
    }

    /// <inheritdoc />
    public event ArdkEventHandler<SessionResultReceivedFromArmArgs> SessionResultReceivedFromArm
    {
      add { _sessionResultReceivedFromArm += value; }
      remove { _sessionResultReceivedFromArm -= value; }
    }

    // Below here are private fields and methods to handle native code and callbacks

    // The pointer to the C++ object handling functionality at the native level
    private IntPtr _nativeHandle;

    // The pointer to the C++ object for sending ARM messages.
    private IntPtr _nativeARMSenderHandle;

    internal IntPtr GetNativeHandle()
    {
      return _nativeHandle;
    }

    private IntPtr _cachedHandleIntPtr = IntPtr.Zero;
    private SafeGCHandle<_NativeMultipeerNetworking> _cachedHandle;

    // Approx memory size of native object
    // Magic number for 10MB, from profiling an iPhone 8
    private const long GCPressure = 10L * 1024L * 1024L;

    // Used to round-trip a pointer through c++,
    // so that we can keep our this pointer even in c# functions
    // marshaled and called by native code
    private IntPtr _applicationHandle
    {
      get
      {
        if (_cachedHandleIntPtr != IntPtr.Zero)
          return _cachedHandleIntPtr;

        lock (this)
        {
          if (_cachedHandleIntPtr != IntPtr.Zero)
            return _cachedHandleIntPtr;

          // https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.gchandle.tointptr.aspx
          _cachedHandle = SafeGCHandle.Alloc(this);
          _cachedHandleIntPtr = _cachedHandle.ToIntPtr();
        }

        return _cachedHandleIntPtr;
      }
    }

    RuntimeEnvironment IMultipeerNetworking.RuntimeEnvironment
    {
      get { return RuntimeEnvironment.LiveDevice; }
    }

#region CallbackImplementation
    private bool _didConnectInitialized;
    private bool _connectionDidFailWithErrorInitialized;
    private bool _didReceiveDataFromPeerInitialized;
    private bool _didAddPeerInitialized;
    private bool _didRemovePeerInitialized;
    private bool _didUpdatePersistentKeyValueInitialized;
    private bool _armCallbacksInitialized;

#region InternalSubscriptions
    /// <summary>
    /// These callbacks are subscribed to at initialization as they set the data that will be provided
    /// by accessors (Self, Host, Peers, etc)
    /// </summary>
    private void SubscribeToInternalCallbacks()
    {
      SubscribeToDidConnect();
      SubscribeToDidAddPeer();
      SubscribeToDidRemovePeer();
      SubscribeToConnectionDidFailWithError();
      SubscribeToDidReceiveDataFromPeer();
      SubscribeToArmCallbacks();
    }

    internal void SubscribeToDidConnect()
    {
      if (_didConnectInitialized)
        return;

      lock (this)
      {
        if (_didConnectInitialized)
          return;

        _NARMultipeerNetworking_Set_didConnectCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didConnectNative
        );

        _didConnectInitialized = true;
      }
    }

    internal void SubscribeToConnectionDidFailWithError()
    {
      if (_connectionDidFailWithErrorInitialized)
        return;

      lock (this)
      {
        if (_connectionDidFailWithErrorInitialized)
          return;

        _NARMultipeerNetworking_Set_connectionDidFailWithErrorCallback
        (
          _applicationHandle,
          _nativeHandle,
          _ConnectionDidFailWithErrorNative
        );

        _connectionDidFailWithErrorInitialized = true;
      }
    }

    internal void SubscribeToDidReceiveDataFromPeer()
    {
      if (_didReceiveDataFromPeerInitialized)
        return;

      lock (this)
      {
        if (_didReceiveDataFromPeerInitialized)
          return;

        _NARMultipeerNetworking_Set_didReceiveDataFromPeerCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didReceiveDataFromPeerNative
        );

        _didReceiveDataFromPeerInitialized = true;
      }
    }

    internal void SubscribeToDidAddPeer()
    {
      if (_didAddPeerInitialized)
        return;

      lock (this)
      {
        if (_didAddPeerInitialized)
          return;

        _NARMultipeerNetworking_Set_didAddPeerCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didAddPeerNative
        );

        _didAddPeerInitialized = true;
      }
    }

    internal void SubscribeToDidRemovePeer()
    {
      if (_didRemovePeerInitialized)
        return;

      lock (this)
      {
        if (_didRemovePeerInitialized)
          return;

        _NARMultipeerNetworking_Set_didRemovePeerCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didRemovePeerNative
        );

        _didRemovePeerInitialized = true;
      }
    }

    internal void SubscribeToDidUpdatePersistentKeyValue()
    {
      if (_didUpdatePersistentKeyValueInitialized)
        return;

      lock (this)
      {
        if (_didUpdatePersistentKeyValueInitialized)
          return;

        _NARMultipeerNetworking_Set_didUpdatePersistentKeyValueCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didUpdatePersistentKeyValueNative
        );

        _didUpdatePersistentKeyValueInitialized = true;
      }
    }

    internal void SubscribeToArmCallbacks()
    {
      if (_armCallbacksInitialized)
        return;

      lock (this)
      {
        if (_armCallbacksInitialized)
          return;

        _NARMultipeerNetworking_Set_didReceiveDataFromARMCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didReceiveDataFromArmNative
        );

        _NARMultipeerNetworking_Set_didReceiveARMSessionStatusCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didReceiveSessionStatusFromArmNative
        );

        _NARMultipeerNetworking_Set_didReceiveARMSessionResultCallback
        (
          _applicationHandle,
          _nativeHandle,
          _didReceiveSessionResultFromArmNative
        );

        _armCallbacksInitialized = true;
      }
    }
#endregion
#endregion

#region InternalCallbacks
    private event ArdkEventHandler<SessionStatusReceivedFromArmArgs> _sessionStatusReceivedFromArm =
      (args) => {};

    private event ArdkEventHandler<SessionResultReceivedFromArmArgs> _sessionResultReceivedFromArm =
      (args) => {};

    [MonoPInvokeCallback(typeof(_NARMultipeerNetworking_Did_Connect_Callback))]
    private static void _didConnectNative
    (
      IntPtr context,
      IntPtr rawSelf,
      IntPtr rawHost,
      UInt32 isHost
    )
    {
      ARLog._Debug("Invoked _didConnectNative");
      var session = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);

      if (session == null || session.IsDestroyed)
      {
        ARLog._Warn("_didConnectNative called after C# object was already destroyed.");

        _Peer._ReleasePeer(rawSelf);
        _Peer._ReleasePeer(rawHost);

        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (session.IsDestroyed)
          {
            ARLog._Warn("Queued _didConnectNative called after C# object was already destroyed.");

            _Peer._ReleasePeer(rawSelf);
            _Peer._ReleasePeer(rawHost);

            return;
          }

          var self = _Peer.FromNativeHandle(rawSelf);
          var host = _Peer.FromNativeHandle(rawHost);

          session.IsConnected = true;
          session.Self = self;
          session.Host = host;

          var handler = session._connected;
          if (handler != null)
          {
            ARLog._Debug("Surfacing Connected event");
            var args = new ConnectedArgs(self, host);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NARMultipeerNetworking_Connection_Did_Fail_With_Error_Callback))]
    private static void _ConnectionDidFailWithErrorNative(IntPtr context, UInt32 errorCode)
    {
      ARLog._Debug("Invoked _ConnectionDidFailWithErrorNative");
      var instance = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);
      if (instance == null || instance.IsDestroyed)
        return;

      ARLog._WarnFormat("Failed with err code {0}",false, errorCode);

      var args = new ConnectionFailedArgs(errorCode);
      instance._connectionFailed.InvokeUsingCallbackQueue(args);
    }

    [MonoPInvokeCallback(typeof(_NARMultipeerNetworking_Did_Receive_Data_From_Peer_Callback))]
    private static void _didReceiveDataFromPeerNative
    (
      IntPtr context,
      UInt32 tag,
      IntPtr rawData,
      UInt64 rawDataSize,
      IntPtr rawPeer,
      byte transportType
    )
    {
      ARLog._Debug("Invoked _didReceiveDataFromPeerNative", true);
      var instance = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);

      if (instance == null || instance.IsDestroyed)
      {
        ARLog._Warn("Queued _didReceiveDataFromPeerNative called after C# instance was destroyed.");
        _Peer._ReleasePeer(rawPeer);
        return;
      }

      var data = new byte[rawDataSize];
      Marshal.Copy(rawData, data, 0, (int)rawDataSize);

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (instance.IsDestroyed)
          {
            var msg = "Queued _didReceiveDataFromPeerNative called after C# instance was destroyed.";
            ARLog._Warn(msg);

            _Peer._ReleasePeer(rawPeer);
            return;
          }

          var peer = _Peer.FromNativeHandle(rawPeer);

          var dataInfo =
            new DataInfo
            {
              tag = tag, peer = peer, transportType = (TransportType)transportType
            };

          var handler = instance.PeerDataReceived;
          if (handler != null)
          {
            ARLog._Debug("Surfacing PeerDataReceived event");
            var args = new PeerDataReceivedArgs(peer, tag, dataInfo.transportType, data);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NARMultipeerNetworking_Did_Add_Or_Remove_Peer_Callback))]
    private static void _didAddPeerNative(IntPtr context, IntPtr rawPeer)
    {
      ARLog._Debug("Invoked _didAddPeerNative");

      var instance = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);
      if (instance == null || instance.IsDestroyed)
      {
        ARLog._Warn("_didAddPeerNative invoked after C# instance was destroyed.");
        _Peer._ReleasePeer(rawPeer);
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (instance.IsDestroyed)
          {
            ARLog._Warn("Queued _didAddPeerNative invoked after C# instance was destroyed.");
            _Peer._ReleasePeer(rawPeer);
            return;
          }

          var peer = _Peer.FromNativeHandle(rawPeer);
          instance._peers[peer.Identifier] = peer;

          var handler = instance.PeerAdded;
          if (handler != null)
          {
            ARLog._Debug("Surfacing PeerAdded event");
            var args = new PeerAddedArgs(peer);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NARMultipeerNetworking_Did_Add_Or_Remove_Peer_Callback))]
    private static void _didRemovePeerNative(IntPtr context, IntPtr peerIDPtr)
    {
      ARLog._Debug("Invoked _didRemovePeerNative");

      var instance = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);
      if (instance == null || instance.IsDestroyed)
      {
        ARLog._Warn("_didRemovePeerNative invoked after C# instance was destroyed.");
        _Peer._ReleasePeer(peerIDPtr);
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (instance.IsDestroyed)
          {
            ARLog._Warn("Queued _didRemovePeerNative invoked after C# instance was destroyed.");
            _Peer._ReleasePeer(peerIDPtr);
            return;
          }

          var peer = _Peer.FromNativeHandle(peerIDPtr);

          // If a peer-left message containing Self.Identifier has been received, you have been dropped
          //   from the session. In that case, clear the peers list.
          if (peer.Identifier == instance.Self.Identifier)
            instance._peers.Clear();
          else
          {
            // It is not guaranteed that the peerIDPtr surfaced is equivalent to the cached value for
            //   that peer, iterate through all of the cached peers and remove the peer by identifier
            var query = instance._peers.Where(kvp => kvp.Value.Identifier == peer.Identifier);

            foreach (var cachedPeer in query.ToList())
              instance._peers.Remove(cachedPeer.Key);
          }

          var handler = instance.PeerRemoved;
          if (handler != null)
          {
            ARLog._Debug("Surfacing PeerRemoved event");
            var args = new PeerRemovedArgs(peer);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NARMultipeerNetworking_Did_Update_Persistent_KeyValue_Callback))]
    private static void _didUpdatePersistentKeyValueNative
    (
      IntPtr context,
      string key,
      IntPtr value,
      UInt64 valueSize
    )
    {
      ARLog._Debug("Invoked _didUpdatePersistentKeyValueNative");
      var session = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);

      if (session == null || session.IsDestroyed)
      {
        ARLog._Warn("_didUpdatePersistentKeyValueNative invoked after C# object was destroyed.");
        return;
      }

      var data = new byte[valueSize];
      Marshal.Copy(value, data, 0, (int)valueSize);

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (session.IsDestroyed)
          {
            ARLog._Warn("_didUpdatePersistentKeyValueNative invoked after C# object was destroyed.");
          }

          var handler = session._persistentKeyValueUpdated;
          if (handler != null)
          {
            ARLog._Debug("Surfacing PersistentKeyValueUpdated event");
            var args = new PersistentKeyValueUpdatedArgs(key, data);
            handler(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_NARMultipeerNetworking_Did_Receive_Data_From_ARM_Callback))]
    private static void _didReceiveDataFromArmNative
    (
      IntPtr context,
      UInt32 tag,
      IntPtr rawData,
      UInt64 rawDataSize
    )
    {
      ARLog._Debug("Invoked _didReceiveDataFromArmNative");
      var session = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);

      if (session == null || session.IsDestroyed)
      {
        ARLog._Warn("_didReceiveDataFromArmNative invoked after C# object was destroyed.");
        return;
      }

      var data = new byte[rawDataSize];
      Marshal.Copy(rawData, data, 0, (int)rawDataSize);

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (session.IsDestroyed)
          {
            ARLog._Warn("_didReceiveDataFromArmNative invoked after C# object was destroyed.");
          }

          ARLog._Debug("Surfacing DataReceivedFromArm event");
          var args = new DataReceivedFromArmArgs(tag, data);
          session.DataReceivedFromArm(args);
        }
      );
    }

    [MonoPInvokeCallback
      (typeof(_NARMultipeerNetworking_Did_Receive_Session_Status_From_ARM_Callback))]
    private static void _didReceiveSessionStatusFromArmNative
    (
      IntPtr context,
      UInt32 status
    )
    {
      ARLog._Debug("Invoked _didReceiveSessionStatusFromArmNative");
      var session = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);

      if (session == null || session.IsDestroyed)
      {
        ARLog._Warn("_didReceiveSessionStatusFromArmNative invoked after C# object was destroyed.");
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (session.IsDestroyed)
          {
            ARLog._Warn("_didReceiveSessionStatusFromArmNative invoked after C# object was destroyed.");
          }

          ARLog._Debug("Surfacing SessionStatusReceivedFromArmArgs event");
          var args = new SessionStatusReceivedFromArmArgs(status);
          session._sessionStatusReceivedFromArm(args);
        }
      );
    }

    [MonoPInvokeCallback
      (typeof(_NARMultipeerNetworking_Did_Receive_Session_Result_From_ARM_Callback))]
    private static void _didReceiveSessionResultFromArmNative
    (
      IntPtr context,
      UInt32 outcome,
      IntPtr rawDetails,
      UInt64 rawDetailsSize
    )
    {
      ARLog._Debug("Invoked _didReceiveSessionResultFromArmNative");
      var session = SafeGCHandle.TryGetInstance<_NativeMultipeerNetworking>(context);

      if (session == null || session.IsDestroyed)
      {
        ARLog._Warn("_didReceiveSessionStatusFromArmNative invoked after C# object was destroyed.");
        return;
      }

      byte[] data;

      if (rawDetailsSize == 0 || rawDetails == IntPtr.Zero)
      {
        data = EmptyArray<byte>.Instance;
      }
      else
      {
        data = new byte[rawDetailsSize];
        Marshal.Copy(rawDetails, data, 0, (int)rawDetailsSize);
      }


      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (session.IsDestroyed)
          {
            ARLog._Warn("_didReceiveSessionStatusFromArmNative invoked after C# object was destroyed.");
          }

          ARLog._Debug("Surfacing SessionResultReceivedFromArmArgs event");
          var args = new SessionResultReceivedFromArmArgs(outcome, data);
          session._sessionResultReceivedFromArm(args);
        }
      );
    }
#endregion

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARMultipeerNetworking_Init
    (
      byte[] stageIdentifier,
      int heartbeatFrequencyMillis,
      string tcpServerAddress,
      byte[] clientMetadata,
      UInt64 clientMetadataSize,
      IntPtr context,
      _NARMultipeerNetworking_Connection_Did_Fail_With_Error_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARMultipeerNetworking_InitEx
    (
      byte[] stageIdentifier,
      int heartbeatFrequencyMillis,
      string tcpServerAddress,
      byte[] clientMetadata,
      UInt64 clientMetadataSize,
      string apiKey,
      string authenticationUrl,
      IntPtr context,
      _NARMultipeerNetworking_Connection_Did_Fail_With_Error_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_GetStageIdentifier
    (
      IntPtr nativeHandle,
      out Guid outIdentifier
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Join
    (
      IntPtr nativeHandle,
      byte[] sessionMetadata,
      UInt64 sessionMetadataSize
    );

    // TODO: Why does leave take the metadata again?
    // Can we join multiple sessions? Why can't it just leave the one it's in?
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Leave
    (
      IntPtr nativeHandle,
      byte[] sessionMetadata,
      UInt64 sessionMetadataSize
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_SendDataToPeers
    (
      IntPtr nativeHandle,
      UInt32 tag,
      byte[] data,
      UInt64 dataSize,
      byte[] peerIdentifiers,
      UInt64 peerIdentifiersSize,
      byte transportType
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_StorePersistentKeyValue
    (
      IntPtr nativeHandle,
      string key,
      byte[] data,
      UInt64 dataSize
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARMultipeerNetworking_GetARMMessageSender(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_ReleaseARMMessageSender
    (
      IntPtr armSenderNativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_SendDataToARM
    (
      IntPtr nativeHandle,
      UInt32 tag,
      byte[] data,
      UInt64 dataSize
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didConnectCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Connect_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_connectionDidFailWithErrorCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Connection_Did_Fail_With_Error_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didReceiveDataFromPeerCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Receive_Data_From_Peer_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didAddPeerCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Add_Or_Remove_Peer_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didRemovePeerCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Add_Or_Remove_Peer_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didUpdatePersistentKeyValueCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Update_Persistent_KeyValue_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didReceiveDataFromARMCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Receive_Data_From_ARM_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didReceiveARMSessionStatusCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Receive_Session_Status_From_ARM_Callback cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMultipeerNetworking_Set_didReceiveARMSessionResultCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARMultipeerNetworking_Did_Receive_Session_Result_From_ARM_Callback cb
    );

    private delegate void _NARMultipeerNetworking_Did_Connect_Callback
    (
      IntPtr context,
      IntPtr peer,
      IntPtr host,
      UInt32 isHost
    );

    private delegate void _NARMultipeerNetworking_Connection_Did_Fail_With_Error_Callback
    (
      IntPtr context,
      UInt32 errCode
    );

    private delegate void _NARMultipeerNetworking_Did_Receive_Data_From_Peer_Callback
    (
      IntPtr context,
      UInt32 tag,
      IntPtr rawData,
      UInt64 rawDataSize,
      IntPtr peerInfo,
      byte transportType
    );

    private delegate void _NARMultipeerNetworking_Did_Add_Or_Remove_Peer_Callback
    (
      IntPtr context,
      IntPtr peerIDPtr
    );

    private delegate void _NARMultipeerNetworking_Did_Update_Persistent_KeyValue_Callback
    (
      IntPtr context,
      string key,
      IntPtr value,
      UInt64 valueSize
    );

    private delegate void _NARMultipeerNetworking_Did_Receive_Data_From_ARM_Callback
    (
      IntPtr context,
      UInt32 tag,
      IntPtr rawData,
      UInt64 rawDataSize
    );

    private delegate void _NARMultipeerNetworking_Did_Receive_Session_Status_From_ARM_Callback
    (
      IntPtr context,
      UInt32 status
    );

    private delegate void _NARMultipeerNetworking_Did_Receive_Session_Result_From_ARM_Callback
    (
      IntPtr context,
      UInt32 outcome,
      IntPtr rawDetails,
      UInt64 rawDetailsSize
    );
  }
}
