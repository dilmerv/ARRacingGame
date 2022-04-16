// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARDK.Extensions {
  [ExecuteInEditMode]
  public class PlanefindingGrid : MonoBehaviour {

    [SerializeField]
    private Renderer _renderer = null;

    [SerializeField]
    private MaterialPropertyBlock propBlock = null;

    [SerializeField]
    private float textureScale = 0.0f;

    private void Awake() {
      propBlock = new MaterialPropertyBlock();

      // Get the current value of the material properties in the renderer.
      _renderer.GetPropertyBlock(propBlock);
    }

    private void Update() {
      if (transform.hasChanged) {
        // Assign our new value.
        var targetVector = new Vector4(
          transform.localScale.x,
          transform.localScale.z,
          -transform.localScale.x * 0.5f,
          -transform.localScale.z * 0.5f);

        propBlock.SetVector("_MainTex_ST", targetVector * textureScale);
        _renderer.SetPropertyBlock(propBlock);

        transform.hasChanged = false;
      }
    }
  }
}
