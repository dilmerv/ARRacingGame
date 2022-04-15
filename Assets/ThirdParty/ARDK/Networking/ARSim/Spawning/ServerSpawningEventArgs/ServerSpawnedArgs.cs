// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Networking.ARSim.Spawning.ServerSpawningEventArgs
{
  public struct ServerSpawnedArgs :
    IArdkEventArgs
  {
    public ServerSpawnedArgs(string objectId, GameObject gameObject):
      this()
    {
      ObjectId = objectId;
      GameObject = gameObject;
    }
    
    public string ObjectId { get; private set; }
    public GameObject GameObject { get; private set; }
  }
}
