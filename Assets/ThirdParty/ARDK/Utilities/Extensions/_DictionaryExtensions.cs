// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Niantic.ARDK.Utilities.Extensions
{
  /// Offers alternative get behaviours for dictionaries
  internal static class _DictionaryExtensions
  {
    /// <summary>
    /// Gets a value from a dictionary, or gives the default(TValue) if it cannot be found.
    /// </summary>
    public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    {
      TValue value;
      dict.TryGetValue(key, out value);
      return value;
    }

    /// Gets a value from a dictionary, or gives the default if it cannot be found.
    public static TValue GetOrDefault<TKey, TValue>
    (
      this Dictionary<TKey, TValue> dict,
      TKey key,
      Func<TValue> defaultFetcher
    )
    {
      TValue value;

      if (dict.TryGetValue(key, out value))
        return value;

      return defaultFetcher != null ? defaultFetcher() : default(TValue);
    }

    /// Gets a value from a dictionary, or creates a new value.
    /// The new value will be inserted into the dictionary.
    public static TValue GetOrInsert<TKey, TValue>
    (
      this Dictionary<TKey, TValue> dict,
      TKey key,
      Func<TValue> creator
    )
    {
      TValue value;

      if (!dict.TryGetValue(key, out value))
      {
        value = creator();
        dict.Add(key, value);
      }

      return value;
    }

    /// Gets a value from a dictionary, or inserts the provided new value.
    /// The new value will be inserted into the dictionary.
    public static TValue GetOrInsert<TKey, TValue>
    (
      this Dictionary<TKey, TValue> dict,
      TKey key,
      TValue valueToInsert
    )
    {
      TValue value;

      if (!dict.TryGetValue(key, out value))
      {
        value = valueToInsert;
        dict.Add(key, value);
      }

      return value;
    }

    /// Gets a value from a dictionary, or creates a new value using its default constructor.
    /// The new value will be inserted into the dictionary.
    public static TValue GetOrInsertNew<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    where
      TValue: new()
    {
      TValue value;

      if (!dict.TryGetValue(key, out value))
      {
        value = new TValue();
        dict.Add(key, value);
      }

      return value;
    }
  }
}
