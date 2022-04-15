// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Niantic.ARDK.Utilities
{
  /// <summary>
  /// This comparer class will just compare objects by their reference, not their content.
  /// It is useful for hashsets when we just want to check if we already hold an instance,
  /// or dictionaries when we want to "attach" properties to existing instances
  /// but don't want to affect identical instances.
  /// </summary>
  internal sealed class _ReferenceComparer<T>:
    IEqualityComparer<T>
  where
    T: class
  {
    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static readonly _ReferenceComparer<T> Instance = new _ReferenceComparer<T>();

    private _ReferenceComparer()
    {
    }

    /// <summary>
    /// Returns a boolean value telling whether a and b are actually the same instance.
    /// </summary>
    public bool Equals(T a, T b)
    {
      return object.ReferenceEquals(a, b);
    }

    /// <summary>
    /// Returns the immutable hash-code given to an instance when it is created.
    /// Such a hash-code is not affected by the class overriding GetHashCode() method.
    /// </summary>
    public int GetHashCode(T instance)
    {
      return RuntimeHelpers.GetHashCode(instance);
    }
  }
}
