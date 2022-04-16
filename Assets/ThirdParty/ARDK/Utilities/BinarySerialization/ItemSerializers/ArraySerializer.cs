// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public static class ArraySerializer<T>
  {
    public static readonly IItemSerializer<T[]> Instance;

    static ArraySerializer()
    {
      if (typeof(T).IsSealed)
        Instance = ArrayOfSealedSerializer<T>.Instance;
      else
        Instance = ArrayOfUnsealedSerializer<T>.Instance;
    }
  }
}
