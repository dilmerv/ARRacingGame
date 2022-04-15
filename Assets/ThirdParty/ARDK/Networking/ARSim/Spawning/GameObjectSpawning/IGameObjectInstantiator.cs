// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Networking.ARSim.Spawning.GameObjectSpawning
{
  /// <summary>
  /// An interface wrapping Unity's Instantiate and Destroy methods, to support alternative spawning
  ///   patterns (Zenject, object pools, etc).
  /// </summary>
  public interface IGameObjectInstantiator
  {
    GameObject Instantiate(GameObject original);

    GameObject Instantiate
    (
      GameObject original,
      Transform parent,
      bool instantiateInWorldSpace = false
    );

    GameObject Instantiate
    (
      GameObject original,
      Vector3 position,
      Quaternion rotation,
      Transform parent = null
    );

    void Destroy(GameObject obj, float timeToDelay = 0.0f);
  }
}
