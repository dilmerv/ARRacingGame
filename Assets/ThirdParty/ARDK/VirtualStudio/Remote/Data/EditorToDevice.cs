// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Networking;
using Niantic.ARDK.AR.Configuration;

// TODO: comment

namespace Niantic.ARDK.VirtualStudio.Remote.Data
{
  [Serializable]
  internal sealed class RemoteConnectionDestroyMessage
  {
  }

#region AR
  [Serializable]
  internal sealed class ARSessionInitMessage
  {
    public Guid StageIdentifier = Guid.Empty;

    public int ImageCompressionQuality = 30;
    public int TargetImageFramerate = 12;
    public int TargetBufferFramerate = 10;
  }

  [Serializable]
  internal sealed class ARSessionRunMessage
  {
    internal IARConfiguration arConfiguration = null;
    public ARSessionRunOptions runOptions = 0;
  }

  [Serializable]
  internal sealed class ARSessionPauseMessage
  {
  }

  [Serializable]
  internal sealed class ARSessionAddAnchorMessage
  {
    public _SerializableARAnchor Anchor = null;
  }

  [Serializable]
  internal sealed class ARSessionRemoveAnchorMessage
  {
    public Guid DeviceAnchorIdentifier;
  }

  [Serializable]
  internal sealed class ARSessionMergeAnchorMessage
  {
    public _SerializableARAnchor Anchor = null;
  }

  [Serializable]
  internal sealed class ARSessionSetWorldScaleMessage
  {
    public float WorldScale = 1.0f;
  }

  [Serializable]
  internal sealed class ARSessionDestroyMessage
  {
  }
#endregion

#region Networking
  [Serializable]
  internal sealed class NetworkingInitMessage
  {
    public ServerConfiguration Configuration = ServerConfiguration.ARBE;
    public Guid StageIdentifier = Guid.Empty;
  }

  [Serializable]
  internal sealed class NetworkingSendDataToPeersMessage
  {
    public static readonly Guid ID = new Guid("52c251b0-afb9-4e4d-ac31-b75cabec02f2");

    public uint Tag = 0U;
    public byte[] Data = null;
    public Guid[] Peers = null;
    public byte TransportType = 0;
    public bool SendToSelf = true;
  }

  [Serializable]
  internal sealed class NetworkingJoinMessage
  {
    public static readonly Guid ID = new Guid("dc0b20d4-08b1-4d6b-9039-1a37f0b062fd");

    public byte[] Metadata = null;
  }

  [Serializable]
  internal sealed class NetworkingLeaveMessage
  {
    public static readonly Guid ID = new Guid("dc0b20d4-08b1-4d6b-9039-1a37f0b062fe");

    public byte[] Metadata = null;
  }

  [Serializable]
  internal sealed class NetworkingDestroyMessage
  {
    public static readonly Guid ID = new Guid("5fb6ceb9-56aa-4057-ad2f-4a3ebc5a5836");
  }

  [Serializable]
  internal sealed class NetworkingStorePersistentKeyValueMessage
  {
    public static readonly Guid ID = new Guid("5fb6ceb9-57aa-4057-ad2f-4a3ebc5a5836");

    public byte[] Key = null;
    public byte[] Value = null;
  }

  [Serializable]
  internal sealed class NetworkingSendDataToArmMessage
  {
    public static readonly Guid ID = new Guid("52c251b0-afb9-4e4d-ac31-b75cabec02f3");

    public uint Tag = 0U;
    public byte[] Data = null;
  }

  #endregion

#region ARNetworking
  [Serializable]
  internal sealed class ARNetworkingInitMessage
  {
    public Guid StageIdentifier = Guid.Empty;
    public ServerConfiguration ServerConfiguration = ServerConfiguration.ARBE;
    public bool ConstructFromExistingNetworking = false;
  }

  [Serializable]
  internal sealed class ARNetworkingDestroyMessage
  {
  }
#endregion
}
