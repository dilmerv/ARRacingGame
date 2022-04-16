// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  /// This class has the purpose of allowing C# objects to be passed to native code as a "handle"
  /// and then obtained back when native code calls back into C#. This is effectively the same
  /// purpose as the GCHandle struct, yet this one is somewhat safer, as it will consistently return
  /// null if we try to get a value of a freed handle, while the one from .NET might return null,
  /// might throw an exception or, even worse, might return an invalid value.
  public static class SafeGCHandle
  {
    // This number needs to be a power of 2. That's why bit-shifting is the best option.
    private const uint kParallelism = 1 << 3;

    // Assuming kParallelism is a multiple of 2, this will have all the lower bytes set.
    // We prefer to use & (and operator) instead of % (mod operator) because it is faster.
    private const uint kBucketSelectorAnd = kParallelism - 1;

    // Maybe a TODO?
    // Considering we are using ConcurrentDictionaries now, I am not sure how much improvement (if
    // any) the kParallelism is giving us. As we keep adding and removing items, the kParallelism
    // is probably helping us on resizes, but assuming we reach a good size, resizes should stop
    // and then the ConcurrentDictionary is just "lock free" all the time... so, is this extra
    // indirection with the array of ConcurrentDictionaries helping us or hurting us?
    private static readonly ConcurrentDictionary<IntPtr, object>[] _dictionaries =
      new ConcurrentDictionary<IntPtr, object>[kParallelism];

    private static long _idGenerator;
    static SafeGCHandle()
    {
      for (int i = 0; i < kParallelism; i++)
        _dictionaries[i] = new ConcurrentDictionary<IntPtr, object>();
    }

    /// Allocates a new handle for the given value, which guarantees that the object is kept alive
    /// even if there are no other references, and returns it just as an IntPtr. A future call to
    /// Free needs to be made, or there will be memory leaks.
    ///
    /// @param instance - The instance to create a handle for. null is a valid argument and will
    ///        just return the handle 0 (IntPtr.Zero).
    ///
    /// @return An IntPtr value that can be used to locate this instance again.
    public static IntPtr AllocAsIntPtr(object instance)
    {
      if (instance == null)
        return IntPtr.Zero;

      long id = Interlocked.Increment(ref _idGenerator);
      uint bucketIndex = ((uint)id) & kBucketSelectorAnd;
      IntPtr idAsIntPtr = new IntPtr(id);
      var dictionary = _dictionaries[bucketIndex];
      _StaticMemberValidator._CollectionsAreEmptyWhenScopeEnds(() => _dictionaries);

      // As we are using an unique id generator, we know that the TryAdd is actually going to add.
      // The only situation where it fails (without throwing) is if the key is duplicated.
      dictionary.TryAdd(idAsIntPtr, instance);

      return idAsIntPtr;
    }

    /// Allocates a new typed safe handle for the given instance. Such returned object is just a
    /// struct and it will not release the object if it goes out-of-scope. This is on purpose, as
    /// the intended use is to pass such handle to the native side and then allow the local one
    /// to go out of scope. At a later point, the instance might be obtained by calling
    /// TryGetInstance or another SafeGCHandle&lt;T&gt; can be reconstructed from its IntPtr
    /// representation by calling SafeGCHandle&lt;T&gt;.FromIntPtr(handleValue).
    /// The handle needs to be explicitly freed when we don't need to hold a reference to the
    /// instance anymore by calling Free() (either from the typed SafeGCHandle or by giving its
    /// IntPtr value).
    ///
    /// @param instance - The instance to create a handle to. If null is provided, a handle with ID
    ///        0 will be returned.
    ///
    /// @return a SafeGCHandle to access this instance again.
    public static SafeGCHandle<T> Alloc<T>(T instance)
    where
      T: class
    {
      IntPtr id = AllocAsIntPtr(instance);
      return SafeGCHandle<T>.FromIntPtr(id);
    }

    /// Tries to get the instance of a previously allocated handle by the handle ID. This method
    /// doesn't care about the actual type of the instance and will return it just as "object".
    ///
    /// @param id - The IntPtr ID of the allocated handle to access to the instance. This is
    ///        obtained by calling AllocAsIntPtr of by calling Alloc to get a handle and then
    ///        calling ToIntPtr() on the handle.
    ///
    /// @return Either the instance as object, or null if the id is either invalid or the handle
    ///         was already released.
    public static object TryGetUntypedInstance(IntPtr id)
    {
      if (id == IntPtr.Zero)
        return null;

      uint buckedIndex = ((uint)id) & kBucketSelectorAnd;
      var dictionary = _dictionaries[buckedIndex];

      object result;
      dictionary.TryGetValue(id, out result);

      // If TryGetValue fails, it always sets result to null.
      return result;
    }

    /// Tries to get an instance referenced by its handle ID as the given generic T type. T must be
    /// a class and null will be returned if either the result is null or the given T type is
    /// incorrect.
    ///
    /// @param id - The IntPtr ID from the allocated handle that can be used to find the instance
    ///        again.
    ///
    /// @return Either the found instance or null.
    public static T TryGetInstance<T>(IntPtr id)
    where
      T : class
    {
      object untypedInstance = TryGetUntypedInstance(id);

      if (untypedInstance == null)
        return null;

      T result = untypedInstance as T;
      if (result == null)
      {
        Debug.Log(
          "Tried to get instance of type " + untypedInstance.GetType().FullName + " as " +
          typeof(T).FullName + " but that's not possible.");
      }

      return result;
    }

    /// Tries to free the handle identified by the given ID.
    ///
    /// @param id - The ID of the handle to free.
    ///
    /// @return True if such ID existed and was freed, false if it wasn't valid (either because
    ///         it never was or because it was being previously freed).
    public static bool Free(IntPtr id)
    {
      if (id == IntPtr.Zero)
        return false;

      uint buckedIndex = ((uint)id) & kBucketSelectorAnd;
      var dictionary = _dictionaries[buckedIndex];
      return dictionary.TryRemove(id, out _);
    }
  }

  /// Represents a type-safe, SafeGCHandle, that can be used to either get access to the instance
  /// again, to obtain its IntPtr ID, or to free the handle.
  ///
  /// This struct is marshalled just as the IntPtr ID it contains, so [DllImport] methods that need
  /// to receive or return handles of a particular type can request for a
  /// SafeGCHandle&lt;SomeType&gt; instead of requesting just for an IntPtr.
  [StructLayout(LayoutKind.Sequential)]
  public struct SafeGCHandle<T>:
    IEquatable<SafeGCHandle<T>>
  where
    T: class
  {
    /// Instantiates a new SafeGCHandle&lt;T&gt; from a provided IntPtr ID. This action never fails
    /// but if the ID is invalid, a future call to TryGetInstance() will return null.
    ///
    /// @param id - The IntPtr ID that represents the allocated handle.
    public static SafeGCHandle<T> FromIntPtr(IntPtr id)
    {
      return new SafeGCHandle<T>(id);
    }

    private readonly IntPtr _id;
    private SafeGCHandle(IntPtr id)
    {
      _id = id;
    }

    /// Gets the inner IntPtr ID of this safe-handle.
    ///
    /// @return The IntPtr ID of this safe-handle.
    public IntPtr ToIntPtr()
    {
      return _id;
    }

    /// Tries to get the instance represented by this safe-handle, already cast to its right type.
    ///
    /// @return Either the typed-instance or null (if the handle is invalid, deallocated or of the
    ///         wrong type).
    public T TryGetInstance()
    {
      return SafeGCHandle.TryGetInstance<T>(_id);
    }

    /// Tries to free the effective handle this struct represents.
    ///
    /// @return Either true if the handle was valid and was deallocated, or false if the handle
    ///         was somehow invalid (either already deallocated or never allocated).
    public bool Free()
    {
      return SafeGCHandle.Free(_id);
    }

    /// Returns the hashcode of this handle, which is effectively the hashcode of its IntPtr ID.
    ///
    /// @return The hashcode of the ID of this handle.
    public override int GetHashCode()
    {
      return _id.GetHashCode();
    }

    /// Compares this handle to any object. It can only return true if obj is actually another
    /// SafeGCHandle of the same T type and pointing to the same ID.
    ///
    /// @returns true if the provided obj is a SafeGCHandle of the same type and referencing the
    ///          same ID. Note that two SafeGCHandles with different IDs that point to the same
    ///          instance are still considered different, as each one of them need to be Free()d
    ///          separately.
    public override bool Equals(object obj)
    {
      if (!(obj is SafeGCHandle<T>))
        return false;

      SafeGCHandle<T> other = (SafeGCHandle<T>)obj;
      return this.Equals(other);
    }

    /// Checks if the provided handle equals this one. This means both have the same ID.
    /// Two different IDs can point to the same instance, but they will still return false in this
    /// check, as each one of them need to be Free()d independently.
    ///
    /// @return True if the IDs of both handles match, false otherwise.
    public bool Equals(SafeGCHandle<T> other)
    {
      return _id == other._id;
    }
  }
}
