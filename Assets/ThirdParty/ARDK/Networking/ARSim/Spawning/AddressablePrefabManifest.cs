// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.Utilities.Collections;

using UnityEngine;

namespace Niantic.ARDK.Networking.ARSim.Spawning
{
  public sealed class AddressablePrefabManifest : IAddressablePrefabManifest
  {
    public IReadOnlyDictionary<string, GameObject> PrefabManifest { get; private set; }
    private readonly Dictionary<string, GameObject> _prefabManifest;

    public AddressablePrefabManifest()
    {
      _prefabManifest = new Dictionary<string, GameObject>();
      PrefabManifest = new _ReadOnlyDictionary<string, GameObject>(_prefabManifest);
    }

    public bool RegisterPrefab(string identifier, GameObject prefab)
    {
      if (_prefabManifest.ContainsKey(identifier))
      {
        Debug.LogWarning("PrefabManifest already contains key: " + identifier);
        return false;
      }

      _prefabManifest[identifier] = prefab;
      return true;
    }
  }
}
