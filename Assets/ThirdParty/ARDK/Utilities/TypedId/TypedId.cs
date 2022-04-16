// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities.TypedId
{
  /// <summary>
  /// A generic identifier that takes into account the type of object that it is representing. For
  ///   example, a TypedId&amp;#60;uint, ClassA&amp;#62; with a value of 0 would not equal a
  ///   TypedId&amp;#60;uint, ClassB&amp;#62 with a value of 0.
  /// </summary>
  /// <typeparam name="TRepresented">Type of the object being represented</typeparam>
  /// <typeparam name="TId">Type of the identifier</typeparam>
  public struct TypedId<TRepresented, TId>:
    IEquatable<TypedId<TRepresented, TId>>,
    IEquatable<TId>
  where
    TRepresented: IRepresentedBy<TId>
  where
    TId: IEquatable<TId>
  {
    /// <summary>
    /// The raw (untyped) identifier held by this TypedId
    /// </summary>
    public TId RawIdentifier { get; private set; }

    /// <summary>
    /// Generate a new TypedId with a raw identifier
    /// </summary>
    /// <param name="rawIdentifier">Value to give this TypedId</param>
    public TypedId(TId rawIdentifier)
      : this()
    {
      RawIdentifier = rawIdentifier;
    }

    /// <summary>
    /// Check if an object is a TypedId<TRepresented, TId>, and if it is equivalent to this TypedId
    /// </summary>
    public override bool Equals(object other)
    {
      return other is TypedId<TRepresented, TId> && Equals((TypedId<TRepresented, TId>) other);
    }

    /// <summary>
    /// Check if another TypedId is equivalent to this TypedId
    /// </summary>
    public bool Equals(TypedId<TRepresented, TId> other)
    {
      if (RawIdentifier == null)
        return other.RawIdentifier == null;

      return RawIdentifier.Equals(other.RawIdentifier);
    }

    /// <summary>
    /// Check if this TypedId is equivalent to a raw identifier
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(TId other)
    {
      if (RawIdentifier == null)
        return other == null;
      
      return RawIdentifier.Equals(other);
    }

    /// <summary>
    /// Get the hashcode of this TypedId
    /// </summary>
    public override int GetHashCode()
    {
      if (RawIdentifier == null)
        return -1;

      return RawIdentifier.GetHashCode();
    }
    
    public static bool operator == (TypedId<TRepresented, TId> lhs, TypedId<TRepresented, TId> rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator != (TypedId<TRepresented, TId> lhs, TypedId<TRepresented, TId> rhs)
    {
      return !(lhs == rhs);
    }

    public static bool operator == (TypedId<TRepresented, TId> lhs, TId rhs)
    {
      return lhs.Equals(rhs);
    }

    public static bool operator != (TypedId<TRepresented, TId> lhs, TId rhs)
    {
      return !(lhs == rhs);
    }
    
    public static bool operator == (TId lhs, TypedId<TRepresented, TId> rhs)
    {
      return rhs.Equals(lhs);
    }

    public static bool operator != (TId lhs, TypedId<TRepresented, TId> rhs)
    {
      return !(lhs == rhs);
    }
  }
}
