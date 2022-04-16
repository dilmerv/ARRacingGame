// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;

using UnityEngine;

namespace Niantic.ARDKExamples.PongHLAPI
{
  [RequireComponent(typeof(AuthBehaviour))]
  public class PlayingFieldBehaviour: NetworkedBehaviour
  {
    protected override void SetupSession
    (
      out Action initializer,
      out int order
    )
    {
      initializer = () =>
      {
        var auth = GetComponent<AuthBehaviour>();

        new UnreliableBroadcastTransformPacker
        (
          "netTransform",
          transform,
          auth.AuthorityToObserverDescriptor(TransportType.UnreliableUnordered),
          TransformPiece.Position,
          Owner.Group
        );
      };

      order = 0;
    }
  }
}
