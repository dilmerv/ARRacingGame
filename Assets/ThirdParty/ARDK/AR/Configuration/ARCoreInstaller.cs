// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;

namespace Niantic.ARDK.AR.Configuration
{
  /// @brief A tool that installs ARCore if it's not already installed.
  /// @remark This class exposes the ArCoreApk_requestInstallCustom method from ARCore, documented here:
  /// https://developers.google.com/ar/reference/c/group/ar-core-apk#arcoreapk_requestinstallcustom
  public class ARCoreInstaller
  {
    public enum InstallBehavior
    {
      // This means the user prompt will have a cancel button and can be refused.
      Optional = 0,

      // This means the user prompt cannot be refused (though the user can still avoid downloading
      // arcore by closing and reopening the app).
      Required = 1,
    };

    public enum InstallMessageType
    {
      // This tells the user that ARCore is required for a feature of your app.
      Feature = 0,

      // This tells the user that ARCore is required for the entire app.
      Application = 1,

      // This tells the user nothing about why ARCore is being requested, assuming you've already
      // informed them.
      UserAlreadyInformed = 2,
    };


    public enum InstallResult
    {
      // This means that ARCore is installed and up to date.
      Installed = 0,

      // The user is being sent to the play store to update/install ARCore.
      PromptingUserToUpdate = 1,

      // The user has rejected the install/update request.
      RejectedByUser = 2,

      // The request failed for some reason unrelated to the user.
      Failed = 3,
    }

    /// Calling this method with shouldPromptUserToUpdate will either return that ARCore is already
    /// installed, or send the user to the play store to install it. Calling it with
    /// shouldPromptUserToUpdate as false will either return that ARCore is successfully installed
    /// or will say that the user declined to install it.
    /// If the user is sent to the play store to install ARCore, this activity will pause. This will
    /// need to be called again after the activity has resumed to check the results.
    public static InstallResult RequestInstallARCore
    (
      bool shouldPromptUserToUpdate,
      InstallBehavior installBehavior,
      InstallMessageType messageType
    )
    {
      var result = InstallResult.Installed;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        result = (InstallResult)_NARConfiguration_RequestInstallARCore
        (
          shouldPromptUserToUpdate ? (byte)1 : (byte)0,
          (uint)installBehavior,
          (uint)messageType
        );
      }

      return result;
    }

    [DllImport(_ARDKLibrary.libraryName)]
    static extern uint _NARConfiguration_RequestInstallARCore
    (
      byte sendUserToInstall,
      uint installBehavior,
      uint messageType
    );
  }
}
