// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using UnityEngine;

namespace Niantic.ARDK.Networking.ARSim.Spawning
{
  /// <summary>
  /// Contains a dictionary mapping string identifiers to prefabs, so that prefabs can be dynamically
  ///   registered and spawned.
  /// @note Currently in internal development, and not useable
  /// </summary>
  public interface IAddressablePrefabManifest
  {
    IReadOnlyDictionary<string, GameObject> PrefabManifest { get; }
    bool RegisterPrefab(string identifier, GameObject prefab);
  }
}
