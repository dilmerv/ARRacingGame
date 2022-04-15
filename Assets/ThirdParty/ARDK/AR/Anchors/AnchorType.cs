// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Anchors
{
  /// Possible types for an anchor. Useful when checking if an Anchor object is actually a
  /// sub-object.
  public enum AnchorType
  {
    /// An anchor.
    Base = 0,

    /// A plane anchor.
    Plane = 1,

    /// An image anchor.
    /// @note This is an iOS-only value.
    Image = 2,
      
    /// A face anchor.
    /// @note Face anchors are only available in the face tracking feature branch
    //Face = 3,
    
    /// A peer anchor, representing another user in a multiplayer session.
    Peer = 4,
    
    /// An anchor that is shared between clients in a multiplayer session.
    Shared = 5,
  }
}
