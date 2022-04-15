// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.Image;
using Niantic.ARDK.AR.LightEstimate;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Niantic.ARDK.AR.Frame
{
  [Serializable]
  internal sealed class _SerializableARFrame:
    _SerializableARFrameBase
  {
    internal _SerializableARFrame()
    {
    }

    internal _SerializableARFrame
    (
      _SerializableImageBuffer capturedImageBuffer,
      _SerializableDepthBuffer depthBuffer,
      _SerializableSemanticBuffer semanticBuffer,
      _SerializableARCamera camera,
      _SerializableARLightEstimate lightEstimate,
      ReadOnlyCollection<IARAnchor> anchors, // Even native ARAnchors are directly serializable.
      _SerializableARMap[] maps,
      float worldScale,
      Matrix4x4 estimatedDisplayTransform
    ):
      base
      (
        capturedImageBuffer,
        depthBuffer,
        semanticBuffer,
        camera,
        lightEstimate,
        anchors,
        maps,
        worldScale,
        estimatedDisplayTransform
      )
    {
    }

    public override ReadOnlyCollection<IARHitTestResult> HitTest
    (
      int viewportWidth,
      int viewportHeight,
      Vector2 screenPoint,
      ARHitTestResultType types
    )
    {
      if ((types & ~ARHitTestResultType.ExistingPlaneUsingExtent) != 0)
      {
        var message =
          "Only hit tests for ARHitTestResultType.ExistingPlaneUsingExtent " +
          "is supported in Mock or Remote ARSessions.";

        ARLog._Debug(message);
      }

      var anchors = Anchors;
      if (anchors == null)
        return EmptyReadOnlyCollection<IARHitTestResult>.Instance;

      // Simulated hit test
      var hitTestResults = new List<IARHitTestResult>();

      var planeAnchors =
        from anchor in anchors
        where anchor.AnchorType == AnchorType.Plane
        select (IARPlaneAnchor) anchor;

      var camera = UnityEngine.Camera.main;
      if (camera == null)
      {
        string message =
          "ARFrame.HitTest requires a camera tagged " +
          "`MainCamera` in the scene, but one was not found.";

        ARLog._Error(message);
        return hitTestResults.AsReadOnly();
      }

      var ray = camera.ScreenPointToRay(screenPoint);
      var v = new Vector3[4];

      foreach (var plane in planeAnchors)
      {
        var worldCenter = plane.Transform.ToPosition() + plane.Center;
        var orientation = plane.Transform.ToRotation();
        GetWorldVertices(worldCenter, plane.Extent, orientation, ref v);

        // Calculate intersection point of ray and plane.
        // Unity uses the left hand rule.
        var normal = Vector3.Cross(v[3] - v[0], v[1] - v[0]);
        if (!PlaneRaycast(ray, worldCenter, normal, out Vector3 hitPosition, out float distance))
          continue;

        // Inside-outside test.
        // There's a few vector subtractions duplicated by pulling these calculations into methods,
        // but there would/should not be 100s of planes in a scene so cleaner code is prioritized.
        if
        (
          !IsInsideTriangle(normal, hitPosition, v[0], v[1], v[3]) &&
          !IsInsideTriangle(normal, hitPosition, v[1], v[2], v[3])
        )
          continue;

        Debug.DrawRay(worldCenter, normal * 2f, Color.blue, 10f);
        Debug.DrawLine(ray.origin, hitPosition, Color.green, 10f);

        var hitTestResult =
          new _SerializableARHitTestResult
          (
            ARHitTestResultType.ExistingPlaneUsingExtent,
            plane._AsSerializablePlane(),
            distance,
            Matrix4x4.identity,
            Matrix4x4.TRS(hitPosition, Quaternion.identity, Vector3.one),
            1.0f
          );

        hitTestResults.Add(hitTestResult);
      }

      hitTestResults.Sort((x, y) => x.Distance.CompareTo(y.Distance));
      return hitTestResults.AsReadOnly();
    }

    private static void GetWorldVertices
    (
      Vector3 worldCenter,
      Vector3 extent,
      Quaternion orientation,
      ref Vector3[] vertices
    )
    {
      var halfWidth = extent.x / 2;
      var halfHeight = extent.z / 2;

      vertices[0] = worldCenter + orientation * new Vector3(-halfWidth, 0, halfHeight);
      vertices[1] = worldCenter + orientation * new Vector3(-halfWidth, 0, -halfHeight);
      vertices[2] = worldCenter + orientation * new Vector3(halfWidth, 0, -halfHeight);
      vertices[3] = worldCenter + orientation * new Vector3(halfWidth, 0, halfHeight);
    }

    private static bool IsInsideTriangle
    (
      Vector3 normal,
      Vector3 point,
      Vector3 v0,
      Vector3 v1,
      Vector3 v2
    )
    {
      return
        (Vector3.Dot(normal, Vector3.Cross(point - v0, v1 - v0)) >= 0) &&
        (Vector3.Dot(normal, Vector3.Cross(point - v1, v2 - v1)) >= 0) &&
        (Vector3.Dot(normal, Vector3.Cross(point - v2, v0 - v2)) >= 0);
    }

    private static bool PlaneRaycast
    (
      Ray ray,
      Vector3 center,
      Vector3 normal,
      out Vector3 hit,
      out float distance
    )
    {
      var denom = Vector3.Dot(ray.direction, normal);
      if (Mathf.Abs(denom) > 0.0001f)
      {
        var diff = center - ray.origin;
        float t = Vector3.Dot(diff, normal) / denom;
        if (t > 0.0001f)
        {
          hit = ray.origin + (t * ray.direction);
          distance = t;
          return true;
        }
      }

      hit = Vector3.positiveInfinity;
      distance = float.PositiveInfinity;
      return false;
    }
  }
}
