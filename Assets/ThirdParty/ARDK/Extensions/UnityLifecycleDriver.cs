// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Security.Cryptography;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  // Stub class to allow sealing of Unity lifecycle methods
  public abstract class UnityLifecycleDriverBase:
    MonoBehaviour
  {
    protected virtual void Awake()
    {
    }

    protected virtual void Start()
    {
    }

    protected virtual void OnDestroy()
    {
    }

    protected virtual void OnEnable()
    {
    }

    protected virtual void OnDisable()
    {
    }
  }

  /// Base class for ARDK's extension MonoBehaviour extension classes. All Unity lifecycle
  /// methods are sealed to prevent child classes from implementing functionality in them;
  /// functionality is instead kept inside virtual methods so the class can be controlled both
  /// by the Unity lifecycle and independently (via pure scripting).
  public abstract class UnityLifecycleDriver:
    UnityLifecycleDriverBase
  {
    /// If true, this component's lifecycle will be tied to Unity's lifecycle methods.
    ///   * Unity's Awake calls Initialize
    ///   * Unity's OnDestroy calls Remove
    ///   * Unity's OnEnable calls EnableFeatures
    ///   * Unity's OnDisable calls Disable
    /// @note
    ///   Setting this to false means solely application code is in charge of calling the Initialize,
    ///   EnableFeatures, and DisableFeatures methods. The Deinitialize method will always be called
    ///   when this component is destroyed.
    /// @note
    ///   False by default so that EnableFeatures isn't automatically called if this
    ///   component is instantiated in script.
    [SerializeField]
    [Tooltip("If true, this component's lifecycle will be tied to Unity's lifecycle methods.")]
    private bool _manageUsingUnityLifecycle = false;

    private _ThreadCheckedObject _threadChecker;
    private bool _initialized = false;
    private bool _deinitialized;

    /// Value is true if this component has been initialized.
    public bool Initialized
    {
      get
      {
        return _initialized;
      }
      private set
      {
        _initialized = value;
      }
    }

    /// Value is true if this component can be validly initialized.
    public bool CanInitialize
    {
      get { return !Initialized && (!_deinitialized || (_deinitialized && _CanReinitialize)); }
    }

    private bool _areFeaturesEnabled = false;

    /// Value is true if this component is enabled. A subclass may gate certain behaviours based
    /// on this value.
    public bool AreFeaturesEnabled
    {
      get
      {
        return _areFeaturesEnabled;
      }
      private set
      {
        _areFeaturesEnabled = value;
      }
    }

    // For use in internal testing only
    internal bool _ManageUsingUnityLifecycle
    {
      get { return _manageUsingUnityLifecycle; }
      set { _manageUsingUnityLifecycle = value; }
    }

    protected virtual bool _CanReinitialize
    {
      get { return false; }
    }

#region LifecycleManagementMethods
    /// Prepares the instance for use. This is where it will gather all the resources it needs as
    /// defined by a subclass in InitializeImpl.
    public void Initialize()
    {
      _threadChecker?._CheckThread();

      if (Initialized)
      {
        ARLog._Warn("This component is already initialized.");
        return;
      }

      if (_deinitialized && !_CanReinitialize)
      {
        ARLog._Warn("Once deinitialized, this component cannot be re-initialized.");
        return;
      }

      Initialized = true;
      InitializeImpl();
    }

    /// Releases any resources held by the instance as defined by a subclass in DeinitializeImpl.
    /// Once this is called, Initialize can't be called. Instead a new instance must be made.
    public void Deinitialize()
    {
      _threadChecker?._CheckThread();

      if (!Initialized)
        return;

      DisableFeatures();

      Initialized = false;
      _deinitialized = true;

      DeinitializeImpl();
    }

    /// Enabled any features controlled by this instance as defined by a subclass in
    /// EnableFeaturesImpl. This will initialize the instance if it wasn't already.
    public void EnableFeatures()
    {
      _threadChecker?._CheckThread();

      // Allow this function to be called multiple times without repeating side effects.
      if (AreFeaturesEnabled)
        return;

      // Ensure this object is already initialized and fail if it can't.
      Initialize();

      if (!Initialized)
        return;

      AreFeaturesEnabled = true;

      EnableFeaturesImpl();
    }

    /// Disable any features controlled by the instance as defined by a subclass in
    /// DisableFeaturesImpl.
    public void DisableFeatures()
    {
      _threadChecker?._CheckThread();

      if (!AreFeaturesEnabled)
        return;

      // There is no need to check the initialization state as an enabled instance is by definition
      // initialized.

      AreFeaturesEnabled = false;

      DisableFeaturesImpl();
    }
#endregion

#region UnityLifecycleIntegration
    protected sealed override void Awake()
    {
      _threadChecker = new _ThreadCheckedObject();

      if (_ManageUsingUnityLifecycle)
        Initialize();
    }

    protected sealed override void Start()
    {
    }

    protected sealed override void OnDestroy()
    {
      Deinitialize();
    }

    protected sealed override void OnEnable()
    {
      if (_ManageUsingUnityLifecycle)
        EnableFeatures();
    }

    protected sealed override void OnDisable()
    {
      if (_ManageUsingUnityLifecycle)
        DisableFeatures();
    }
#endregion

    /// @note If overriding in a subclass, make sure to call this base method.
    protected virtual void InitializeImpl()
    {
      _threadChecker?._CheckThread();
    }

    protected virtual void DeinitializeImpl()
    {
      _threadChecker?._CheckThread();
    }

    protected virtual void EnableFeaturesImpl()
    {
      _threadChecker?._CheckThread();
    }

    protected virtual void DisableFeaturesImpl()
    {
      _threadChecker?._CheckThread();
    }

    // Called when the user hits the Reset button in the Inspector's context menu or when
    // adding the component the first time. This function is only called in editor mode.
    // Used to give good default values in the Inspector.
    protected virtual void Reset()
    {
      _manageUsingUnityLifecycle = true;
    }
  }
}
