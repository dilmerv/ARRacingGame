// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Networking
{
  public interface IPeer:
    IEquatable<IPeer>
  {
    /// <summary>
    /// A unique identifier for the peer
    /// </summary>
    Guid Identifier { get; }

    string ToString();

    string ToString(int count);
  }
}
