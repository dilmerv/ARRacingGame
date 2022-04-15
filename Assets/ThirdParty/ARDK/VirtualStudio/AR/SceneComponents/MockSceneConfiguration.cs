// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.VirtualStudio.Remote;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  public sealed class MockSceneConfiguration:
    MonoBehaviour
  {
    private void Awake()
    {
#if UNITY_EDITOR
      if (_RemoteConnection.IsEnabled)
        RemoveFromScene();
      else
        ARSessionFactory.SessionInitialized += OnSessionInitialized;
#else
      RemoveFromScene();
#endif
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= OnSessionInitialized;
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (args.Session.RuntimeEnvironment != RuntimeEnvironment.Mock)
        RemoveFromScene();
    }

    private void RemoveFromScene()
    {
      Destroy(gameObject);
    }
  }
}

