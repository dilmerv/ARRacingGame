// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Concurrent;

using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  // A class that works like a dictionary but allows the values to be collected, effectively
  // removing the entire pair from the dictionary when that happens.
  internal sealed class _WeakValueDictionary<TKey, TValue>:
    IDisposable, IEnumerable
  where
    TValue: class
  {
    private static readonly Func<TKey, WeakReference> _createWeakReference =
      (_) => new WeakReference(null);

    internal ConcurrentDictionary<TKey, WeakReference> _innerDictionary =
      new ConcurrentDictionary<TKey, WeakReference>();

    internal _WeakValueDictionary()
    {
      _GcObserver._GcHappened += _ClearCollectedValues;
    }

    public void Dispose()
    {
      _GcObserver._GcHappened -= _ClearCollectedValues;
      _innerDictionary = null;
    }

    public IEnumerator GetEnumerator()
    {
      return _innerDictionary.GetEnumerator();
    }

    // Aside from tests and individual checks, using Count is not really safe, as we might
    // check that Count is bigger than zero and, at next line, it is 0 already because a GC
    // happened between those to calls.
    public int Count
    {
      get { return _innerDictionary.Count; }
    }

    // This method removes all the key/value pairs for those items that were already collected.
    // The fact that WeakReferences allow their values to be collected might still cause memory
    // leaks if we never remove the WeakReferences themselves from the dictionaries, as those
    // dictionaries will just grow and will not allow either the key, or the WeakReference, to die.
    private void _ClearCollectedValues()
    {
      foreach (var pair in _innerDictionary)
      {
        var weakReference = pair.Value;

        // This double-checked "is-alive" condition exists for the following reason:
        // Even if we are using a ConcurrentDictionary and WeakReferences are thread-safe, there is
        // a chance that we check a WeakReference is dead but, just "before" removing it, another
        // call to TryAdd or GetOrAdd was made, effectively making the WeakReference alive again.
        // So, to avoid any issues, if the WeakReference is not alive, we lock it, check that it
        // is still not alive, and then remove it while holding the lock.
        if (!weakReference.IsAlive)
          lock (weakReference)
            if (!weakReference.IsAlive)
              _innerDictionary.TryRemove(pair.Key, out _);
      }
    }

    public bool TryAdd(TKey key, TValue value)
    {
      if (key == null)
        throw new ArgumentNullException(nameof(key));

      var weakReference = _innerDictionary.GetOrAdd(key, _createWeakReference);

      if (weakReference.IsAlive)
        return false;

      lock (weakReference)
      {
        if (weakReference.IsAlive)
          return false;

        weakReference.Target = value;

        // In the expected case, we will find a WeakReference for our key, which is the exact
        // WeakReference we just set.
        WeakReference oldWeakReference;
        if (_innerDictionary.TryGetValue(key, out oldWeakReference))
          return oldWeakReference == weakReference;

        // If that's not the case, that means that while we found a WeakReference and decided to set
        // it, another thread decided to remove our weak-reference. It doesn't matter if they added
        // a different WeakReference or not. We are just going to try to re-add our weakReference,
        // and return if we succeeded (so our weakReference is the valid one) or not (so, the new
        // one that was added is the one that stays and ours is useless).
        return _innerDictionary.TryAdd(key, weakReference);
      }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> createValue)
    {
      if (key == null)
        throw new ArgumentNullException(nameof(key));

      while (true)
      {
        // NO-WEAK-REFERENCE-YET.
        var weakReference = _innerDictionary.GetOrAdd(key, _createWeakReference);

        while (true)
        {
          // CHECKING-EXISTING-WEAK-REFERENCE
          var untypedResult = weakReference.Target;
          if (untypedResult != null)
            return (TValue)untypedResult;

          lock (weakReference)
          {
            // Check again as a value might have been added while we tried to lock.
            untypedResult = weakReference.Target;
            if (untypedResult != null)
              return (TValue)untypedResult;

            // This might look as a very odd thing to do, but we want to guarantee that we do not
            // created duplicated values when dealing with our own collection mechanism. So, if a
            // weak-reference was removed, we need to try to re-add it. If we can't, we try to get
            // the existing one by letting the loop run.
            WeakReference existingWeakReference;
            _innerDictionary.TryGetValue(key, out existingWeakReference);
            if (existingWeakReference != weakReference)
            {
              // If the existing weak reference is not null, that means we simply should ignore
              // ours.
              if (existingWeakReference != null)
              {
                // Changing weakReference here shouldn't affect the lock/unlock logic... C# and
                // .NET are prepared to lock/unlock the same object even if the variable contents
                // changed. In fact the disabled warning tells us that the unlock will happen on the
                // original value, which is what we want.
                // So, here we just want to go back to where untypedResult is set using the new
                // weakRefernce value. That is, we go back to (CHECKING-EXISTING-WEAK-REFERENCE).
#pragma warning disable CS0728
                weakReference = existingWeakReference;
#pragma warning restore CS0728
                continue;
              }

              // As the existing was null, we try to re-add it. If we can, we proceed, if not, we
              // need to retry the entire process.
              if (!_innerDictionary.TryAdd(key, weakReference))
                break; // Means we will evaluate weakReference again. Go to NO-WEAK-REFERENCE-YET.
            }

            // If we got to this point, our weakReference is guaranteed to still be in the
            // dictionary and it cannot be removed as to remove it, we need to lock it, and we
            // already hold the lock. So, from now on we just create and set the value that we are
            // going to return.
            var result = createValue(key);
            weakReference.Target = result;
            return result;
          }
        }
      }
    }

    public TValue TryGetValue(TKey key)
    {
      if (key == null)
        throw new ArgumentNullException(nameof(key));

      WeakReference weakReference;
      if (!_innerDictionary.TryGetValue(key, out weakReference))
        return null;

      return (TValue)weakReference.Target;
    }

    public bool Remove(TKey key)
    {
      if (key == null)
        throw new ArgumentNullException(nameof(key));

      return _innerDictionary.TryRemove(key, out _);
    }
  }
}
