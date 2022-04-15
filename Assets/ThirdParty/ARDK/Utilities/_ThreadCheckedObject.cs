// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Diagnostics;

namespace Niantic.ARDK.Utilities
{
  internal class _ThreadCheckedObject
  {
    [ThreadStatic]
    private static object _staticCurrentThreadToken;
    
    private static object _GetOrCreateCurrentThreadToken()
    {
      var result = _staticCurrentThreadToken;

      if (result == null)
      {
        result = new object();
        _staticCurrentThreadToken = result;
      }

      return result;
    }

    private readonly object _creatorThreadToken;
    
    internal _ThreadCheckedObject()
    {
      _creatorThreadToken = _GetOrCreateCurrentThreadToken();
    }

    [Conditional("DEBUG")]
    internal void _CheckThread()
    {
      // We don't use _GetOrCreateCurrentThreadToken as we don't want to create a token for
      // this thread if we don't need to.
      if (_creatorThreadToken != _staticCurrentThreadToken)
        throw new InvalidOperationException("This action can only be done by the creator thread.");
    }
  }
}
