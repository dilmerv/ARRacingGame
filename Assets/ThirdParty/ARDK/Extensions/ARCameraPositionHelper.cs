// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.Utilities;

using UnityEngine;

/// @namespace Niantic.ARDK.Extensions
/// @brief Premade helper scripts that can be easily added to a Unity project and add
///   AR/multipeer functionality
namespace Niantic.ARDK.Extensions
{
  /// <summary>
  /// A helper class to automatically position an AR camera and transform its output
  /// </summary>
  public class ARCameraPositionHelper: MonoBehaviour
  {
    /// The Unity Camera in the scene doing the rendering.
    public Camera Camera;

    private ARCameraFeed _cameraFeed;

    private IARSession _session;

    private void Start()
    {
      ARSessionFactory.SessionInitialized += _OnSessionInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= _OnSessionInitialized;

      var session = _session;
      if (session != null)
        session.FrameUpdated -= _FrameUpdated;
    }

    private void _OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      var oldSession = _session;
      if (oldSession != null)
        oldSession.FrameUpdated -= _FrameUpdated;

      var newSession = args.Session;
      _session = newSession;
      newSession.FrameUpdated += _FrameUpdated;
    }

    private void _FrameUpdated(FrameUpdatedArgs args)
    {
      var localCamera = Camera;
      if (localCamera == null)
        return;

      var session = _session;
      if (session == null)
        return;

      // Set the camera's position.
      var worldTransform = args.Frame.Camera.GetViewMatrix(Screen.orientation).inverse;
      localCamera.transform.position = worldTransform.ToPosition();
      localCamera.transform.rotation = worldTransform.ToRotation();
    }
  }
}
