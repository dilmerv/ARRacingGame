using System.Collections.Generic;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Mesh
{
  public class MeshBlocksUpdatedArgs:
    IArdkEventArgs
  {
    internal MeshBlocksUpdatedArgs
    (
      IReadOnlyCollection<Vector3Int> blocksUpdated,
      IReadOnlyCollection<Vector3Int> blocksObsoleted,
      IARMesh mesh
    )
    {
      BlocksUpdated = blocksUpdated;
      BlocksObsoleted = blocksObsoleted;
      Mesh = mesh;
    }

    public IReadOnlyCollection<Vector3Int> BlocksUpdated { get; private set; }
    public IReadOnlyCollection<Vector3Int> BlocksObsoleted { get; private set; }

    public IARMesh Mesh { get; private set; }
  }
}
