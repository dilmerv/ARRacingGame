// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Meshing
{
  public struct MeshObjectsUpdatedArgs:
    IArdkEventArgs
  {
    public MeshObjectsUpdatedArgs
    (
      IList<GameObject> blocksUpdated,
      IList<GameObject> collidersUpdated
    )
    {
      BlocksUpdated = new ReadOnlyCollection<GameObject>(blocksUpdated);
      CollidersUpdated = new ReadOnlyCollection<GameObject>(collidersUpdated);
    }

    public ReadOnlyCollection<GameObject> BlocksUpdated { get; }
    public ReadOnlyCollection<GameObject> CollidersUpdated { get; }

  }
}