// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Utilities.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  /// A helper class to ensure callbacks from native code get run on Unity's main thread.
  /// @note You *do not* need to interact directly with this class, it will manually inject an
  /// invisible, immortal game object to flush callbacks by itself.
  // TODO: As the class is internal, it should be named _CallbackQueue.
  internal sealed class _CallbackQueue:
    MonoBehaviour
  {
    /// The _callbackQueue is a buffer for callback-handlers coming-in from ARDK.
    /// We need this because our callbacks occur in a different thread/update cycle than Unity's
    /// main thread.
    private static readonly Queue<Action> _callbackQueue = new Queue<Action>();

    /// <summary>
    /// Internal flag for application shutting down.
    /// </summary>
    // TODO: This should be renamed to _isApplicationQuitting, as the current name looks more like
    // a public event.
    internal static bool ApplicationQuitting;

#if UNITY_EDITOR
    /// Run so that the EditorRemoteConnector can start up a networking session while the Editor is
    /// in edit mode.
    /// TODO: refactor to be confined to EditorRemoteConnector
    [InitializeOnLoadMethod]
    private static void _OnEditorLoad()
    {
      EditorApplication.update += _ConsumeQueue;
    }
#endif

    /// Called when Unity is first initializing right after a game starts, before a scene loads.
    /// We create a game object that is invisible and won't be destroyed by switching scenes. This
    /// becomes our link into Unity's main run loop where we can flush our callbacks ;)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void _OnLoad()
    {
      var sneakySnake = new GameObject();
      sneakySnake.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;

      DontDestroyOnLoad(sneakySnake);
      sneakySnake.AddComponent<_CallbackQueue>();
    }

    internal static void _ConsumeQueue()
    {
      Action[] actions;

      var exceptionOccurred = false;
      lock (_callbackQueue)
      {
        actions = _callbackQueue.ToArray();
        _callbackQueue.Clear();
      }

      foreach (var action in actions)
      {
        try
        {
          action();
        }
        catch (Exception e)
        {
          exceptionOccurred = true;
          ARLog._Exception(e);
        }
      }

      if (exceptionOccurred)
      {
        var message =
          "An exception occurred in a method subscribed to an ARDK event, check the device logs " +
          "for more information";
        
        throw new Exception(message);
      }
    }

    /// Queue a new callback to be called on Unity's main thread.
    /// @param callback The callback to be called on Unity's main thread.
    public static void QueueCallback(Action callback)
    {
      if (callback == null)
        return;

      lock (_callbackQueue)
        _callbackQueue.Enqueue(callback);
    }

    internal static event Action ApplicationWillQuit;

    /// In this Update method, we call all of the relevant callback functions subscribed to the
    /// ARSession. Meaning that if any of those callback functions are very heavy, they will slow
    /// down your app's performance and may have frame drops. Unfortunately, Unity doesn't have
    /// great insight into the callbacks this method calls, so you may have to manually inspect
    /// each function subscribed to an event.
    private void Update()
    {
      _ConsumeQueue();
    }

    private void OnApplicationQuit()
    {
      if (ApplicationQuitting)
      {
        // Prevents the multiple invokations of this method that are happening for some reason
        return;
      }

      ApplicationQuitting = true;

      var handler = ApplicationWillQuit;
      if (handler != null)
        handler();
    }

    private void OnDestroy()
    {
      if (!ApplicationQuitting)
        ARLog._Error("Callback Queue Destroyed. This should not happen.");
    }
  }
}
