// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.ObjectModel;

namespace Niantic.ARDK.Utilities.Collections
{
  public static class EmptyReadOnlyCollection<T>
  {
    public static readonly ReadOnlyCollection<T> Instance =
      new ReadOnlyCollection<T>(EmptyArray<T>.Instance);
  }
}
