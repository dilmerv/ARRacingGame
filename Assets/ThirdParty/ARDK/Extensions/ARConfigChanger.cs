// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;

namespace Niantic.ARDK.Extensions
{
  /// Extension for UnityLifecycleDriver that allows inheritors to modify the ARSession's
  /// configuration, through the use of ARSessionChangesCollector. Configuration info
  /// is asked for by the ARSession whenever it runs, and this class may additionally be used
  /// to update the configuration after the session has already ran.
  public abstract class ARConfigChanger:
    UnityLifecycleDriver
  {
    private ARSessionChangesCollector _changesCollector;

    internal event Action _ConfigurationChanged;
    
    /// Inheritors should override this to modify session configuration settings based
    /// on their script's needs.
    /// 
    /// @note This is executed as a result of the ARSession being run, which may or may not be
    ///   triggered by a call to RaiseConfigurationChanged().
    public abstract void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    );
    
    /// Inheritors should call this function to alert the session that the configuration
    /// has changed, and will result in ApplyARConfigurationChange() being called.
    protected void RaiseConfigurationChanged()
    {
      _ConfigurationChanged?.Invoke();
    }

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      ARSessionFactory.SessionInitialized += SetConfigChangesCollector;
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      ARSessionFactory.SessionInitialized -= SetConfigChangesCollector;
      _changesCollector?.Unregister(this);
    }

    private void SetConfigChangesCollector(AnyARSessionInitializedArgs args)
    {
      var arSession = (_IARSession) args.Session;
      _changesCollector = arSession.ARSessionChangesCollector;
      _changesCollector.Register(this);

      // The session's ARSessionChangesCollector is destroyed when the session is disposed.
      arSession.Deinitialized +=
        _ =>
        {
          _changesCollector?.Unregister(this);
          _changesCollector = null;
        };
    }
  }
}
