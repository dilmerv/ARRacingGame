// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.External;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  /// Instantiates, updates, and removes GameObjects for each detected
  /// [PlaneAnchor](@ref Niantic.ARDK.AR.Anchors.IARPlaneAnchor]. This settings on this class
  /// are the ultimate authority on the used IARWorldTrackingConfiguration.PlaneDetection value.
  public sealed class ARPlaneManager:
    ARSessionListener
  {
    /// The object to spawn and update when a plane is detected.
    [SerializeField]
    private GameObject _planePrefab;

    [SerializeField]
    [EnumFlag]
    private PlaneDetection _detectedPlaneTypes;

    // Used to track when the Inspector-public _detectedPlaneTypes is changed in OnValidate
    private PlaneDetection _prevDetectedPlaneTypes;

    private readonly Dictionary<Guid, GameObject> _planeLookup = new Dictionary<Guid, GameObject>();

    /// If this is null no plane prefabs will be created.
    /// If this changes after planes have already been discovered existing instances won't be
    /// changed.
    public GameObject PlanePrefab
    {
      get { return _planePrefab; }
      set { _planePrefab = value; }
    }

    public PlaneDetection DetectedPlaneTypes
    {
      get
      {
        return _detectedPlaneTypes;
      }
      set
      {
        if (value != _detectedPlaneTypes)
        {
          _detectedPlaneTypes = value;
          RaiseConfigurationChanged();
        }
      }
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      ClearAllPlanes();
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      _prevDetectedPlaneTypes = _detectedPlaneTypes;
      RaiseConfigurationChanged();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      RaiseConfigurationChanged();
    }

    protected override void OnSessionDeinitialized()
    {
      ClearAllPlanes();
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration worldConfig)
        worldConfig.PlaneDetection = AreFeaturesEnabled ? DetectedPlaneTypes : PlaneDetection.None;
    }

    protected override void ListenToSession()
    {
      ARSession.AnchorsAdded += OnAnchorsAdded;
      ARSession.AnchorsUpdated += OnAnchorsUpdated;
      ARSession.AnchorsRemoved += OnAnchorsRemoved;
    }

    protected override void StopListeningToSession()
    {
      ARSession.AnchorsAdded -= OnAnchorsAdded;
      ARSession.AnchorsUpdated -= OnAnchorsUpdated;
      ARSession.AnchorsRemoved -= OnAnchorsRemoved;
    }

    private void OnAnchorsAdded(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (anchor is IARPlaneAnchor planeAnchor)
        {
          CreateAnchorPrefab(anchor as IARPlaneAnchor);
          RefreshAnchor(planeAnchor);
        }
      }
    }

    private void CreateAnchorPrefab(IARPlaneAnchor anchor)
    {
      if (_planePrefab == null)
        return;

      var plane = Instantiate(_planePrefab);
      plane.name = "Plane-" + anchor.Identifier.ToString().Substring(0, 5);
      _planeLookup.Add(anchor.Identifier, plane);
    }

    private void OnAnchorsUpdated(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (anchor is IARPlaneAnchor planeAnchor)
          RefreshAnchor(planeAnchor);
      }
    }

    private void OnAnchorsRemoved(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (anchor.AnchorType != AnchorType.Plane)
          continue;

        Destroy(_planeLookup[anchor.Identifier]);
        _planeLookup.Remove(anchor.Identifier);
      }
    }

    private void RefreshAnchor(IARPlaneAnchor anchor)
    {
      if (_planeLookup.TryGetValue(anchor.Identifier, out GameObject go))
      {
        // ARKit plane anchors will not change transform but will update the center value
        // ARCore plane anchors will update the transform
        go.transform.position = anchor.Transform.ToPosition() + anchor.Center;
        go.transform.rotation = anchor.Transform.ToRotation();
        go.transform.localScale = anchor.Extent;
      }
    }

    private void ClearAllPlanes()
    {
      foreach (var go in _planeLookup.Values)
        Destroy(go);

      _planeLookup.Clear();
    }

    private void OnValidate()
    {
      if (_detectedPlaneTypes != _prevDetectedPlaneTypes)
      {
        _prevDetectedPlaneTypes = _detectedPlaneTypes;
        RaiseConfigurationChanged();
      }
    }
  }
}
