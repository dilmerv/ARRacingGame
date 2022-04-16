// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Depth.Generators;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Localization;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.AR.PointCloud;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Extensions.Meshing;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.VirtualStudio.AR
{
  internal sealed class _RemoteEditorARSession:
    _IARSession,
    ILocalizableARSession
  {
    private DepthPointCloudGenerator _depthPointCloudGen;

    private Dictionary<Guid, _SerializableARAnchor> _editorAnchors =
      new Dictionary<Guid, _SerializableARAnchor>();
    private Dictionary<Guid, Guid> _editorToDeviceAnchorIdentifiers = new Dictionary<Guid, Guid>();
    private Dictionary<Guid, Guid> _deviceToEditorAnchorIdentifiers = new Dictionary<Guid, Guid>();

    internal _RemoteEditorARSession(Guid stageIdentifier)
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(ARSessionFactory));

      StageIdentifier = stageIdentifier;
      ARSessionChangesCollector = new ARSessionChangesCollector(this);

      _EasyConnection.Register<ARSessionFrameUpdatedMessage>(HandleDidUpdateFrame);

      _EasyConnection.Register<ARSessionMapsAddedMessage>(HandleDidAddMaps);
      _EasyConnection.Register<ARSessionMapsUpdatedMessage>(HandleDidUpdateMaps);

      _EasyConnection.Register<ARSessionAddedCustomAnchorMessage>(HandleAddedAnchor);
      _EasyConnection.Register<ARSessionAnchorsAddedMessage>(HandleDidAddAnchors);
      _EasyConnection.Register<ARSessionAnchorsUpdatedMessage>(HandleDidUpdateAnchors);
      _EasyConnection.Register<ARSessionAnchorsMergedMessage>(HandleDidMergeAnchors);
      _EasyConnection.Register<ARSessionAnchorsRemovedMessage>(HandleDidRemoveAnchors);

      _EasyConnection.Register<ARSessionCameraTrackingStateChangedMessage>
      (
        HandleCameraDidChangeTrackingState
      );

      _EasyConnection.Register<ARSessionWasInterruptedMessage>(HandleSessionWasInterrupted);
      _EasyConnection.Register<ARSessionInterruptionEndedMessage>(HandleSessionInterruptionEnded);
      _EasyConnection.Register<ARSessionFailedMessage>(HandleDidFailWithError);

      _EasyConnection.Send
      (
        new ARSessionInitMessage
        {
          StageIdentifier = stageIdentifier,
#if UNITY_EDITOR_OSX // Only can do image compression on OSX
          ImageCompressionQuality = _RemoteBufferConfiguration.ImageCompression,
#endif
          TargetImageFramerate = _RemoteBufferConfiguration.ImageFramerate,
          TargetBufferFramerate = _RemoteBufferConfiguration.AwarenessFramerate
        },
        TransportType.ReliableOrdered
      );
    }

    ~_RemoteEditorARSession()
    {
      ARLog._Error("_RemoteEditorARSession should be destroyed by an explicit call to Dispose().");
    }

    private bool _isDestroyed;
    public void Dispose()
    {
      if (_isDestroyed)
        return;

      _isDestroyed = true;
      GC.SuppressFinalize(this);

      var handler = Deinitialized;
      if (handler != null)
      {
        var args = new ARSessionDeinitializedArgs();
        handler(args);
      }

      _EasyConnection.Unregister<ARSessionFrameUpdatedMessage>();
      _EasyConnection.Unregister<ARSessionMeshUpdatedMessage>();

      _EasyConnection.Unregister<ARSessionAddedCustomAnchorMessage>();
      _EasyConnection.Unregister<ARSessionAnchorsAddedMessage>();
      _EasyConnection.Unregister<ARSessionAnchorsUpdatedMessage>();
      _EasyConnection.Unregister<ARSessionAnchorsMergedMessage>();
      _EasyConnection.Unregister<ARSessionAnchorsRemovedMessage>();

      _EasyConnection.Unregister<ARSessionMapsAddedMessage>();
      _EasyConnection.Unregister<ARSessionMapsUpdatedMessage>();

      _EasyConnection.Unregister<ARSessionCameraTrackingStateChangedMessage>();
      _EasyConnection.Unregister<ARSessionWasInterruptedMessage>();
      _EasyConnection.Unregister<ARSessionInterruptionEndedMessage>();
      _EasyConnection.Unregister<ARSessionFailedMessage>();

      _EasyConnection.Send(new ARSessionDestroyMessage(), TransportType.ReliableOrdered);

      // Dispose of any generators that we've created.
      DisposeGenerators();
    }

    private void DisposeGenerators()
    {
      var depthPointCloudGen = _depthPointCloudGen;
      if (depthPointCloudGen != null)
      {
        _depthPointCloudGen = null;
        depthPointCloudGen.Dispose();
      }
    }
    
    private ILocalizer _localizer;
    /// @note Currently use a mock localizer for remote. Real localization will not be run on device
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    public ILocalizer Localizer
    {
      get => _localizer ?? (_localizer = new _MockLocalizer(this));
    }

    public Guid StageIdentifier { get; private set; }

    private IARFrame _currentFrame;
    /// <inheritdoc />
    public IARFrame CurrentFrame
    {
      get { return _currentFrame; }
      internal set
      {
        _SessionFrameSharedLogic._MakeSessionFrameBecomeNonCurrent(this);
        _currentFrame = value;
      }
    }

    /// <inheritdoc />
    public ARFrameDisposalPolicy DefaultFrameDisposalPolicy { get; set; }

    public IARConfiguration Configuration { get; private set; }

    private float _worldScale = 1.0f;
    public float WorldScale
    {
      get { return _worldScale; }
      set
      {
        _EasyConnection.Send
        (
          new ARSessionSetWorldScaleMessage { WorldScale = value },
          TransportType.ReliableOrdered
        );

        _worldScale = value;
      }
    }

    public ARSessionState State { get; private set; }

    public ARSessionChangesCollector ARSessionChangesCollector { get; }

    public ARSessionRunOptions RunOptions { get; private set; }

    public void Run
    (
      IARConfiguration configuration,
      ARSessionRunOptions options = ARSessionRunOptions.None
    )
    {
      ARSessionChangesCollector._CollectChanges(configuration, ref options);

      if (!_ARConfigurationValidator.RunAllChecks(this, configuration))
        return;

      Configuration = configuration;
      RunOptions = options;

      State = ARSessionState.Running;

      // Need to destroy the generators so they can be recreated once we get new depth data
      DisposeGenerators();

      if ((RunOptions & ARSessionRunOptions.RemoveExistingMesh) != 0)
        _meshDataParser.Clear();

      var message = new ARSessionRunMessage
      {
        arConfiguration = configuration,
        runOptions = options
      };

      ARLog._DebugFormat("Running _RemoteEditorARSession with options: {0}", false, options);
      _EasyConnection.Send(message, TransportType.ReliableOrdered);

      var handler = _onDidRun;
      if (handler != null)
        handler(new ARSessionRanArgs());
    }

    public void Pause()
    {
      if (State != ARSessionState.Running)
        return;

      State = ARSessionState.Paused;
      _EasyConnection.Send(new ARSessionPauseMessage(), TransportType.ReliableOrdered);

      var handler = Paused;
      if (handler != null)
        handler(new ARSessionPausedArgs());
    }

    public IARAnchor AddAnchor(Matrix4x4 transform)
    {
      var identifier = Guid.NewGuid();
      var anchor = new _SerializableARBaseAnchor(transform, identifier);

      _editorAnchors.Add(identifier, anchor);

      ARLog._DebugFormat("Sending AddAnchor request (editor id: {0})", false, identifier);

      _EasyConnection.Send
      (
        new ARSessionAddAnchorMessage { Anchor = anchor },
        TransportType.ReliableOrdered
      );

      return anchor;
    }

    public void RemoveAnchor(IARAnchor anchor)
    {
      ARLog._DebugFormat("Sending RemoveAnchor request (editor id: {0})", false, anchor.Identifier);
      var deviceIdentifier = _editorToDeviceAnchorIdentifiers[anchor.Identifier];

      _EasyConnection.Send
      (
        new ARSessionRemoveAnchorMessage { DeviceAnchorIdentifier = deviceIdentifier },
        TransportType.ReliableOrdered
      );
    }

    public AwarenessInitializationStatus GetAwarenessInitializationStatus
    (
      out AwarenessInitializationError error,
      out string errorMessage
    )
    {
      ARLog._Warn
      (
        "Checking the status of Awareness features in a Remote ARSession is not yet implemented. " +
        "Will always return a Ready status."
      );

      error = AwarenessInitializationError.None;
      errorMessage = string.Empty;

      return AwarenessInitializationStatus.Ready;
    }

    private void HandleDidUpdateFrame(ARSessionFrameUpdatedMessage message)
    {
      var frame = message.Frame;
      UpdateGenerators(frame);

      _InvokeFrameUpdated(frame);
    }

    private void _InvokeFrameUpdated(IARFrame frame)
    {
      CurrentFrame = frame;

      var handler = FrameUpdated;
      if (handler != null)
      {
        var args = new FrameUpdatedArgs(frame);
        handler(args);
      }
    }

    // TODO: Pull depth point cloud generation into an extension so this code isn't duplicated
    // from _NativeARSession
    private void UpdateGenerators(IARFrame frame)
    {
      if (!(Configuration is IARWorldTrackingConfiguration worldConfig))
        return;

      var pointCloudsEnabled = worldConfig.DepthPointCloudSettings.IsEnabled;
      if (!pointCloudsEnabled)
        return;

      var depthBuffer = frame.Depth;
      if (depthBuffer == null || !depthBuffer.IsKeyframe)
        return;

      // Create a generator if needed
      if (_depthPointCloudGen == null)
      {
        _depthPointCloudGen =
          new DepthPointCloudGenerator
          (
            worldConfig.DepthPointCloudSettings
          );
      }

      // Generate the point cloud
      var pointCloud = _depthPointCloudGen.GeneratePointCloud(frame.Depth, frame.Camera);

      var frameBase = (_ARFrameBase)frame;
      frameBase.DepthPointCloud = pointCloud;
    }

    private void HandleAddedAnchor(ARSessionAddedCustomAnchorMessage message)
    {
      ARLog._DebugFormat
      (
        "Added anchor (editor: {0}, device: {1} ",
        false,
        message.EditorIdentifier,
        message.Anchor.Identifier
      );

      // There is a small chance this message will get received/handled after the anchor gets
      // surfaced through HandleDidAddAnchors, but that will get fixed once networking reliability
      // gets fixed. So just add the anchors here instead of also adding them in HandleDidAddAnchors.

      _editorToDeviceAnchorIdentifiers.Add(message.EditorIdentifier, message.Anchor.Identifier);
      _deviceToEditorAnchorIdentifiers.Add(message.Anchor.Identifier, message.EditorIdentifier);
    }

    private void HandleDidAddAnchors(ARSessionAnchorsAddedMessage message)
    {
      var anchors =
        new IARAnchor
        [
          message.Anchors.Length +
          message.PlaneAnchors.Length +
          message.ImageAnchors.Length
        ];

      var i = 0;
      foreach (var anchor in message.Anchors)
      {
        // This returns the same IARAnchor object as in the AddAnchor method,
        // same as in _NativeARSession.
        if (_deviceToEditorAnchorIdentifiers.TryGetValue(anchor.Identifier, out Guid id))
        {
          var editorAnchor = _editorAnchors[id];
          editorAnchor.Transform = anchor.Transform; // This might not be needed
          anchors[i++] = editorAnchor;
        }
        else
        {
          ARLog._WarnFormat
          (
            "An anchor was added by the device session that was not added by the editor session."
          );

          anchors[i++] = anchor;
        }
      }

      foreach (var anchor in message.PlaneAnchors)
      {
        anchors[i++] = anchor;
      }

      foreach (var anchor in message.ImageAnchors)
      {
        anchors[i++] = anchor;
      }

      var handler = AnchorsAdded;
      if (handler != null)
      {
        var args = new AnchorsArgs(anchors);
        handler(args);
      }
    }

    private void HandleDidUpdateAnchors(ARSessionAnchorsUpdatedMessage message)
    {
      var anchors =
        new IARAnchor
        [
          message.Anchors.Length +
          message.PlaneAnchors.Length +
          message.ImageAnchors.Length
        ];

      var i = 0;
      foreach (var anchor in message.Anchors)
      {
        if (_deviceToEditorAnchorIdentifiers.TryGetValue(anchor.Identifier, out Guid id))
        {
          var editorAnchor = _editorAnchors[id];
          editorAnchor.Transform = anchor.Transform;
          anchors[i++] = anchor;
        }
        else
        {
          ARLog._WarnFormat
          (
            "An anchor was updated by the device session that was not added by the editor session."
          );

          anchors[i++] = anchor;
        }
      }

      foreach (var anchor in message.PlaneAnchors)
      {
        anchors[i++] = anchor;
      }

      foreach (var anchor in message.ImageAnchors)
      {
        anchors[i++] = anchor;
      }

      var handler = AnchorsUpdated;
      if (handler != null)
      {
        var args = new AnchorsArgs(anchors);
        handler(args);
      }
    }

    private void HandleDidRemoveAnchors(ARSessionAnchorsRemovedMessage message)
    {
      var anchors =
        new IARAnchor
        [
          message.Anchors.Length +
          message.PlaneAnchors.Length +
          message.ImageAnchors.Length
        ];

      var i = 0;
      foreach (var anchor in message.Anchors)
      {
        if (_deviceToEditorAnchorIdentifiers.TryGetValue(anchor.Identifier, out Guid id))
        {
          var editorAnchor = _editorAnchors[id];
          anchors[i++] = editorAnchor;

          _editorAnchors.Remove(id);
          _editorToDeviceAnchorIdentifiers.Remove(id);
          _deviceToEditorAnchorIdentifiers.Remove(anchor.Identifier);
        }
        else
        {
          ARLog._WarnFormat
          (
            "An anchor was removed by the device session that was not added by the editor session."
          );

          anchors[i++] = anchor;
        }
      }

      foreach (var anchor in message.PlaneAnchors)
      {
        anchors[i++] = anchor;
      }

      foreach (var anchor in message.ImageAnchors)
      {
        anchors[i++] = anchor;
      }

      var handler = AnchorsRemoved;
      if (handler != null)
      {
        var args = new AnchorsArgs(anchors);
        handler(args);
      }
    }

    private void HandleDidMergeAnchors(ARSessionAnchorsMergedMessage message)
    {
      IARAnchor parent = message.ParentAnchor;

      var handler = AnchorsMerged;
      if (handler != null)
      {
        var args = new AnchorsMergedArgs(parent, message.ChildAnchors);
        handler(args);
      }
    }

    private void HandleDidAddMaps(ARSessionMapsAddedMessage message)
    {
      var handler = MapsAdded;
      if (handler != null)
      {
        var args = new MapsArgs(message.Maps);
        handler(args);
      }
    }

    private void HandleDidUpdateMaps(ARSessionMapsUpdatedMessage message)
    {
      var handler = MapsUpdated;
      if (handler != null)
      {
        var args = new MapsArgs(message.Maps);
        handler(args);
      }
    }

    private void HandleCameraDidChangeTrackingState
    (
      ARSessionCameraTrackingStateChangedMessage message
    )
    {
      var camera = message.Camera;

      var handler = CameraTrackingStateChanged;
      if (handler != null)
      {
        var args = new CameraTrackingStateChangedArgs(camera, camera.TrackingState);
        handler(args);
      }
    }

    private void HandleSessionWasInterrupted(ARSessionWasInterruptedMessage message)
    {
      var handler = SessionInterrupted;
      if (handler != null)
        handler(new ARSessionInterruptedArgs());
    }

    private void HandleSessionInterruptionEnded(ARSessionInterruptionEndedMessage message)
    {
      var handler = SessionInterruptionEnded;
      if (handler != null)
        handler(new ARSessionInterruptionEndedArgs());
    }

    private void HandleDidFailWithError(ARSessionFailedMessage message)
    {
      var handler = SessionFailed;
      if (handler != null)
      {
        var args = new ARSessionFailedArgs(message.Error);
        handler(args);
      }
    }

    private ArdkEventHandler<ARSessionRanArgs> _onDidRun;
    public event ArdkEventHandler<ARSessionRanArgs> Ran
    {
      add
      {
        _onDidRun += value;

        if (State == ARSessionState.Running)
          value(new ARSessionRanArgs());
      }
      remove
      {
        _onDidRun -= value;
      }
    }

    public event ArdkEventHandler<ARSessionPausedArgs> Paused;
    public event ArdkEventHandler<ARSessionDeinitializedArgs> Deinitialized;

    public event ArdkEventHandler<ARSessionInterruptedArgs> SessionInterrupted;
    public event ArdkEventHandler<ARSessionInterruptionEndedArgs> SessionInterruptionEnded;
    public event ArdkEventHandler<ARSessionFailedArgs> SessionFailed;
    public event ArdkEventHandler<CameraTrackingStateChangedArgs> CameraTrackingStateChanged;

    public event ArdkEventHandler<FrameUpdatedArgs> FrameUpdated;
    public event ArdkEventHandler<AnchorsMergedArgs> AnchorsMerged;
    public event ArdkEventHandler<AnchorsArgs> AnchorsAdded;
    public event ArdkEventHandler<AnchorsArgs> AnchorsUpdated;
    public event ArdkEventHandler<AnchorsArgs> AnchorsRemoved;
    public event ArdkEventHandler<MapsArgs> MapsAdded;
    public event ArdkEventHandler<MapsArgs> MapsUpdated;

    RuntimeEnvironment IARSession.RuntimeEnvironment
    {
      get { return RuntimeEnvironment.Remote; }
    }

    public IARMesh Mesh
    {
      get { return _meshDataParser; }
    }

    private _MeshDataParser _meshDataParser = new _MeshDataParser();

    void IARSession.SetupLocationService(ILocationService locationService)
    {
      // Todo: figure out support
      throw new NotSupportedException("LocationService is not supported with Remote ARSessions.");
    }

    event ArdkEventHandler<QueryingShouldSessionAttemptRelocalizationArgs> IARSession.QueryingShouldSessionAttemptRelocalization
    {
      add { /* Do nothing. */ }
      remove { /* Do nothing. */}
    }
  }
}
