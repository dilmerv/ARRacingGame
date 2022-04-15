// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

/// @namespace Niantic.ARDK.Networking.HLAPI.Routing
/// @brief Routing over a network
namespace Niantic.ARDK.Networking.HLAPI.Routing
{
  /// <summary>
  /// An id for representing something over the network.
  /// </summary>
  public struct NetworkId:
    IEquatable<NetworkId>
  {
    private readonly ulong _id;

    public NetworkId(ulong id)
    {
      _id = id;
    }

    public ulong RawId
    {
      get
      {
        return _id;
      }
    }

    public bool Equals(NetworkId other)
    {
      return _id.Equals(other._id);
    }

    public override bool Equals(object obj)
    {
      return obj is NetworkId && Equals((NetworkId)obj);
    }

    public override int GetHashCode()
    {
      return _id.GetHashCode();
    }

    public override string ToString()
    {
      return _id.ToString();
    }

    public static explicit operator ulong(NetworkId id)
    {
      return id._id;
    }

    public static explicit operator NetworkId(ulong id)
    {
      return new NetworkId(id);
    }

    public static bool operator == (NetworkId id1, NetworkId id2)
    {
      return id1._id == id2._id;
    }

    public static bool operator != (NetworkId id1, NetworkId id2)
    {
      return id1._id != id2._id;
    }
  }
}
