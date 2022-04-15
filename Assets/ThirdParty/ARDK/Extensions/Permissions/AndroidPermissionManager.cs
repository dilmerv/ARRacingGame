// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
using System.Collections.Generic;
using UnityEngine.Android;

namespace Niantic.ARDK.Extensions.Permissions
{
  /// Static helper for requesting permissions at runtime.
  public static class AndroidPermissionManager
  {
    /// Android's permission system needs specific strings to be entered.
    /// This enum table assigns each permission the appropriate string it needs for the functions.
    private static readonly Dictionary<ARDKPermission, string> AndroidPermissionString =
      new Dictionary<ARDKPermission, string>
      {
        { ARDKPermission.Camera, Permission.Camera },
        { ARDKPermission.Microphone, Permission.Microphone },
        { ARDKPermission.FineLocation, Permission.FineLocation },
        { ARDKPermission.CoarseLocation, Permission.CoarseLocation },
        { ARDKPermission.ExternalStorageRead, Permission.ExternalStorageRead },
        { ARDKPermission.ExternalStorageWrite, Permission.ExternalStorageWrite },
      };

    /// Request a single Android permission.
    /// @note This can still be safely called on other platforms, but will do nothing.
    public static void RequestPermission(ARDKPermission permission)
    {
      if (!Permission.HasUserAuthorizedPermission(AndroidPermissionString[permission]))
        Permission.RequestUserPermission(AndroidPermissionString[permission]);
    }

    public static bool HasPermission(ARDKPermission permission)
    {
      return Permission.HasUserAuthorizedPermission(AndroidPermissionString[permission]);
    }
  }
}
#endif

