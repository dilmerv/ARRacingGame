// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

// TODO: comment

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity {
  public class NetworkSceneSpawnManifest : MonoBehaviour {
    [SerializeField] private PrefabManifest _manifest = null;

    private void Awake() {
      NetworkSpawner.LoadManifest(_manifest);
    }

    private void OnDestroy() {
      NetworkSpawner.UnloadManifest(_manifest);
    }
  }
}