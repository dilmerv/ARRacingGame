// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Networking.NetworkAnchors
{
  /// Unique identifier for this shared anchor. If this anchor is uploaded to the cloud,
  /// it will have the same identifier across all sessions it is discovered in.
  /// @note This is currently in internal development, and not useable.
  [Serializable]
  public struct SharedAnchorIdentifier:
    IEquatable<SharedAnchorIdentifier>
  {
    internal Guid Guid
    {
      get
      {
        return _guid;
      }
    }

    private readonly Guid _guid;

    internal SharedAnchorIdentifier(Guid guid)
    {
      _guid = guid;
    }

    /// Checks if this struct has been initialized as a valid identifier.
    /// @returns True if the struct is uninitialized.
    public bool IsEmpty()
    {
      return _guid == Guid.Empty;
    }

    public override string ToString()
    {
      return "SharedAnchorIdentifier_" + _guid.ToString();
    }

    public override bool Equals(Object obj)
    {
      if (obj is SharedAnchorIdentifier)
        return Equals((SharedAnchorIdentifier) obj);

      return false;
    }

    public override int GetHashCode()
    {
      return _guid.GetHashCode();
    }

    public static bool operator ==(SharedAnchorIdentifier a, SharedAnchorIdentifier b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(SharedAnchorIdentifier a, SharedAnchorIdentifier b)
    {
      return !a.Equals(b);
    }

    public bool Equals(SharedAnchorIdentifier other)
    {
      return _guid.Equals(other._guid);
    }
  }
}
