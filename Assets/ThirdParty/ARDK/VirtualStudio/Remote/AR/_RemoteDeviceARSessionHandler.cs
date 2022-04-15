// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  internal sealed class _RemoteDeviceARSessionHandler
  {
    private readonly IARSession _session;

    public IARSession InnerARSession
    {
      get { return _session; }
    }

    private readonly int _imageCompressionQuality;
    private readonly float _targetImageFrameDelta;
    private readonly float _targetAwarenessFrameDelta;
    private Dictionary<Guid, IARAnchor> _addedAnchors = new Dictionary<Guid, IARAnchor>();

    internal _RemoteDeviceARSessionHandler
    (
      Guid stageIdentifier,
      int imageCompressionQuality,
      int targetImageFramerate,
      int targetAwarenessFramerate
    )
    {
      _session = ARSessionFactory.Create(stageIdentifier);

      _imageCompressionQuality = imageCompressionQuality;
      _targetImageFrameDelta = 1.0f / targetImageFramerate;
      _targetAwarenessFrameDelta = 1.0f / targetAwarenessFramerate;

      _EasyConnection.Register<ARSessionRunMessage>(HandleRunMessage);
      _EasyConnection.Register<ARSessionPauseMessage>(HandlePauseMessage);
      _EasyConnection.Register<ARSessionAddAnchorMessage>(HandleAddAnchorMessage);
      _EasyConnection.Register<ARSessionRemoveAnchorMessage>(HandleRemoveAnchorMessage);
      _EasyConnection.Register<ARSessionSetWorldScaleMessage>(HandleSetWorldScaleMessage);
      _EasyConnection.Register<ARSessionDestroyMessage>(HandleDestroyMessage);

      _session.FrameUpdated += OnFrameUpdated;
      _session.AnchorsAdded += OnAnchorsAdded;
      _session.AnchorsUpdated += OnAnchorsUpdated;
      _session.AnchorsRemoved += OnAnchorsRemoved;
      _session.AnchorsMerged += OnAnchorsMerged;
      _session.MapsAdded += OnMapsAdded;
      _session.MapsUpdated += OnMapsUpdated;

      _session.CameraTrackingStateChanged += OnCameraTrackingStateChanged;
      _session.SessionInterrupted += OnSessionInterrupted;
      _session.SessionInterruptionEnded += OnSessionInterruptionEnded;
      _session.SessionFailed += OnSessionFailed;
    }

    private bool _isDestroyed;

    ~_RemoteDeviceARSessionHandler()
    {
      ARLog._Error("_RemoteDeviceARSessionHandler should be destroyed by an explicit call to Dispose().");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      if (_isDestroyed)
        return;

      _isDestroyed = true;

      _EasyConnection.Unregister<ARSessionRunMessage>();
      _EasyConnection.Unregister<ARSessionPauseMessage>();
      _EasyConnection.Unregister<ARSessionAddAnchorMessage>();
      _EasyConnection.Unregister<ARSessionRemoveAnchorMessage>();
      _EasyConnection.Unregister<ARSessionSetWorldScaleMessage>();
      _EasyConnection.Unregister<ARSessionDestroyMessage>();

      _session.FrameUpdated -= OnFrameUpdated;
      _session.AnchorsAdded -= OnAnchorsAdded;
      _session.AnchorsUpdated -= OnAnchorsUpdated;
      _session.AnchorsRemoved -= OnAnchorsRemoved;
      _session.AnchorsMerged -= OnAnchorsMerged;
      _session.MapsAdded -= OnMapsAdded;
      _session.MapsUpdated -= OnMapsUpdated;

      _session.CameraTrackingStateChanged -= OnCameraTrackingStateChanged;
      _session.SessionInterrupted -= OnSessionInterrupted;
      _session.SessionInterruptionEnded -= OnSessionInterruptionEnded;
      _session.SessionFailed -= OnSessionFailed;

      _session.Dispose();
    }

#region CallbackFowarding

    private static float _lastImageCaptureTime;
    private static float _lastAwarenessCaptureTime;
    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      var frame = args.Frame;

      var includeImageBuffers = false;
      var includeAwarenessBuffers = false;
      var unscaledTime = Time.unscaledTime;

      if ((unscaledTime - _lastImageCaptureTime) > _targetImageFrameDelta)
      {
        includeImageBuffers = true;
        _lastImageCaptureTime = unscaledTime;
      }

      if ((unscaledTime - _lastAwarenessCaptureTime) > _targetAwarenessFrameDelta)
      {
        includeAwarenessBuffers = true;
        _lastAwarenessCaptureTime = unscaledTime;
      }
      
      //TODO: AR-8359 remove the serialize as soon as possible. This warning disabling is temporary.
#pragma warning disable CS0618
      var serializedFrame =
        frame.Serialize
        (
          includeImageBuffers,
          includeAwarenessBuffers,
          _imageCompressionQuality
        );
#pragma warning restore CS0618
      
      // Todo: Should be sending UnreliableOrdered, but Unreliable protocols won’t deliver any
      // message if payload size is ~1.8KB or more.
      _EasyConnection.Send
      (
        new ARSessionFrameUpdatedMessage{ Frame = serializedFrame._AsSerializable() },
        TransportType.ReliableUnordered
      );
    }

    private static void OnAnchorsAdded(AnchorsArgs args)
    {
      var anchors = args.Anchors;
      var anchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Base
        select anchor._AsSerializableBase()).ToArray();

      var planeAnchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Plane
        select ((IARPlaneAnchor)anchor)._AsSerializablePlane()).ToArray();

      var imageAnchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Image
        select ((IARImageAnchor)anchor)._AsSerializableImage()).ToArray();


      // TODO: We could just serialize everything in a single array.
      // The serializer knows what to do!
      _EasyConnection.Send
      (
        new ARSessionAnchorsAddedMessage
        {
          Anchors = anchorArray,
          PlaneAnchors = planeAnchorArray,
          ImageAnchors = imageAnchorArray
        }
      );
    }

    private static void OnAnchorsUpdated(AnchorsArgs args)
    {
      var anchors = args.Anchors;
      var anchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Base
        select anchor._AsSerializableBase()).ToArray();

      var planeAnchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Plane
        select ((IARPlaneAnchor)anchor)._AsSerializablePlane()).ToArray();

      var imageAnchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Image
        select ((IARImageAnchor)anchor)._AsSerializableImage()).ToArray();


      _EasyConnection.Send
      (
        new ARSessionAnchorsUpdatedMessage
        {
          Anchors = anchorArray,
          PlaneAnchors = planeAnchorArray,
          ImageAnchors = imageAnchorArray
        }
      );
    }

    private static void OnAnchorsRemoved(AnchorsArgs args)
    {
      var anchors = args.Anchors;
      var anchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Base
        select anchor._AsSerializableBase()).ToArray();

      var planeAnchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Plane
        select ((IARPlaneAnchor)anchor)._AsSerializablePlane()).ToArray();

      var imageAnchorArray = (
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Image
        select ((IARImageAnchor)anchor)._AsSerializableImage()).ToArray();

      _EasyConnection.Send
      (
        new ARSessionAnchorsRemovedMessage
        {
          Anchors = anchorArray,
          PlaneAnchors = planeAnchorArray,
          ImageAnchors = imageAnchorArray
        }
      );
    }

    private static void OnAnchorsMerged(AnchorsMergedArgs args)
    {
      var parentQuery = ((IARPlaneAnchor)args.Parent)._AsSerializablePlane();

      var childrenArray =
        (
          from child in args.Children
          select ((IARPlaneAnchor)child)._AsSerializablePlane()
        ).ToArray();

      _EasyConnection.Send
      (
        new ARSessionAnchorsMergedMessage
        {
          ParentAnchor = parentQuery, ChildAnchors = childrenArray
        }
      );
    }

    private static void OnMapsAdded(MapsArgs args)
    {
      var serializableMaps = args.Maps._AsSerializableArray();
      _EasyConnection.Send(new ARSessionMapsAddedMessage { Maps = serializableMaps });
    }

    private static void OnMapsUpdated(MapsArgs args)
    {
      var serializableMaps = args.Maps._AsSerializableArray();
      _EasyConnection.Send(new ARSessionMapsUpdatedMessage { Maps = serializableMaps });
    }

    private static void OnCameraTrackingStateChanged(CameraTrackingStateChangedArgs args)
    {
      var serializableCamera = args.Camera._AsSerializable();
      if (serializableCamera == null)
        return;

      _EasyConnection.Send
      (
        new ARSessionCameraTrackingStateChangedMessage { Camera = serializableCamera }
      );
    }

    private static void OnSessionInterrupted(ARSessionInterruptedArgs args)
    {
      _EasyConnection.Send(new ARSessionWasInterruptedMessage());
    }

    private static void OnSessionInterruptionEnded(ARSessionInterruptionEndedArgs args)
    {
      _EasyConnection.Send(new ARSessionInterruptionEndedMessage());
    }

    private static void OnSessionFailed(ARSessionFailedArgs args)
    {
      _EasyConnection.Send(new ARSessionFailedMessage { Error = args.Error });
    }
#endregion

#region EditorRequests
    private void HandleRunMessage(ARSessionRunMessage message)
    {
      IARConfiguration configuration;

      if (message.arConfiguration != null)
        configuration = message.arConfiguration;
      else
        throw new Exception("No valid configuration passed to PlayerARSession");

      _session.Run(configuration, message.runOptions);
    }

    private void HandlePauseMessage(ARSessionPauseMessage message)
    {
      _session.Pause();
    }

    private void HandleAddAnchorMessage(ARSessionAddAnchorMessage message)
    {
      var deviceAnchor = _session.AddAnchor(message.Anchor.Transform);
      _addedAnchors.Add(deviceAnchor.Identifier, deviceAnchor);

      _EasyConnection.Send
      (
        new ARSessionAddedCustomAnchorMessage
        {
          Anchor = deviceAnchor._AsSerializable(),
          EditorIdentifier = message.Anchor.Identifier
        }
      );
    }

    private void HandleRemoveAnchorMessage(ARSessionRemoveAnchorMessage message)
    {
      if (_addedAnchors.TryGetValue(message.DeviceAnchorIdentifier, out IARAnchor anchor))
      {
        _session.RemoveAnchor(anchor);
      }
      else
      {
        ARLog._ErrorFormat
        (
          "No anchor with identifier {0} was found to remove.",
          message.DeviceAnchorIdentifier
        );
      }
    }

    private void HandleSetWorldScaleMessage(ARSessionSetWorldScaleMessage message)
    {
      _session.WorldScale = message.WorldScale;
    }

    private void HandleDestroyMessage(ARSessionDestroyMessage message)
    {
      Dispose();
    }
#endregion
  }
}
