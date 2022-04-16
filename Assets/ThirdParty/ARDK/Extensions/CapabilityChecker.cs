// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Events;

namespace Niantic.ARDK.Extensions
{
  [DisallowMultipleComponent]
  public class CapabilityChecker: UnityLifecycleDriver
  {
    public bool HasSucceeded { get; private set; }

    [Serializable]
    public enum FailureType
    {
      Error,
      DeviceIncapable,
      Timeout,
      DeviceNotUpToDate,
      ErrorDuringInstallation,
      UserRejectedInstallation,
    }

    [Serializable]
    public struct FailureReason
    {
      public FailureType type;
      public string message;
    }

    [Serializable]
    public class FailureEvent: UnityEvent<FailureReason> {}

    [Serializable]
    public class SuccessEvent: UnityEvent {}

    public SuccessEvent Success = new SuccessEvent();
    public FailureEvent Failure = new FailureEvent();

    private void Succeed()
    {
      ARLog._Debug("Capability check succeeded ");
      HasSucceeded = true;
      Success.Invoke();
    }

    private void Fail(FailureType type, string message)
    {
      ARLog._Debug(message);
      var reason =
        new FailureReason
        {
          type = type,
          message = message,
        };

      Failure.Invoke(reason);
    }

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      ValidateSuccessListeners();
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      if (!HasSucceeded)
        CheckCapability();
    }

    /// Asynchronously checks whether the device is AR-capable. Reports the result through either
    /// the Success or Failure event and the HasSucceeded property.
    public void CheckCapability()
    {
      ARWorldTrackingConfigurationFactory.CheckCapabilityAndSupport
      (
        (hardwareCapability, softwareSupport) =>
        {
          switch (hardwareCapability)
          {
            case ARHardwareCapability.NotCapable:
              Fail
              (
                FailureType.DeviceIncapable,
                "ARDK: Device not capable of world tracking!"
              );

              return;

            case ARHardwareCapability.CheckFailedWithError:
              Fail
              (
                FailureType.Error,
                "ARDK: Unable to check for compatibility due to some error! Check logs for ARCore issues."
              );

              return;

            case ARHardwareCapability.CheckFailedWithTimeout:
              Fail
              (
                FailureType.Timeout,
                "ARDK: Unable to check for compatibility due to network timeout! Make sure you are connected to the Internet and try again later!"
              );

              return;

            case ARHardwareCapability.Capable:
              // We're good!
              break;

            default:
              throw new ArgumentOutOfRangeException
              (
                nameof(hardwareCapability),
                hardwareCapability,
                null
              );
          }

          // We know now that our hardware is capable! Time to check the software
          switch (softwareSupport)
          {
            case ARSoftwareSupport.NotSupported:
              string message;
#if UNITY_IOS
            message =
                "ARDK: Your current iOS version is not supported! Please upgrade to at least iOS 11 to enjoy this feature.";
#elif UNITY_ANDROID
              message =
                "ARDK: Your current Android system version is not supported! Please upgrade to a version supported by ArCore.";
#else
              message =
                "ARDK: Your current system is not supported! AR features are only supported on iOS/Android";
#endif
              Fail
              (
                FailureType.DeviceIncapable,
                message
              );

              return;

            case ARSoftwareSupport.SupportedNeedsUpdate:
#if UNITY_IOS
              Fail
              (
                FailureType.DeviceNotUpToDate,
                "ARDK: Your current iOS version is out of date! Please update to enjoy this feature."
              );
#elif UNITY_ANDROID
              InstallARCore();
#endif
              return;

            case ARSoftwareSupport.Supported:
              // We're good!
              break;

            default:
              throw new ArgumentOutOfRangeException(nameof(softwareSupport), softwareSupport, null);
          }

          Succeed();
        }
      );
    }

    private void InstallARCore()
    {
      var installationResult = ARCoreInstaller.RequestInstallARCore
      (
        true,
        ARCoreInstaller.InstallBehavior.Optional,
        ARCoreInstaller.InstallMessageType.Application
      );

      switch (installationResult)
      {
        case ARCoreInstaller.InstallResult.Failed:
        {
          Fail
          (
            FailureType.ErrorDuringInstallation,
            "Failed to install ARCore."
          );

          break;
        }

        case ARCoreInstaller.InstallResult.Installed:
        {
          Fail
          (
            FailureType.ErrorDuringInstallation,
            "ARCore installation needed, but ARCore is already installed."
          );

          break;
        }

        case ARCoreInstaller.InstallResult.RejectedByUser:
        {
          Fail
          (
            FailureType.UserRejectedInstallation,
            "User refused to install ARCore."
          );

          break;
        }

        case ARCoreInstaller.InstallResult.PromptingUserToUpdate:
        {
          Debug.Log("Sending the user to the ARCore installation popup.");
          _havePromptedTheUserToInstallARCore = true;
          break;
        }
      }
    }

    private bool _havePromptedTheUserToInstallARCore = false;

    private void OnApplicationPause(bool pauseStatus)
    {
      if (_havePromptedTheUserToInstallARCore && !pauseStatus)
      {
        var installationResult = ARCoreInstaller.RequestInstallARCore
        (
          true,
          ARCoreInstaller.InstallBehavior.Optional,
          ARCoreInstaller.InstallMessageType.Application
        );

        switch (installationResult)
        {
          case ARCoreInstaller.InstallResult.Failed:
          {
            Fail
            (
              FailureType.ErrorDuringInstallation,
              "Failed to install ARCore."
            );

            return;
          }

          case ARCoreInstaller.InstallResult.Installed:
          {
            break;
          }

          case ARCoreInstaller.InstallResult.RejectedByUser:
          {
            Fail
            (
              FailureType.UserRejectedInstallation,
              "User refused to install ARCore."
            );

            return;
          }

          case ARCoreInstaller.InstallResult.PromptingUserToUpdate:
          {
            Fail
            (
              FailureType.ErrorDuringInstallation,
              "Somehow re-prompting the user about installing ARCore."
            );

            return;
          }
        }

        Succeed();
      }
    }

    private int _successListenersCount = 0;
    private void OnValidate()
    {
      if (Success.GetPersistentEventCount() > _successListenersCount)
      {
        ValidateSuccessListeners();
      }
    }

    private void ValidateSuccessListeners()
    {
      for (var i = 0; i < Success.GetPersistentEventCount(); i++)
      {
        var target = Success.GetPersistentTarget(i);
        if (target.GetType() == typeof(ARSessionManager))
        {
          ARLog._Error
          (
            "ARSessionManager is set up to create an ARSession after the capability check passes, " +
            "and should not be subscribed to the CapabilityChecker.Success event separately."
          );
        }
      }
    }
  }
}
