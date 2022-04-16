// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Collections
{
  public static class EmptyArdkReadOnlyCollection<T>
  {
    public static readonly ARDKReadOnlyCollection<T> Instance =
      EmptyArray<T>.Instance.AsArdkReadOnly();
  }
}
