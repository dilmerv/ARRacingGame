// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Internals;

namespace Niantic.ARDK.AR.SLAM
{
  /// Unique identifier for map layers. A map layer is a collection of maps stored in the cloud
  /// that together form a persistent, collaboratively built representation of the real-world
  /// environment.
  /// @note This is part of an experimental feature that is currently disabled in release builds.
  [Serializable]
  public struct MapLayerIdentifier
  {
    private readonly Guid _guid;

    public MapLayerIdentifier(Guid guid)
    {
      _guid = guid;
    }

    public bool IsEmpty()
    {
      return _guid == Guid.Empty;
    }

    internal Guid _ToGuid()
    {
      return _guid;
    }

    public override string ToString()
    {
      return "MapLayerIdentifier_" + _guid.ToString();
    }

    public override bool Equals(Object obj)
    {
      if (obj is MapLayerIdentifier)
        return Equals((MapLayerIdentifier) obj);

      return false;
    }

    public override int GetHashCode()
    {
      return _guid.GetHashCode();
    }

    public static bool operator ==(MapLayerIdentifier a, MapLayerIdentifier b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(MapLayerIdentifier a, MapLayerIdentifier b)
    {
      return !a.Equals(b);
    }

    public bool Equals(MapLayerIdentifier other)
    {
      return _guid.Equals(other._guid);
    }
  }
}
