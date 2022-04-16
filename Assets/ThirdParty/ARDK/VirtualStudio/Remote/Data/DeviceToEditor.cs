// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;


using UnityEngine;

// TODO: comment

namespace Niantic.ARDK.VirtualStudio.Remote.Data
{
#region AR
  [Serializable]
  internal sealed class ARSessionFrameUpdatedMessage
  {
    public _SerializableARFrame Frame;
  }

  [Serializable]
  internal sealed class ARSessionMeshUpdatedMessage
  {
    public _SerializableARMeshData MeshData;
  }

  [Serializable]
  internal sealed class ARSessionAddedCustomAnchorMessage
  {
    public _SerializableARAnchor Anchor;
    public Guid EditorIdentifier;
  }

  [Serializable]
  internal sealed class ARSessionAnchorsAddedMessage
  {
    public _SerializableARAnchor[] Anchors;
    public _SerializableARPlaneAnchor[] PlaneAnchors;
    public _SerializableARImageAnchor[] ImageAnchors;
  }

  [Serializable]
  internal sealed class ARSessionAnchorsUpdatedMessage
  {
    public _SerializableARAnchor[] Anchors;
    public _SerializableARPlaneAnchor[] PlaneAnchors;
    public _SerializableARImageAnchor[] ImageAnchors;
  }

  [Serializable]
  internal sealed class ARSessionAnchorsRemovedMessage
  {
    public _SerializableARAnchor[] Anchors;
    public _SerializableARPlaneAnchor[] PlaneAnchors;
    public _SerializableARImageAnchor[] ImageAnchors;
  }

  [Serializable]
  internal sealed class ARSessionAnchorsMergedMessage
  {
    public _SerializableARPlaneAnchor ParentAnchor;
    public _SerializableARPlaneAnchor[] ChildAnchors;
  }

  [Serializable]
  internal sealed class ARSessionMapsAddedMessage
  {
    public ARDK.AR.SLAM._SerializableARMap[] Maps;
  }

  [Serializable]
  internal sealed class ARSessionMapsUpdatedMessage
  {
    public ARDK.AR.SLAM._SerializableARMap[] Maps;
  }
  
  [Serializable]
  internal sealed class ARSessionLocalizationUpdatedMessage
  {
    // TODO
  }

  [Serializable]
  internal sealed class ARSessionCameraTrackingStateChangedMessage
  {
    internal _SerializableARCamera Camera;
  }

  [Serializable]
  internal sealed class ARSessionWasInterruptedMessage
  {
  }

  [Serializable]
  internal sealed class ARSessionInterruptionEndedMessage
  {
  }

  [Serializable]
  internal sealed class ARSessionFailedMessage
  {
    public ARError Error;
  }
#endregion

#region Networking
  [Serializable]
  internal sealed class NetworkingConnectedMessage
  {
    public static readonly Guid ID = new Guid("4dae7981-fb53-499b-8e36-45072d00bcb5");

    public Guid SelfIdentifier;
    public Guid HostIdentifier;
  }

  [Serializable]
  internal sealed class NetworkingConnectionFailedWithErrorMessage
  {
    public static readonly Guid ID = new Guid("8abf4e4f-041d-4cea-b898-8018aedada87");

    public uint ErrorCode;
  }

  [Serializable]
  internal sealed class NetworkingDisconenctedMessage
  {
    public static readonly Guid ID = new Guid("8437924b-728e-40b7-bc04-f16628716a53");
  }

  [Serializable]
  internal sealed class NetworkingPeerDataReceivedMessage
  {
    public static readonly Guid ID = new Guid("c91b6ce0-3922-4ac7-a269-0c60e974e4db");

    public uint Tag;
    public byte[] Data;
    public Guid PeerIdentifier;
    public byte TransportType;
  }

  [Serializable]
  internal sealed class NetworkingPeerAddedMessage
  {
    public static readonly Guid ID = new Guid("e2abdcb8-0a74-4081-a716-75fec0e55e9c");

    public Guid PeerIdentifier;
  }

  [Serializable]
  internal sealed class NetworkingPeerRemovedMessage
  {
    public static readonly Guid ID = new Guid("34b6a435-f9ed-420a-b2e3-6e866a52efe6");

    public Guid PeerIdentifier;
  }

  [Serializable]
  internal sealed class NetworkingPersistentKeyValueUpdatedMessage
  {
    public static readonly Guid ID = new Guid("34b6a435-f9ed-420a-b2e3-6e866a52ffe6");

    public byte[] Key;
    public byte[] Value;
  }

  [Serializable]
  internal sealed class NetworkingDeinitializedMessage
  {
    public static readonly Guid ID = new Guid("c0e3f4b5-da75-498b-93ba-9139ec3eaa20");
  }

  [Serializable]
  internal sealed class NetworkingDataReceivedFromArmMessage
  {
    public static readonly Guid ID = new Guid("c91b6ce0-3922-4ac7-a269-0c60e974e4dc");

    public uint Tag;
    public byte[] Data;
  }

  [Serializable]
  internal sealed class NetworkingStatusReceivedFromArmMessage
  {
    public static readonly Guid ID = new Guid("c91b6ce0-3922-4ac7-a269-0c60e974e4dd");

    public uint Status;
  }

  [Serializable]
  internal sealed class NetworkingResultReceivedFromArmMessage
  {
    public static readonly Guid ID = new Guid("c91b6ce0-3922-4ac7-a269-0c60e974e4de");

    public uint Outcome;
    public byte[] Details;
  }

#endregion

#region ARNetworking
  [Serializable]
  internal sealed class ARNetworkingPeerStateReceivedMessage
  {
    public static readonly Guid ID = new Guid("464cc2a0-8b0d-483b-abf7-94e0c3809458");

    public PeerState PeerState;
    public Guid PeerIdentifier;
  }

  [Serializable]
  internal sealed class ARNetworkingPeerPoseReceivedMessage
  {
    public static readonly Guid ID = new Guid("464cc2a0-8b0d-483b-abf7-95e0c3809458");

    public Matrix4x4 Pose;
    public Guid PeerIdentifier;
  }

  [Serializable]
  internal sealed class ARNetworkingDeinitializedMessage
  {
    public static readonly Guid ID = new Guid("c0e3f4b5-da75-498b-93ba-9139ec3eaa29");
  }
#endregion
}
