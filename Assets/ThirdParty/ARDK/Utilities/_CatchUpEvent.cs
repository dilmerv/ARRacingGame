// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Niantic.ARDK.Utilities
{
  // This is a helper class for events that should trigger even when the user registers to it
  // after it was originally fired.
  // This particular implementation will store the EventArgs used, and can be fired multiple times.
  // If the event is fired multiple times, a new event handler that gets registered will be fired
  // for all the values that this event was fired with.
  internal sealed class _CatchUpEvent<T>:
    IDisposable
  where
    T: struct, IArdkEventArgs
  {
    private readonly object _lock = new object();
    private List<T> _values;
    private ArdkEventHandler<T> _handler;
    private bool _isDisposed;

    public void Dispose()
    {
      lock (_lock)
      {
        _handler = null;
        _isDisposed = true;
      }
    }

    public void Register(ArdkEventHandler<T> handler)
    {
      if (handler == null)
        return;

      lock (_lock)
      {
        if (_isDisposed)
          return;

        // This is done inside the lock as we don't want to "register" to the handler if
        // concurrently we are being invoked as that could cause double-invokes.
        _handler += handler;

        var values = _values;
        if (values != null)
          foreach (var value in values)
            handler(value);
      }
    }

    public void Unregister(ArdkEventHandler<T> handler)
    {
      // There is no need to lock to remove an event handler, as that is thread safe already.
      _handler -= handler;
    }

    public void InvokeUsingCallbackQueue(T value)
    {
      ArdkEventHandler<T> handler;
      lock (_lock)
      {
        if (_isDisposed)
          return;

        if (_values == null)
          _values = new List<T>();
        
        _values.Add(value);
        handler = _handler;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (_isDisposed)
            return;

          if (handler != null)
            handler(value);
        }
      );
    }
  }
}