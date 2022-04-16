// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

namespace Niantic.ARDK.Utilities.BinarySerialization
{
  /// <summary>
  /// Interface used to "mark" a class as a serialization-context, so it can be used
  /// in a call to BinarySerializer.GetContext&lt;T&gt;();.
  /// Context classes must be default-constructible.
  /// </summary>
  public interface ISerializationContext
  {
  }
}