// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities
{
  /// <summary>
  /// A value that may not exist.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <remarks>
  /// Optional provides implicit conversion between itself and <code>T</code> and explicit for the
  /// opposite.
  /// </remarks>
  public struct Optional<T>:
    IEquatable<Optional<T>>,
    IEquatable<T>
  {
    private T _value;
    private bool _hasValue;

    /// <summary>
    /// True when a value does exist.
    /// </summary>
    public bool HasValue
    {
      get { return _hasValue; }
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <exception cref="NullReferenceException">
    /// If getting the value and there is no value.
    /// </exception>
    public T Value
    {
      get
      {
        if (!_hasValue)
          throw new NullReferenceException();

        return _value;
      }
      set
      {
        _value = value;
        _hasValue = (value != null);
      }
    }

    /// <summary>
    /// Creates a new optional with a given default value.
    /// </summary>
    /// <param name="value"></param>
    public Optional(T value)
    {
      _value = value;
      _hasValue = (value != null);
    }

    /// <summary>
    /// Clears the value.
    /// </summary>
    public void Clear()
    {
      _hasValue = false;
      _value = default(T);
    }

    /// <summary>
    /// Checks the equality between two Optionals
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Optional<T> other)
    {
      if (_hasValue != other._hasValue)
        return false;

      if (!_hasValue)
        return true;

      return _value.Equals(other._value);
    }

    /// <summary>
    /// Checks the equality between and Optional<T> and T
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(T other)
    {
      if (other == null)
        return !_hasValue;

      return _value.Equals(other);
    }

    /// <summary>
    /// Returns the value of the optional, or the default of T if no value has been set
    /// </summary>
    /// <returns></returns>
    public T GetOrDefault()
    {
      return _value;
    }

    public override bool Equals(object obj)
    {
      if (obj is Optional<T>)
        return Equals((Optional<T>)obj);

      if (obj is T)
        return Equals((T)obj);

      return false;
    }

    public override int GetHashCode()
    {
      if (!_hasValue)
        return 0;

      return _value.GetHashCode();
    }

    public static explicit operator T(Optional<T> optional)
    {
      return optional.Value;
    }

    public static implicit operator Optional<T>(T value)
    {
      return new Optional<T>(value);
    }

    public static bool operator ==(Optional<T> a, Optional<T> b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(Optional<T> a, Optional<T> b)
    {
      return !(a == b);
    }

    public static bool operator ==(Optional<T> a, T b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(Optional<T> a, T b)
    {
      return !(a == b);
    }

    public static bool operator ==(T a, Optional<T> b)
    {
      return b.Equals(a);
    }

    public static bool operator !=(T a, Optional<T> b)
    {
      return !(a == b);
    }
  }
}
