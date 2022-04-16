// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// Attach this component to a GameObject in a mock environment scene and adjust the `center` and
  /// `rotation` values so the Gizmo lies flat on top of the plane. A mock `IARPlaneAnchor` will
  /// become discovered and raised through the `IARSession.AnchorsAdded` event after
  /// '_timeToDiscovery' seconds have passed.
  /// @note
  ///   Mock plane anchors are one-sided, so make sure to configure the `rotation` value so the
  ///   plane's normal vector (indicated by the arrow) points in the correct direction.
  public sealed class MockPlaneAnchor:
    MockAnchorBase
  {
    [SerializeField]
    private Vector3 _center = Vector3.zero;

    [Tooltip("All values should be increments of 90")]
    [SerializeField]
    private Vector3 _rotation = Vector3.zero;

    [SerializeField]
    private PlaneAlignment _planeAlignment = default(PlaneAlignment);

    [Header("Plane Classification Options")]
    [SerializeField]
    private bool _shouldSuccessfullyClassify = true;

    [SerializeField]
    private PlaneClassification _planeClassification = default(PlaneClassification);

    // Time (in seconds) it takes for this anchor's PlaneClassificationStatus to settle after
    // it is discovered.
    [SerializeField]
    private float _timeToClassify = 1f;

    private _SerializableARPlaneAnchor _anchorData;

    private Vector3 _localScale = new Vector3(1, 0.001f, 1);

    protected override bool Initialize()
    {
      if (_planeAlignment == PlaneAlignment.Unknown)
      {
        ARLog._Error("MockPlaneAnchors with Unknown plane alignments will not be discovered.");
        return false;
      }

      return true;
    }

    protected override IARAnchor AnchorData
    {
      get => _anchorData;
    }

    protected override bool UpdateAnchorData()
    {
      // Note: transform.hasChanged is susceptible to other logic changing this flag's value
      // but for the time being, this is also the most straightforward approach for detecting
      // changes to this GameObject's transform component
      if (_anchorData == null || !transform.hasChanged)
        return false;

      var localTransform =
        Matrix4x4.TRS
        (
          _center,
          Quaternion.Euler(_rotation).normalized,
          _localScale
        );

      var worldTransform = transform.localToWorldMatrix * localTransform;

      _anchorData.Transform =
        Matrix4x4.TRS
        (
          worldTransform.ToPosition(),
          worldTransform.ToRotation(),
          Vector3.one
        );

      _anchorData.Extent =
        new Vector3
        (
          worldTransform.lossyScale.x,
          0,
          worldTransform.lossyScale.z
        );

      transform.hasChanged = false;
      return true;
    }

    internal override void CreateAndAddAnchorToSession(_IMockARSession arSession)
    {
      if (_anchorData == null)
      {
        // Initialize the anchor data with initial values such as
        // a new guid, non-transform related values, etc
        _anchorData =
          new _SerializableARPlaneAnchor
          (
            new Matrix4x4(),
            Guid.NewGuid(),
            _planeAlignment,
            PlaneClassification.None,
            PlaneClassificationStatus.Undetermined,
            Vector3.zero,
            Vector3.zero
          );

        // Transform and Extent values will be set here
        UpdateAnchorData();

        // Value starts off as true, so needs to be set to false here
        transform.hasChanged = false;
      }

      if (arSession.AddAnchor(_anchorData))
        StartCoroutine(ClassifyPlane(arSession));
      else
      {
        ARLog._DebugFormat
        (
          "Plane anchor for {0} cannot be detected. If that is unintended, check" +
          "that the active ARWorldTrackingConfiguration's PlaneDetection value is correct."
        );

        enabled = false;
      }
    }

    internal override void RemoveAnchorFromSession(_IMockARSession arSession)
    {
      arSession.RemoveAnchor(_anchorData);
    }

    private IEnumerator ClassifyPlane(_IMockARSession arSession)
    {
      yield return new WaitForSeconds(_timeToClassify);

      _anchorData.Classification =
        _shouldSuccessfullyClassify
          ? _planeClassification
          : PlaneClassification.None;

      _anchorData.ClassificationStatus =
        _shouldSuccessfullyClassify
          ? PlaneClassificationStatus.Known
          : PlaneClassificationStatus.Unknown;

      arSession.UpdateAnchor(_anchorData);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.matrix = transform.localToWorldMatrix;

      var orientation = Quaternion.Euler(_rotation);
      Gizmos.DrawWireCube(_center, orientation * _localScale);

      var worldCenter = transform.position;
      Handles.ArrowHandleCap
      (
        0,
        worldCenter,
        transform.rotation * orientation * Quaternion.LookRotation(Vector3.up),
        .25f,
        EventType.Repaint
      );
    }
#endif
  }
}
