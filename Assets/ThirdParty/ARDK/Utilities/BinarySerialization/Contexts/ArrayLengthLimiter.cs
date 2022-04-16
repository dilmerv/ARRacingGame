// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using UnityEngine;

namespace Niantic.ARDK.Utilities.BinarySerialization.Contexts
{
  /// <summary>
  /// Context class used by array serializers to limit the maximum amount of array items
  /// in a single serialization (even if from different arrays). This is used to avoid possible
  /// Denial of Service attacks where a Stream tells to allocate 2GB of memory or similar.
  /// The limit is int items, not actual bytes allocated. By default, the limit is 10 million items,
  /// which is around one 10mb for bytes, 40mb for ints.
  /// </summary>
  public sealed class ArrayLengthLimiter:
    ISerializationContext
  {
    private static int _limit = 10 * 1000 * 1000; // Approx 10mb.

    /// <summary>
    /// Gets or sets the limit of array items that can be serialized or deserialized.
    /// This value is used *before* allocating memory during deserialization, as a way to avoid
    /// DoS attacks.
    /// </summary>
    public static int Limit
    {
      get
      {
        return _limit;
      }
      set
      {
        if (value < 0)
          throw new ArgumentOutOfRangeException("Limit");

        _limit = value;
      }
    }

    /// <summary>
    /// Gets the amount of array items already in use during the current (de)serialization process.
    /// </summary>
    public int AmountInUse { get; private set; }

    private const string _errorMessageAboveLimit =
      "The requested amount will pass the limit length reserved for this serialization operation.";
      
    /// <summary>
    /// Reserves the given amount of "array items" or throw an InvalidOperationException if that
    // amount will be above the limit allowed for the current serialization process.
    /// </summary>
    public void ReserveOrThrow(int amount)
    {
      if (AmountInUse + amount > _limit)
        throw new InvalidOperationException(_errorMessageAboveLimit);

      AmountInUse += amount;
    }
  }
}
