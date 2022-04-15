// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Extensions;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.AR.Configuration
{
  /// An instance of this class is owned by an ARSession.
  /// ARConfigChanger instances Register to the ARConfigChangerCollector of the ARSession they need
  /// to affect, and when ARSession.Run is called, all registered ARConfigChangers will have their
  /// changes collected.
  ///
  /// ARConfigChangers can also request the ARSession be re-run (and so config changes re-collected)
  /// by invoking their _ConfigurationChanged event. Re-runs will be triggered at max once per frame.
  public sealed class ARSessionChangesCollector
  {
    public sealed class ARSessionRunProperties
    {
      public IARConfiguration ARConfiguration;
      public ARSessionRunOptions RunOptions;

      public ARSessionRunProperties
      (
        IARConfiguration arConfiguration,
        ARSessionRunOptions runOptions
      )
      {
        ARConfiguration = arConfiguration;
        RunOptions = runOptions;
      }
    }
    private _IARSession _arSession;

    private Action<ARSessionRunProperties> _configChangesRequested;

    private bool _observerRequestedCollection;

    private IARConfiguration _preChangeConfiguration;

    internal ARSessionChangesCollector(_IARSession arSession)
    {
      _arSession = arSession;
      _arSession.Deinitialized += _ => _arSession = null;
      _UpdateLoop.Tick += RerunIfRequested;
    }

    internal void _CollectChanges(IARConfiguration arConfiguration, ref ARSessionRunOptions runOptions)
    {
      _preChangeConfiguration = ARWorldTrackingConfigurationFactory.Create();
      arConfiguration.CopyTo(_preChangeConfiguration);

      var properties = new ARSessionRunProperties(arConfiguration, runOptions);
      _configChangesRequested?.Invoke(properties);

      runOptions = properties.RunOptions;

      _observerRequestedCollection = false;
    }

    public void Register(ARConfigChanger changer)
    {
      ARLog._DebugFormat("Registered {0} as ARConfigChanger.", objs: changer.GetType());
      _configChangesRequested += changer.ApplyARConfigurationChange;

      // Rerun will only occur if the ARSession.State is Running in the next Unity Update frame.
      changer._ConfigurationChanged += RequestCollection;
    }

    public void Unregister(ARConfigChanger changer)
    {
      ARLog._DebugFormat("Unregistered {0} as ARConfigChanger.", objs: changer.GetType());
      _configChangesRequested -= changer.ApplyARConfigurationChange;
      changer._ConfigurationChanged -= RequestCollection;
    }

    private void RequestCollection()
    {
      _observerRequestedCollection = true;
    }

    private void RerunIfRequested()
    {
      if (_observerRequestedCollection && _arSession != null)
      {
        _observerRequestedCollection = false;

        if (_arSession.State != ARSessionState.Running)
          return;

        var configCopy = ARWorldTrackingConfigurationFactory.Create();
        _preChangeConfiguration.CopyTo(configCopy);

        // The ARSession will call _CollectChanges.
        // Keep the ARConfiguration from the last run, but have a fresh slate for run options.
        // Just because a dev wanted to reset tracking on the previous run doesn't mean they want
        // changing plane detection, for example, to reset tracking again.
        _arSession.Run(configCopy, ARSessionRunOptions.None);
      }
    }
  }
}
