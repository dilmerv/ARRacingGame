// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

// TODO: comment

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity {
  [Serializable]
  [CreateAssetMenu(menuName ="Networking/Spawning/PrefabManifest")]
  public class PrefabManifest : ScriptableObject {
    public List<NetworkedUnityObject> Prefabs = new List<NetworkedUnityObject>();
  }
}