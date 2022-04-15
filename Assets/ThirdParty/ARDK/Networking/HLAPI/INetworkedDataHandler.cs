// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Networking.HLAPI
{
  /// <summary>
  /// Interface for a data handler that is attached to the HLAPI through an INetworkGroup.
  /// @note Implement custom data handlers by extending `NetworkedDataHandlerBase` rather than this
  ///   class. The protected methods in that abstract class define the data handler's behaviour
  ///   with sending/receiving data. However, users should never call those methods, as it is the
  ///   role of the INetworkGroup to send/receive data.
  /// </summary>
  public interface INetworkedDataHandler
  {
    /// <summary>
    /// Unique identifier for this data handler. Two handlers with the same Identifier cannot be
    ///   registered to the same INetworkGroup
    /// </summary>
    string Identifier { get; }

    /// <summary>
    /// Group that this data handler is attached to.
    /// </summary>
    INetworkGroup Group { get; }

    /// <summary>
    /// Unregister this handler from its current group.
    /// </summary>
    void Unregister();
  }
}
