// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR
{
  internal struct _CppAddressAndScale:
    IEquatable<_CppAddressAndScale>
  {
    internal readonly IntPtr _cppAddress;
    internal readonly float _scale;

    internal _CppAddressAndScale(IntPtr cppAddress, float scale)
    {
      _cppAddress = cppAddress;
      _scale = scale;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is _CppAddressAndScale))
        return false;

      var other = (_CppAddressAndScale)obj;
      return Equals(other);
    }

    public bool Equals(_CppAddressAndScale other)
    {
      return _cppAddress == other._cppAddress && _scale == other._scale;
    }

    public override int GetHashCode()
    {
      return _cppAddress.GetHashCode() ^ _scale.GetHashCode();
    }
  }
}
