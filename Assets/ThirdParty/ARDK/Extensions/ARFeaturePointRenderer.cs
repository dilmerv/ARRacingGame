// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Utilities.Extensions;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  //! A helper script that visualizes feature points
  /// <summary>
  /// A sample class that can be added to a scene to visual feature points found in each frame.
  /// On each frame, the list of current feature points will be queried, and the first
  ///   'MaxFeaturePoints' amount will be instantiated into the scene.
  /// </summary>
  public class ARFeaturePointRenderer: 
    MonoBehaviour
  {
    /// The object used to represent the feature points.
    public GameObject FeaturePointsObjectPf;

    public int MaxFeaturePoints;

    private Dictionary<int, GameObject> _existingPoints;

    private IARSession _session;

    private void Start()
    {
      _existingPoints = new Dictionary<int, GameObject>(MaxFeaturePoints);

      ARSessionFactory.SessionInitialized += _SessionInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= _SessionInitialized;

      var oldSession = _session;
      if (oldSession != null)
        oldSession.FrameUpdated -= _FrameUpdated;

      ClearObjects();
    }

    private void ClearObjects()
    {
      foreach (var point in _existingPoints)
        Destroy(point.Value);

      _existingPoints.Clear();
    }

    private void _SessionInitialized(AnyARSessionInitializedArgs args)
    {
      var oldSession = _session;
      if (oldSession != null)
        oldSession.FrameUpdated -= _FrameUpdated;

      var newSession = args.Session;
      _session = newSession;
      newSession.FrameUpdated += _FrameUpdated;
      newSession.Deinitialized += _OnDeinitialized;
    }

    private void _OnDeinitialized(ARSessionDeinitializedArgs args)
    {
      ClearObjects();
    }

    private void _FrameUpdated(FrameUpdatedArgs args)
    {
      var frame = args.Frame;
      if (frame.RawFeaturePoints == null)
        return;

      var points = frame.RawFeaturePoints.Points;

      var i = 0;
      for (; i < points.Count && i < MaxFeaturePoints; i++)
      {
        var fp = GetFeaturePoint(i);
        fp.transform.position = points[i];
        fp.SetActive(true);
      }

      for (; i < MaxFeaturePoints; i++)
      {
        var fp = _existingPoints.GetOrDefault(i);

        if (fp)
          fp.SetActive(false);
      }
    }

    private GameObject GetFeaturePoint(int index)
    {
      return
        _existingPoints.GetOrInsert
        (
          index,
          () => Instantiate
          (
            FeaturePointsObjectPf,
            Vector3.zero,
            Quaternion.identity,
            null
          )
        );
    }
  }
}
