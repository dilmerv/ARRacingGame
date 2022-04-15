// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking.HLAPI.Data;

using UnityEngine;

// TODO: comment

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity
{
  [RequireComponent(typeof(AuthBehaviour))]
  public sealed class NetTransform :
    NetworkedBehaviour
  {
    [SerializeField]
    private TransformPiece _replicatedPieces = TransformPiece.All;

    private UnreliableBroadcastTransformPacker _transformPacker;

    protected override void SetupSession(out Action initializer, out int order)
    {
      initializer = () =>
      {
        var descriptor = Owner.Auth.AuthorityToObserverDescriptor(TransportType.UnreliableUnordered);
        _transformPacker = new UnreliableBroadcastTransformPacker
        (
          "NetTransform",
          gameObject.transform,
          descriptor,
          _replicatedPieces,
          Owner.Group
        );
      };

      order = 0;
    }

    private void OnDestroy()
    {
      _transformPacker?.Unregister();
    }
  }
}
