// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.AR.SLAM;

namespace Niantic.ARDK.VirtualStudio.AR
{
  internal interface _IMockARSession:
    IMockARSession, 
    _IARSession
  {
    bool CheckMapsUnion(IARSession otherSession);

    bool AddAnchor(_SerializableARAnchor anchor);

    void UpdateMesh(_IARMeshData meshData);

    // Event raised when mock ARMaps are added to the session (ie by MockMap) but the
    // local peer is not the host. A "local map" isn't a valid concept in _NativeARSession,
    // but it's needed here to support mock localizing.
    //
    // Note: This event doesn't need to follow the new pattern for events to avoid breaking
    // changes because it is an internal event.
    event Action<IARMap[]> ImplDidAddLocalMaps;
  }
}
