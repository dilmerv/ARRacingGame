// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.ObjectModel;

namespace Niantic.ARDK.Utilities.Collections
{
  internal static class _ReadOnlyCollectionExtensions
  {
    public static ReadOnlyCollection<T> AsNonNullReadOnly<T>(this T[] source)
    {
      if (source == null || source.Length == 0)
        return EmptyReadOnlyCollection<T>.Instance;

      return new ReadOnlyCollection<T>(source);
    }
  }
}
