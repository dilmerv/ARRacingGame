// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities.Extensions
{
  /// Extends the behaviour of Guids
  internal static class _GuidExtensions
  {
    /// Combine two Guids to deterministically create a new Guid
    public static Guid Combine(this Guid guid, Guid other)
    {
      var bytesThis = guid.ToByteArray();
      var bytesOther = other.ToByteArray();

      for (var i = 0; i < bytesThis.Length; i++)
        bytesThis[i] ^= bytesOther[i];

      return new Guid(bytesThis);
    }
  }
}
