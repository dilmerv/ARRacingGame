// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// A handle for a device on remote connection.
  /// </summary>
  /// <remarks>
  /// This class structurally matches its native counter-part.
  /// </remarks>
  [StructLayout(LayoutKind.Explicit)]
  internal struct _DeviceHandle: IEquatable<_DeviceHandle>
  {
    /// <summary>
    /// The UUID of the device.
    /// </summary>
    [FieldOffset(0)]
    public Guid Identifier;

    public bool Equals(_DeviceHandle other)
    {
      return Identifier.Equals(other.Identifier);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
        return false;

      return obj is _DeviceHandle && Equals((_DeviceHandle)obj);
    }

    public override int GetHashCode()
    {
      return Identifier.GetHashCode();
    }

    /// <summary>
    /// Checks if two handles are the same.
    /// </summary>
    public static bool operator ==(_DeviceHandle left, _DeviceHandle right)
    {
      return left.Equals(right);
    }

    /// <summary>
    /// Checks if two handles are not the same.
    /// </summary>
    public static bool operator !=(_DeviceHandle left, _DeviceHandle right)
    {
      return !left.Equals(right);
    }
  }
}
