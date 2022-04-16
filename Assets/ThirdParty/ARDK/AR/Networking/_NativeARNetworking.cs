// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.AR.Networking.NetworkAnchors;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Marker;

using UnityEngine;

using AOT;

namespace Niantic.ARDK.AR.Networking
{
  // Handles AR Networking sessions through native code.
  // Contains an internal _NativeMultipeerNetworking instance to handle multipeer networking calls.
  //
  // Beyond the typical multipeer networking events, also contains events for receiving pose and
  // state data from peers
  internal sealed class _NativeARNetworking:
    _ThreadCheckedObject,
    IARNetworking
  {
    public IMultipeerNetworking Networking
    {
      get
      {
        return _networking;
      }
    }

    private readonly IMultipeerNetworking _networking;

    public IARSession ARSession
    {
      get
      {
        return _arSession;
      }
    }

    private readonly IARSession _arSession;

    public IReadOnlyDictionary<IPeer, PeerState> LatestPeerStates
    {
      get
      {
        return _readOnlyLatestPeerStates;
      }
    }

    // All accesses of this dictionary must come from the main thread.
    private readonly Dictionary<IPeer, PeerState> _latestPeerStates =
      new Dictionary<IPeer, PeerState>();

    private _ReadOnlyDictionary<IPeer, PeerState> _readOnlyLatestPeerStates;

    private readonly _NetworkAnchorManager _networkAnchorManager = new _NetworkAnchorManager();

    public IReadOnlyDictionary<IPeer, Matrix4x4> LatestPeerPoses
    {
      get
      {
        return _networkAnchorManager.LatestPeerPoses;
      }
    }

    /// <inheritdoc />
    public PeerState LocalPeerState { get; private set; }

    // Set to true if the _NativeARNetworking is destroyed, or if the underlying native code
    // is explicitly destroyed with Destroy. Prevents any currently queued callbacks from
    // processing
    internal bool IsDestroyed { get; private set; }

    private MarkerScanCoordinator _scanCoordinator;

    // Creates an AR multipeer networking client with a custom server configuration.
    internal _NativeARNetworking(IARSession arSession,ServerConfiguration configuration)
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(ARNetworkingFactory));

      ARLog._Debug("Creating _NativeARNetworking with a server configuration");
      // Create networking
      // ServerConfiguration validation is done in the _NativeMultipeerNetworking constructor
      var networking =
        MultipeerNetworkingFactory.Create(configuration, arSession.StageIdentifier);

      if (!_ValidateComponents(arSession, networking))
      {
        networking.Dispose();
        return;
      }

      _arSession = arSession;
      _networking = networking;

      FinishInitialization();
    }

    /// Creates an AR multipeer networking client with an existing multipeer networking instance.
    internal _NativeARNetworking(IARSession arSession, IMultipeerNetworking networking)
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(ARNetworkingFactory));

      ARLog._Debug("Creating _NativeARNetworking with an existing multipeer networking");

      if (!_ValidateComponents(arSession, networking))
        return;

      _arSession = arSession;
      _networking = networking;

      FinishInitialization();
    }

    private bool _ValidateComponents(IARSession arSession, IMultipeerNetworking networking)
    {
      if (string.IsNullOrEmpty(ArdkGlobalConfig.GetDbowUrl()))
      {
        ARLog._Debug("DBOW URL was not set. The default URL will be used.");
        ArdkGlobalConfig.SetDbowUrl(ArdkGlobalConfig._DBOW_URL);
      }

      if (arSession.StageIdentifier != networking.StageIdentifier)
      {
        var msg =
          "Failed to create _NativeARNetworking because the ARSession and MultipeerNetworking" +
          " must have the same StageIdentifier.";
        ARLog._Error(msg);
        return false;
      }

      ARLog._Debug("_NativeARNetworking components validated");
      return true;
    }

    private void FinishInitialization()
    {
      _arSession.Ran += HandleConfigurationChange;

      InitializePeerStateTracking();

      ARLog._Debug("Finished creating _NativeARNetworking");
    }

    ~_NativeARNetworking()
    {
      ARLog._Error("_NativeARNetworking should be destroyed by an explicit call to Dispose().");
      Dispose(false);
    }

    private void HandleConfigurationChange(ARSessionRanArgs args)
    {
      _CheckThread();

      if (_nativeHandle == IntPtr.Zero)
      {
        ConstructNativeHandle(_arSession.Configuration);
        _UpdateLoop.Tick += ProcessGraphAnchorUpdates;
      }
      else
      {
        ConfigureMapping(_arSession.Configuration);
      }
    }

    private void ConstructNativeHandle(IARConfiguration arConfig)
    {
#pragma warning disable 0162
      if (NativeAccess.Mode != NativeAccess.ModeType.Native)
        return;
#pragma warning restore 0162

      var nativeNetworking = _networking as _NativeMultipeerNetworking;
      if (nativeNetworking == null)
        throw new IncorrectlyUsedNativeClassException();

      var worldConfiguration = arConfig as IARWorldTrackingConfiguration;

      var isUsingCloudMaps =
        worldConfiguration != null && (worldConfiguration.MappingRole != MappingRole.MapperIfHost);

      if (isUsingCloudMaps)
      {
        ARLog._Debug("Creating a native handle for the _NativeARNetworking with cloud maps");

        _nativeHandle =
          _NARARMultipeerNetworking_InitWithNetworking
          (
            nativeNetworking.GetNativeHandle(),
            (UInt16)NativeARNetworkingMode_Experimental.Experimental_1,
            worldConfiguration.MapLayerIdentifier._ToGuid(),
            (uint) (worldConfiguration.MappingRole == MappingRole.Mapper ? 0 : 1)
          );
      }
      else
      {
        ARLog._Debug("Creating a native handle for the _NativeARNetworking without cloud maps");

        _nativeHandle =
          _NARARMultipeerNetworking_InitWithNetworking
          (
            nativeNetworking.GetNativeHandle(),
            (UInt16)NativeARNetworkingMode_Experimental.Default,
            default(Guid),
            0
          );
      }

      // Inform the GC that this class is holding a large native object, so it gets cleaned up fast
      // TODO(awang): Make an IReleasable interface that handles this for all native-related classes
      GC.AddMemoryPressure(GCPressure);

      SubscribeToInternalCallbacks(true);
    }

    private void DisposeNativeHandle()
    {
#pragma warning disable 0162
      if (NativeAccess.Mode != NativeAccess.ModeType.Native)
        return;
#pragma warning restore 0162

      if (_nativeHandle != IntPtr.Zero)
      {
        _NARARMultipeerNetworking_Release(_nativeHandle);
        _nativeHandle = IntPtr.Zero;
        GC.RemoveMemoryPressure(GCPressure);
      }

      ARLog._Debug("Successfully released native ARNetworking object");
    }

    private void ConfigureMapping(IARConfiguration arConfig)
    {
      ARLog._Debug
      (
        "ARConfiguration changed, but toggling mapping is currently not supported. " +
        "Nothing needs to happen."
      );

      // Todo (kcho): Call native method to toggle mapping. This will be in the MVP.
      //throw new NotImplementedException("Toggling mapping is currently not supported. ");
    }

    public void Dispose()
    {
      _CheckThread();

      ARLog._Debug("Disposing _NativeARNetworking");
      GC.SuppressFinalize(this);
      Dispose(true);
    }

    private void Dispose(bool disposing)
    {
      if (IsDestroyed)
        return;

      if (disposing)
      {
        var deinitializing = Deinitialized;
        if (deinitializing != null)
        {
          var args = new ARNetworkingDeinitializedArgs();
          deinitializing(args);
        }

        _networking.PeerRemoved -= OnPeerRemoved;
        _arSession.Ran -= HandleConfigurationChange;
        _UpdateLoop.Tick -= ProcessGraphAnchorUpdates;

        if (_scanCoordinator != null)
        {
          _scanCoordinator.Dispose();
          _scanCoordinator = null;
        }
      }

      _cachedHandle.Free();
      _cachedHandleIntPtr = IntPtr.Zero;

      DisposeNativeHandle();

      IsDestroyed = true;
    }

    public void EnablePoseBroadcasting()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARARMultipeerNetworking_EnablePoseBroadcasting(_nativeHandle);
    }

    public void DisablePoseBroadcasting()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARARMultipeerNetworking_DisablePoseBroadcasting(_nativeHandle);
    }

    public void SetTargetPoseLatency(Int64 targetPoseLatency)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARARMultipeerNetworking_SetTargetPoseLatency(_nativeHandle, targetPoseLatency);
    }

    public void InitializeForMarkerScanning(Vector3[] markerPointLocations)
    {
      _CheckThread();

      if (_scanCoordinator != null)
      {
        var errorMessage =
          "Either a marker mapper or scanner session was already started with this " +
          "AR networking session.";

        ARLog._Error(errorMessage);
        return;
      }

      if (!Networking.IsConnected || !Networking.Host.Equals(Networking.Self))
      {
        var errorMessage =
          "The peer calling this method must be the host in an existing" +
          "networking session.";

        ARLog._Error(errorMessage);
        return;
      }

      _scanCoordinator = new MarkerScanCoordinator(this);
      _scanCoordinator.InitializeForMarkerScanning(markerPointLocations);
    }

    public void ScanForMarker
    (
      MarkerScanOption options,
      Action<MarkerMetadata> gotResult = null,
      IMarkerScanner scanner = null,
      IMetadataSerializer deserializer = null
    )
    {
      _CheckThread();

      if (_scanCoordinator != null)
      {
        var warningMessage =
          "Either a marker mapper or scanner session was already started with this " +
          "AR networking session.";

        ARLog._Warn(warningMessage);
        return;
      }

      if (Networking.IsConnected && (options & MarkerScanOption.ScanToJoin) != 0)
      {
        var errorMessage =
          "This peer has already joined a networking session, and so cannot join " +
          "a session using a marker.";

        ARLog._Error(errorMessage);
        return;
      }

      if (Networking.IsConnected && Networking.Host.Equals(Networking.Self))
      {
        var errorMessage =
          "The peer calling this method cannot be the host in an existing" +
          "networking session.";

        ARLog._Error(errorMessage);
        return;
      }

      _scanCoordinator = new MarkerScanCoordinator(this);
      _scanCoordinator.Finished += () => { _scanCoordinator = null; };

      _scanCoordinator.ScanForMarker(options, gotResult, scanner, deserializer);
    }

    private void ProcessGraphAnchorUpdates()
    {
      _networkAnchorManager.ProcessAllNewData();
    }

    // These callbacks are subscribed to at initialization as they set the data that will be provided
    // by accessors (LatestPeerPoses and LocalPeerState)
    private void SubscribeToInternalCallbacks(bool force = false)
    {
      _CheckThread();

      if (force)
      {
        _didReceivePoseFromPeerInitialized = false;
        _didReceiveStateFromPeerInitialized = false;
      }

      SubscribeToDidReceivePoseFromPeer();
      SubscribeToDidReceiveStateFromPeer();
    }

    private void InitializePeerStateTracking()
    {
      _readOnlyLatestPeerStates = new _ReadOnlyDictionary<IPeer, PeerState>(_latestPeerStates);

      _networking.PeerAdded += OnPeerAdded;
      _networking.PeerRemoved += OnPeerRemoved;
    }

    // Add a peer to state tracking when they join the session
    private void OnPeerAdded(PeerAddedArgs args)
    {
      _CheckThread();

      var peer = args.Peer;
      if(!_latestPeerStates.ContainsKey(peer))
        _latestPeerStates.Add(peer, PeerState.Unknown);
    }

    // Remove a peer from the internal pose and state tracking when they leave the session
    private void OnPeerRemoved(PeerRemovedArgs args)
    {
      _CheckThread();

      var peer = args.Peer;

      if (peer.Equals(_networking.Self))
      {
        LocalPeerState = PeerState.Unknown;
        _latestPeerStates.Clear();
        _networkAnchorManager.RemoveAllPeers();
      }
      else
      {
        _latestPeerStates.Remove(peer);
        _networkAnchorManager.RemovePeer(peer);
      }
    }

    // If the NativeARNetworking or its IMultipeerNetworking has been disposed before the callback
    //  is invoked, we should not surface the callback.
    private static bool _ShouldEarlyReturnFromNativeCallback(_NativeARNetworking arNetworking)
    {
      if (arNetworking == null)
        return true;

      if (arNetworking.IsDestroyed)
        return true;

      if (arNetworking._networking == null)
        return true;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        if (arNetworking._networking is _NativeMultipeerNetworking nativeNetworking)
        {
          if (nativeNetworking.IsDestroyed)
            return true;
        }
        else
        {
          return true;
        }
      }

      return false;
    }

    // Wrap both the native _Peer handling and testing _Peer generation from a native handle in a
    //  single method.
    // Because the native callbacks are expecting IntPtrs, the TestingShim will register test _Peers
    //  to a list, then use the IntPtr to index into the list to find the correct peer.
    // Because we are accessing testing _Peers through indexing into a List, tests that invoke
    //  PeerState or PeerPose updates cannot run concurrently.
    private static IPeer _GetPeerFromHandle(IntPtr handle)
    {
#pragma warning disable CS0162
      if (NativeAccess.Mode == NativeAccess.ModeType.Testing)
      {
        return _TestingShim._GetPeerFromTestingHandle(handle);
      }
#pragma warning restore CS0162


      return _Peer.FromNativeHandle(handle);
    }

#region TestingShim
    internal static class _TestingShim
    {
      private static List<IPeer> _peerLookup;
      private static List<GCHandle> _pinnedPoses;

      internal static IPeer _GetPeerFromTestingHandle(IntPtr handle)
      {
        if (NativeAccess.Mode != NativeAccess.ModeType.Testing)
          return null;
#pragma warning disable CS0162
        var index = handle.ToInt32();
        return _peerLookup[index];
#pragma warning restore CS0162
      }

      // Invoke the native peer state updated callback with the specified parameters.
      internal static void _InvokePeerStateUpdate
      (
        _NativeARNetworking arNetworking,
        UInt32 rawPeerState,
        IPeer peer
      )
      {
        if (NativeAccess.Mode != NativeAccess.ModeType.Testing)
          return;
#pragma warning disable CS0162
        if (_peerLookup == null)
          _peerLookup = new List<IPeer>();

        if (!_peerLookup.Contains(peer))
          _peerLookup.Add(peer);

        var testingHandle = new IntPtr(_peerLookup.IndexOf(peer));

        _onDidReceiveStateFromPeerNative(arNetworking._applicationHandle, rawPeerState, testingHandle);
#pragma warning restore CS0162
      }

      // Invoke the native peer pose updated callback with the specified parameters
      internal static void _InvokePeerPoseUpdate
      (
        _NativeARNetworking arNetworking,
        float[] nativePose,
        IPeer peer
      )
      {
        if (NativeAccess.Mode != NativeAccess.ModeType.Testing)
          return;
#pragma warning disable CS0162
        if (_peerLookup == null)
          _peerLookup = new List<IPeer>();

        if (!_peerLookup.Contains(peer))
          _peerLookup.Add(peer);

        var testingHandle = new IntPtr(_peerLookup.IndexOf(peer));

        if (_pinnedPoses == null)
          _pinnedPoses = new List<GCHandle>();

        var pinnedInternalArray = GCHandle.Alloc(nativePose, GCHandleType.Pinned);

        try
        {
          _pinnedPoses.Add(pinnedInternalArray);

          _onDidReceivePoseFromPeerNative
          (
            arNetworking._applicationHandle,
            pinnedInternalArray.AddrOfPinnedObject(),
            testingHandle
          );
        }
        finally
        {
          pinnedInternalArray.Free();
          _pinnedPoses.Remove(pinnedInternalArray);
        }
#pragma warning restore CS0162
      }

      internal static void _InvokeNetworkAnchorManagerUpdate(_NativeARNetworking arNetworking)
      {
        // This is required to update pose dictionary managed by the NetworkAnchorManager. Because
        //  the NetworkAnchorManager update happens before the CallbackQueue, the pose dictionary
        //  will always be one frame behind events.
        arNetworking.ProcessGraphAnchorUpdates();
      }

      internal static void Reset()
      {
        if(_peerLookup != null)
          _peerLookup.Clear();

        if (_pinnedPoses != null)
        {
          foreach (var pose in _pinnedPoses)
          {
            pose.Free();
          }
          _pinnedPoses.Clear();
        }
      }
    }
#endregion

#region PoseFromPeer
    private bool _didReceivePoseFromPeerInitialized = false;

    private void SubscribeToDidReceivePoseFromPeer()
    {
      _CheckThread();

      if (_didReceivePoseFromPeerInitialized)
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARARMultipeerNetworking_Set_didReceivePoseFromPeerCallback
        (
          _applicationHandle,
          _nativeHandle,
          _onDidReceivePoseFromPeerNative
        );
      }

      _didReceivePoseFromPeerInitialized = true;
    }

    private ArdkEventHandler<PeerPoseReceivedArgs> _peerPoseReceived;

    /// <inheritdoc />
    public event ArdkEventHandler<PeerPoseReceivedArgs> PeerPoseReceived
    {
      add
      {
        SubscribeToDidReceivePoseFromPeer();
        _peerPoseReceived += value;
      }
      remove
      {
        _peerPoseReceived -= value;
      }
    }

    [MonoPInvokeCallback(typeof(_NARARMultipeerNetworking_Did_Receive_Pose_From_Peer_CallbackDelegate))]
    private static void _onDidReceivePoseFromPeerNative
    (
       IntPtr context,
       IntPtr pose,
       IntPtr peerHandle
    )
    {
      var arNetworking = SafeGCHandle.TryGetInstance<_NativeARNetworking>(context);

      if (_ShouldEarlyReturnFromNativeCallback(arNetworking))
      {
        _Peer._ReleasePeer(peerHandle);
        return;
      }

      var poseSchema = new float[16];
      Marshal.Copy(pose, poseSchema, 0, 16);
      var poseMatrix = NARConversions.FromNARToUnity(_Convert.InternalToMatrix4x4(poseSchema));

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (_ShouldEarlyReturnFromNativeCallback(arNetworking))
          {
            _Peer._ReleasePeer(peerHandle);
            return;
          }

          var peer = _GetPeerFromHandle(peerHandle);

          ARLog._DebugFormat
          (
            "Handling pose from peer {0}",
            true,
            peer.Identifier
          );

          var networkAnchorManager = arNetworking._networkAnchorManager;
          if (networkAnchorManager != null)
            networkAnchorManager.QueuePeerPose(peer, poseMatrix);

          var peerPoseReceived = arNetworking._peerPoseReceived;
          if (peerPoseReceived != null)
          {
            var args = new PeerPoseReceivedArgs(peer, poseMatrix);
            peerPoseReceived(args);
          }
        }
      );
    }
#endregion

#region StateFromPeer
    private bool _didReceiveStateFromPeerInitialized = false;

    private void SubscribeToDidReceiveStateFromPeer()
    {
      _CheckThread();

      if (_didReceiveStateFromPeerInitialized)
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARARMultipeerNetworking_Set_didReceiveStateFromPeerCallback
        (
          _applicationHandle,
          _nativeHandle,
          _onDidReceiveStateFromPeerNative
        );
      }

      _didReceiveStateFromPeerInitialized = true;
    }

    private ArdkEventHandler<PeerStateReceivedArgs> _peerStateReceived;

    /// <inheritdoc />
    public event ArdkEventHandler<PeerStateReceivedArgs> PeerStateReceived
    {
      add
      {
        SubscribeToDidReceiveStateFromPeer();
        _peerStateReceived += value;
      }
      remove
      {
        _peerStateReceived -= value;
      }
    }

    [MonoPInvokeCallback(typeof(_NARARMultipeerNetworking_Did_Receive_State_From_Peer_CallbackDelegate))]
    private static void _onDidReceiveStateFromPeerNative
    (
      IntPtr context,
      UInt32 rawPeerState,
      IntPtr peerHandle
    )
    {
      var arNetworking = SafeGCHandle.TryGetInstance<_NativeARNetworking>(context);
      if (_ShouldEarlyReturnFromNativeCallback(arNetworking))
      {
        // arNetworking was deallocated
        _Peer._ReleasePeer(peerHandle);
        return;
      }

      var peerState = (PeerState)rawPeerState;

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (_ShouldEarlyReturnFromNativeCallback(arNetworking))
          {
            _Peer._ReleasePeer(peerHandle);
            return;
          }

          var peer = _GetPeerFromHandle(peerHandle);
          if (peer.Equals(arNetworking._networking.Self))
            arNetworking.LocalPeerState = peerState;

          arNetworking._latestPeerStates[peer] = peerState;

          ARLog._DebugFormat
          (
            "Handling state from peer {0}",
            true,
            peer.Identifier
          );

          var peerStateReceived = arNetworking._peerStateReceived;
          if (peerStateReceived != null)
          {
            var args = new PeerStateReceivedArgs(peer, peerState);
            peerStateReceived(args);
          }
        }
      );
    }
#endregion

    /// Event fired when instances of this class are deinitializing.
    public event ArdkEventHandler<ARNetworkingDeinitializedArgs> Deinitialized;

    // Private handles and callbacks to handle native code and callbacks

    private IntPtr _nativeHandle = IntPtr.Zero;

    private IntPtr _cachedHandleIntPtr = IntPtr.Zero;
    private SafeGCHandle<_NativeARNetworking> _cachedHandle;

    // Approx memory consumption of native objects
    // Magic number representing 150MB, from profiling an iPhone 8
    private const long GCPressure = 150L * 1024L * 1024L;

    // Used to round-trip a pointer through c++,
    // so that we can keep our this pointer even in c# functions
    // marshaled and called by native code
    private IntPtr _applicationHandle
    {
      get
      {
        _CheckThread();

        if (_cachedHandleIntPtr != IntPtr.Zero)
          return _cachedHandleIntPtr;

        _cachedHandle = SafeGCHandle.Alloc(this);
        _cachedHandleIntPtr = _cachedHandle.ToIntPtr();
        return _cachedHandleIntPtr;
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARARMultipeerNetworking_InitWithNetworking
    (
      IntPtr nativeMultipeerNetworking,
      UInt16 arNetworkingModeExperimental,
      Guid mapLayerIdentifier,
      UInt32 localizationOnly
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARARMultipeerNetworking_Release(IntPtr nativeHandle);


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARARMultipeerNetworking_EnablePoseBroadcasting
    (
      IntPtr nativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARARMultipeerNetworking_DisablePoseBroadcasting
    (
      IntPtr nativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARARMultipeerNetworking_SetTargetPoseLatency
    (
      IntPtr nativeHandle,
      Int64 targetPoseLatency
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARARMultipeerNetworking_GetNetworking(IntPtr nativeHandle);


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARARMultipeerNetworking_Set_didReceivePoseFromPeerCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARARMultipeerNetworking_Did_Receive_Pose_From_Peer_CallbackDelegate cb
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARARMultipeerNetworking_Set_didReceiveStateFromPeerCallback
    (
      IntPtr context,
      IntPtr nativeHandle,
      _NARARMultipeerNetworking_Did_Receive_State_From_Peer_CallbackDelegate cb
    );

    private delegate void _NARARMultipeerNetworking_Did_Receive_Pose_From_Peer_CallbackDelegate
    (
      IntPtr context,
      IntPtr pose,
      IntPtr peer
    );

    private delegate void _NARARMultipeerNetworking_Did_Receive_State_From_Peer_CallbackDelegate
    (
      IntPtr context,
      UInt32 peerState,
      IntPtr peer
    );
  }
}
