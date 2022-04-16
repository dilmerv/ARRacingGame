// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Collections
{
  /// <summary>
  /// Generic class that generates a single, reusable instance of an empty array of type T.
  /// </summary>
  public static class EmptyArray<T>
  {
    public static readonly T[] Instance = new T[0];
  }
}
