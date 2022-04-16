// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Niantic.ARDK.Utilities
{
  /// Helper class to create KeyValuePairs using type-inference.
  /// Instead of new KeyValuePair&lt;type1, type2&gt;(value1, value2) just do:
  ///   _KeyValuePair.Create(value1, value2);
  internal static class _KeyValuePair
  {
    public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
    {
      return new KeyValuePair<TKey, TValue>(key, value);
    }
  }
}
