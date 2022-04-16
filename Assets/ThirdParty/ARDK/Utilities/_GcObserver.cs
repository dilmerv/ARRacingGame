// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Niantic.ARDK.Utilities
{
  // Class to "observe" when a garbage collection happens. This is useful to allow code that
  // uses collections of WeakReferences, so them we can release the now nullified ones. 
  internal static class _GcObserver
  {
    static _GcObserver()
    {
      var reviver = new _Reviver();
     
      // This KeepAlive call actually does nothing. The object is free to die on the next line
      // and that's actually what we want. This call is being done only to avoid a warning that
      // a new object was never assigned or that the reviver variable was never used.
      GC.KeepAlive(reviver);
    }

    private sealed class _Reviver
    {
      // This needs to be static or else the DomainUnload event will keep this object alive.
      // We use this flag to stop the _Reviver from reviving when the domain is unloading, or else
      // we "dead-lock" as the domain cannot be unloaded untill all the objects are dead and, as the
      // name says, the reviver keeps reviving.
      private static volatile bool _isUnloading;

      private static readonly object _lock = new object();
      
      internal _Reviver()
      {
        // When the domain is unloading, we should stop the _Reviver from reviving. Setting this
        // flag to true does the job.
        AppDomain.CurrentDomain.DomainUnload += (a, b) => _isUnloading = true;
        
        var thread = new Thread(_GcHappenedCaller);
        thread.Name = "GC Observer";
        thread.Start();
      }

      ~_Reviver()
      {
        // We only revive in normal garbage collections. When the domain is unloading we cannot
        // reviver, or els we won't allow the app to finish.
        if (_isUnloading)
          return;

        GC.ReRegisterForFinalize(this);
        _FireCleanup();
      }

      private void _FireCleanup()
      {
        bool lockTaken = false;
        try
        {
          // We cannot use a normal lock, as all other threads are stopped during a GC.
          // So, we try to get the lock. If we can't, we just skip it. This also avoids any issues
          // in a situation where we have 2 GCs happening in a very short time but our event
          // handlers still didn't finish doing their work.
          Monitor.TryEnter(_lock, ref lockTaken);

          if (lockTaken)
            Monitor.Pulse(_lock);
        }
        finally
        {
          if (lockTaken)
            Monitor.Exit(_lock);
        }
      }

      // This method needs to be static or else the thread would keep this object alive.
      private static void _GcHappenedCaller(object ignoredArgument)
      {
        var currentThread = Thread.CurrentThread;

        lock (_lock)
        {
          while (!_isUnloading)
          {
            currentThread.IsBackground = true;
            Monitor.Wait(_lock);

            lock (_gcHappened)
            {
              int count = _gcHappened.Count;
              if (count > 0)
              {
                currentThread.IsBackground = false;

                for (int i=count-1; i>=0; i--)
                {
                  var weakAction = _gcHappened[i];

                  if (!weakAction._TryInvoke())
                    _gcHappened.RemoveAt(i);
                }
              }
            }
          }
        }
      }
    }

    // A normal Action holds a Target and the MethodInfo to be invoked, but doesn't allow the Target
    // to be collected. The WeakAction also holds a Target and a MethodInfo to be invoked, but it
    // allows the Target to be collected.
    private struct _WeakAction
    {
      internal readonly WeakReference _targetReference;
      internal readonly MethodInfo _method;

      internal _WeakAction(Action action)
      {
        // target can be null when the method is static.
        var target = action.Target;

        if (target != null)
          _targetReference = new WeakReference(target);
        else
          _targetReference = null;
        
        _method = action.Method;
      }

      internal bool _TryInvoke()
      {
        if (_targetReference == null)
        {
          // If the method is static, we invoke it without issues.
          _method.Invoke(null, null);
          return true;
        }

        var target = _targetReference.Target;
        if (target == null)
          return false;

        _method.Invoke(target, null);
        return true;
      }
    }

    private static readonly List<_WeakAction> _gcHappened = new List<_WeakAction>();

    private static int _testOnly_initialCount;

    // These two are for testing purposes only.
    internal static int _TestOnly_GcHappenedHandlerCount
    {
      get
      {
        lock (_gcHappened)
          return _gcHappened.Count - _testOnly_initialCount;
      }
    }
    internal static void _TestOnly_SetupGcHappenedHandlerCount()
    {
      for (int i=0; i<2; i++)
      {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Thread.Sleep(100);
      }

      lock (_gcHappened)
        _testOnly_initialCount = _gcHappened.Count;
    }

    // As this event is internal, we can use Action as its type.
    // This implemenation is so odd because we keep all the handlers as "weak handlers". If they
    // need to be kept alive, they need to be either static, or part of an object that's going
    // to stay around.
    internal static event Action _GcHappened
    {
      add
      {
        if (value == null)
          return;

        var weakAction = new _WeakAction(value);
        lock (_gcHappened)
          _gcHappened.Add(weakAction);
      }
      remove
      {
        if (value == null)
          return;

        for (int i=_gcHappened.Count-1; i>=0; i--)
        {
          var weakAction = _gcHappened[i];

          if (value.Method != weakAction._method)
            continue;

          var targetReference = weakAction._targetReference;
          if (targetReference == null)
          {
            _gcHappened.RemoveAt(i);

            // The real C# events allow double registration, so we also assume that the same event
            // might be registered more than once and are unregistering only one.
            return;
          }

          if (targetReference.Target == value.Target)
          {
            _gcHappened.RemoveAt(i);
            return;
          }
        }
      }
    }
  }
}
