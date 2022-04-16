// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDKExamples.Common.Helpers;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  /// A simple AR-only demo to show that you don't necessarily always need to detect planes!
  public class AnchorsWithoutPlanesExampleManager:
    MonoBehaviour
  {
    public Camera Camera;
    public GameObject PrefabToPlace;
    public Text AnchorDisplayText;

    private IARSession _session = null;
    private Dictionary<Guid, IARAnchor> _addedAnchors = new Dictionary<Guid, IARAnchor>();
    private Dictionary<Guid, GameObject> _placedObjects = new Dictionary<Guid, GameObject>();

    private void Awake()
    {
      // Listen for the ARSession that is created/run by the ARSessionManager component in the scene.
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
    }

    private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      _session = args.Session;
      _session.AnchorsAdded += OnAnchorsAdded;
      _session.AnchorsRemoved += OnAnchorsRemoved;
      _session.Deinitialized += _ => _session = null;
    }

    private void Update()
    {
      if (_session == null)
        return;

      // Get the current frame
      var currentFrame = _session.CurrentFrame;
      if (currentFrame == null)
        return;

      // Display the number of anchors
      AnchorDisplayText.text = "Anchors: " + currentFrame.Anchors.Count;

      // Return if we don't have a touch at the beginning
      if (PlatformAgnosticInput.touchCount <= 0)
        return;

      var touch = PlatformAgnosticInput.GetTouch(0);
      if (touch.phase != TouchPhase.Began)
        return;

      // Hit test against the placed objects, and remove if a placed object was tapped.
      // Else, place an object at the tap location.
      var worldRay = Camera.ScreenPointToRay(touch.position);
      RaycastHit hit;

      if (Physics.Raycast(worldRay, out hit, 1000f))
      {
        var anchorAttachment = hit.transform.gameObject.GetComponent<ARAnchorAttachment>();
        if (anchorAttachment != null)
        {
          var anchor = anchorAttachment.AttachedAnchor;

          if (_addedAnchors.ContainsKey(anchor.Identifier))
            _session.RemoveAnchor(anchor);

          return;
        }
      }

      HitTestToPlaceAnchor(currentFrame, touch);
    }

    private void HitTestToPlaceAnchor(IARFrame frame, Touch touch)
    {
#if UNITY_EDITOR
      // Hit tests against EstimatedHorizontalPlanes don't work in Virtual Studio Remote/Mock,
      // so just place the cube under mouse click
      var position = Camera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 1f));
#else
      // Do a hit test against estimated planes (ie against the real world environment, not against
      // Unity colliders like in Update
      var results =
        frame.HitTest
        (
          Camera.pixelWidth,
          Camera.pixelHeight,
          touch.position,
          ARHitTestResultType.EstimatedHorizontalPlane
        );

      Debug.Log("Hit test results: " + results.Count);

      if (results.Count <= 0)
        return;

      // Get the closest result
      var result = results[0];

      // Create a new anchor and add it to our list and the session
      var position = result.WorldTransform.ToPosition();
#endif

      var anchor = _session.AddAnchor(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one));
      _addedAnchors.Add(anchor.Identifier, anchor);

      Debug.LogFormat("Created anchor (id: {0}, position: {1} ", anchor.Identifier, position.ToString("F4"));
    }

    private void OnDestroy()
    {
      // OnDestroy being called means the scene was unloaded. So ARSessionManager will
      // dispose the session and we don't have to do any session related cleanup.

      _addedAnchors.Clear();

      ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
    }

    private void OnAnchorsAdded(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (!_addedAnchors.ContainsKey(anchor.Identifier))
        {
          // Plane and image detection are both disabled in this scene, so the only anchors getting
          // surfaced through this callback are the anchors added in HitTestToPlaceAnchor.
          Debug.LogWarningFormat
          (
            "Found anchor (id: {0}) not added by this class. This should not happen.",
            anchor.Identifier
          );

          continue;
        }

        // Create the cube object and add a component that will keep it attached to the new anchor.
        var cube =
          Instantiate
          (
            PrefabToPlace,
            new Vector3(0, 0, 0),
            Quaternion.identity
          );

        var attachment = cube.AddComponent<ARAnchorAttachment>();
        attachment.AttachedAnchor = anchor;
        var cubeYOffset = PrefabToPlace.transform.localScale.y / 2;
        attachment.Offset = Matrix4x4.Translate(new Vector3(0, cubeYOffset, 0));

        // Keep track of the anchor objects
        _placedObjects.Add(anchor.Identifier, cube);
      }
    }

    private void OnAnchorsRemoved(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (_addedAnchors.ContainsKey(anchor.Identifier))
        {
          _addedAnchors.Remove(anchor.Identifier);

          Destroy(_placedObjects[anchor.Identifier]);
          _placedObjects.Remove(anchor.Identifier);
        }
      }
    }

    public void ClearAnchors()
    {
      if (_session == null)
        return;

      // Clear out anchors. The OnAnchorsRemoved method should get invoked and handle clearing
      // the placed objects.
      foreach (var anchor in _addedAnchors)
        _session.RemoveAnchor(anchor.Value);
    }
  }
}
