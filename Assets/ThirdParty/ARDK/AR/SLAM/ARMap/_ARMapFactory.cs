// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;

using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.AR.SLAM
{
  internal static class _ARMapFactory
  {
    internal static _SerializableARMap _AsSerializable(this IARMap source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableARMap;
      if (possibleResult != null)
        return possibleResult;
      
      return new _SerializableARMap(source.Identifier, source.WorldScale, source.Transform);
    }

    internal static _SerializableARMap[] _AsSerializableArray(this IList<IARMap> source)
    {
      if (source == null)
        return null;

      int count = source.Count;
      if (count == 0)
        return EmptyArray<_SerializableARMap>.Instance;
      
      var result = new _SerializableARMap[count];
      for(int i=0; i<count; i++)
        result[i] = source[i]._AsSerializable();

      return result;
    }
  }
}
