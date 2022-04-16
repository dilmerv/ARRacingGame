// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Networking.HLAPI.Authority {
  /// <summary>
  /// The common roles that can be used for network authority.
  /// </summary>
  public enum Role {
    None = 0,
    Observer,
    Authority = UInt16.MaxValue,
  }
}