// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Networking.ARSim.Spawning.GameObjectSpawning
{
  /// <summary>
  /// Implementation of an IGameObjectInstantiator that uses basic UnityEngine calls.
  /// </summary>
  public sealed class UnityGameObjectInstantiator : 
    IGameObjectInstantiator
  {
    /// <inheritdoc />
    public GameObject Instantiate(GameObject original)
    {
      return UnityEngine.Object.Instantiate(original);
    }

    /// <inheritdoc />
    public GameObject Instantiate
    (
      GameObject original,
      Transform parent,
      bool instantiateInWorldSpace = false
    )
    {
      return UnityEngine.Object.Instantiate(original, parent, instantiateInWorldSpace);
    }

    /// <inheritdoc />
    public GameObject Instantiate
    (
      GameObject original,
      Vector3 position,
      Quaternion rotation,
      Transform parent = null
    )
    {
      return parent != null ? 
        UnityEngine.Object.Instantiate(original, position, rotation, parent) : 
        UnityEngine.Object.Instantiate(original, position, rotation);
    }

    /// <inheritdoc />
    public void Destroy(GameObject obj, float timeToDelay = 0.0f)
    {
      UnityEngine.Object.Destroy(obj, timeToDelay);
    }
  }
}
