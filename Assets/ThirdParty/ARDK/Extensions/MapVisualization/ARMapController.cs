// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Extensions.MapVisualization {
  /// Controller for map visualization prefab for AR localization
  public class ARMapController : MonoBehaviour, IMapVisualizationController {
    private MeshRenderer _meshRenderer = null;
    private Color _color;
    private bool _visibility = true;

    /// <inheritdoc />
    public void VisualizeMap(ARDK.AR.SLAM.IARMap map) {
      if (_meshRenderer == null) {
        _meshRenderer = GetComponent<MeshRenderer>();
        _color = Random.ColorHSV(0.5f, 1f, 0.5f, 1f, 0.5f, 1f, 1f, 1f);
      }

      transform.position = map.Transform.ToPosition();
      transform.rotation = map.Transform.ToRotation();
      transform.localScale = new Vector3(0.05f,0.05f,0.05f);
      _meshRenderer.material.color = _color;
    }

    /// <inheritdoc />
    public void SetVisibility(bool visibility) {
      if (_visibility == visibility) {
        // Visibility did not change, do nothing
        return;
      }

      _visibility = visibility;
      transform.gameObject.SetActive(_visibility);
    }
  }
}