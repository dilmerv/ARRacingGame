// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Niantic.ARDK.Utilities
{
  // Byte arrays don't provide a valid hash code according to its data, so if we want to use them
  // as keys in dictionaries (or just store them in HashSets) we need to provide a comparer that
  // provides a consistent hashcode based on the bytes, allowing 2 different byte arrays that are
  // equal in contents to be seen as equals and having the same hashcode.
  // Notice that changing the contents of the array will change the hashcode, so do not do that
  // after adding the array as a key in a dictionary or hashset, as this will make the item "lost"
  // because of the different hash used to store it.
  internal sealed class _ByteArrayComparer:
    IEqualityComparer<byte[]>
  {
    public static readonly _ByteArrayComparer Instance = new _ByteArrayComparer();
    
    private _ByteArrayComparer()
    {
    }
    
    // Gets a hash-code by "XORing" all the Int32 values in the array (the code actually uses UInt64
    // blocks for performance, and at the end XORs the first 32 bits with the last 32 bits).
    // As that simple XORing could always generate the exact same hashcode for any empty memory
    // block of any length, the starting XORing value is actually the Length of the array itself.
    // If the array has a length that is not multiple of a "block" the final bytes are xored byte
    // by byte, but endianness is ignored. That means that a big-endian processor and a
    // little-endian processor will generate different values, yet the generation is consistent in
    // the same process (so, it is OK to use as dictionary hasher, but not OK to save in a file).
    public int GetHashCode(byte[] array)
    {
      if (array == null)
        return 0;

      unchecked
      {
        int length = array.Length;
        UInt64 result = (UInt64)length;
        int countBlocks = length / 8;
        unsafe
        {
          fixed (byte* untypedPointer = array)
          {
            UInt64* blockPointer = (UInt64*)untypedPointer;
            for (int i = 0; i < countBlocks; i++)
              result ^= blockPointer[i];

            int indexAtLastBlock = countBlocks * 8;
            for (int i = indexAtLastBlock; i < length; i++)
            {
              UInt64 valueAtIndex = untypedPointer[i];
              int bitShiftAmount = (i % 8) * 8;
              result ^= valueAtIndex << bitShiftAmount;
            }
          }
        }
        
        return (int)(result ^ (result >> 32));
      }
    }
    public bool Equals(byte[] x, byte[] y)
    {
      // If both are the same instance, no need to check anything else.
      if (x == y)
        return true;
      
      // As the instances are different, if one of them is null, they are just different.
      // No need to check further, also we can't access Length on null.
      if (x == null || y == null)
        return false;

      // When length is different, they are clearly not the same.
      if (x.Length != y.Length)
        return false;
      
      return x.SequenceEqual(y);
    }
  }
}
