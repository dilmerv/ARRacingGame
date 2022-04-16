// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Niantic.ARDK.Utilities.Collections
{
  /// <summary>
  /// Helper class to create new ARDKReadOnlyCollection&lt;T&gt; instances by using type-inference.
  /// That is: var readOnlySet = hashSet.AsArdkReadOnly();
  /// </summary>
  public static class ARDKReadOnlyCollection
  {
    public static ARDKReadOnlyCollection<T> AsArdkReadOnly<T>(this ICollection<T> mutableCollection)
    {
      return new ARDKReadOnlyCollection<T>(mutableCollection);
    }
  }

  /// <summary>
  /// Represents a read-only accessor to a mutable collection, similar to what ReadOnlyCollection
  /// does to lists and arrays, but being just a "collection" (the .NET ReadOnlyCollection should be
  /// named ReadOnlyList).
  /// Instances of this class cannot modify the collection they point to, yet the original
  /// collection can still be modified by anyone having a reference to it (and any changes will be
  /// visible by instances of this class).
  /// </summary>
  /// <remarks>
  /// This class implements the mutable ICollection&lt;T&gt; (but throws on any mutation method) to
  /// support the fast LINQ operations. LINQ cast only to the mutable interfaces, not to the
  /// read-only ones.
  /// </remarks>
  public sealed class ARDKReadOnlyCollection<T>:
    IReadOnlyCollection<T>,
    ICollection<T>
  {
    private readonly ICollection<T> _collection;

    public ARDKReadOnlyCollection(ICollection<T> mutableCollection)
    {
      if (mutableCollection == null)
        throw new ArgumentNullException(nameof(mutableCollection));

      _collection = mutableCollection;
    }

    public int Count
    {
      get
      {
        return _collection.Count;
      }
    }

    public bool Contains(T item)
    {
      return _collection.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      _collection.CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
      return _collection.GetEnumerator();
    }

    // Explicit IEnumerable implementation.
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    // Explicit ICollection implementation.
    bool ICollection<T>.IsReadOnly
    {
      get {
        return true;
      }
    }

    void ICollection<T>.Clear()
    {
      throw new NotSupportedException("This collection is read-only.");
    }

    void ICollection<T>.Add(T item)
    {
      throw new NotSupportedException("This collection is read-only.");
    }
    
    bool ICollection<T>.Remove(T item)
    {
      throw new NotSupportedException("This collection is read-only.");
    }
  }
}
