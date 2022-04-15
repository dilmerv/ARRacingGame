// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDKExamples.Helpers
{
  //! Helper script that spawns a cursor on a plane if it finds one
  /// <summary>
  /// A sample class that can be added to a scene to demonstrate basic plane finding and hit
  ///   testing usage. On each updated frame, a hit test will be applied from the middle of the
  ///   screen and spawn a cursor if it finds a plane.
  /// </summary>
  public class ARCursorRenderer:
    MonoBehaviour
  {
    /// The camera used to render the scene. Used to get the center of the screen.
    public Camera Camera;

    /// The object we will place to represent the cursor!
    public GameObject CursorObject;

    /// A reference to the spawned cursor in the center of the screen.
    private GameObject _spawnedCursorObject;

    private IARSession _session;

    private void Start()
    {
      ARSessionFactory.SessionInitialized += _SessionInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= _SessionInitialized;

      var session = _session;
      if (session != null)
        session.FrameUpdated -= _FrameUpdated;

      DestroySpawnedCursor();
    }

    private void DestroySpawnedCursor()
    {
      if (_spawnedCursorObject == null)
        return;

      Destroy(_spawnedCursorObject);
      _spawnedCursorObject = null;
    }

    private void _SessionInitialized(AnyARSessionInitializedArgs args)
    {
      var oldSession = _session;
      if (oldSession != null)
        oldSession.FrameUpdated -= _FrameUpdated;

      var newSession = args.Session;
      _session = newSession;
      newSession.FrameUpdated += _FrameUpdated;
      newSession.Deinitialized += _OnSessionDeinitialized;
    }

    private void _OnSessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      DestroySpawnedCursor();
    }

    private void _FrameUpdated(FrameUpdatedArgs args)
    {
      var camera = Camera;
      if (camera == null)
        return;

      var viewportWidth = camera.pixelWidth;
      var viewportHeight = camera.pixelHeight;

      // Hit testing for cursor in the middle of the screen
      var middle = new Vector2(viewportWidth / 2f, viewportHeight / 2f);

      var frame = args.Frame;
      // Perform a hit test and either estimate a horizontal plane, or use an existing plane and its
      // extents!
      var hitTestResults =
        frame.HitTest
        (
          viewportWidth,
          viewportHeight,
          middle,
          ARHitTestResultType.ExistingPlaneUsingExtent |
          ARHitTestResultType.EstimatedHorizontalPlane
        );

      if (hitTestResults.Count == 0)
        return;

      if (_spawnedCursorObject == null)
        _spawnedCursorObject = Instantiate(CursorObject, Vector2.one, Quaternion.identity);

      // Set the cursor object to the hit test result's position
      _spawnedCursorObject.transform.position = hitTestResults[0].WorldTransform.ToPosition();

      // Orient the cursor object to look at the user, but remain flat on the "ground", aka
      // only rotate about the y-axis
      _spawnedCursorObject.transform.LookAt
      (
        new Vector3
        (
          frame.Camera.Transform[0, 3],
          _spawnedCursorObject.transform.position.y,
          frame.Camera.Transform[2, 3]
        )
      );
    }
  }
}
