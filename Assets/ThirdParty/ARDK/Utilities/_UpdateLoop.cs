// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  /// <summary>
  /// A class to inject callbacks into unity update loops.
  /// </summary>
  [DefaultExecutionOrder(Int32.MinValue)]
  internal sealed class _UpdateLoop: 
    MonoBehaviour
  {
    private bool _createdByStaticInitializer;

    /// <summary>
    /// Event for standard unity tick.
    /// @Note: Tick is always called before other objects' Update, so if you subscribe in Update to both
    /// Tick and LateTick, LateTick will run first, on the frame that you subscribe.
    /// </summary>
    public static event Action Tick = () => {};

    /// <summary>
    /// Event for standard unity late tick.
    /// </summary>
    public static event Action LateTick = () => {};

    /// <summary>
    /// Event for standard unity fixed tick.
    /// </summary>
    public static event Action FixedTick = () => {};

    static _UpdateLoop()
    {
      var go = new GameObject("__update_loop__", typeof(_UpdateLoop));
      go.hideFlags = HideFlags.HideInHierarchy; 

      // Can only use DontDestroyOnLoad in play mode, but _UpdateLoop is used
      // in edit mode tests as well
      if (Application.isPlaying)
        DontDestroyOnLoad(go);

      go.GetComponent<_UpdateLoop>()._createdByStaticInitializer = true;
    }

    private void Start()
    {
      if (!_createdByStaticInitializer)
        Destroy(this);
    }

    private void Update()
    {
      Tick();
    }

    private void LateUpdate()
    {
      LateTick();
    }

    private void FixedUpdate()
    {
      FixedTick();
    }
  }
}
