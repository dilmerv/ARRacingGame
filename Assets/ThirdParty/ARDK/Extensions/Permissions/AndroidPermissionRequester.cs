// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif

namespace Niantic.ARDK.Extensions.Permissions
{
  /// Permission types ARDK supports requesting from the user
  public enum ARDKPermission
  {
    Camera,
    Microphone,
    FineLocation,
    CoarseLocation,
    ExternalStorageRead,
    ExternalStorageWrite
  }

  /// Quick solution for requesting permissions from an Android device. We recommend replacing this
  /// component with a better solution that follows iOS and Android's best practices for
  /// requesting solutions.
  /// @note Other MonoBehaviour's Start methods will get called before the permission flow finishes,
  /// so it isn't safe to initialize ARDK resources in Start that depend on the result of this
  /// request.
  /// @note Permission requests will pop up on iOS devices when a app starts a certain service
  /// that requires an ungranted permission.
  public class AndroidPermissionRequester: MonoBehaviour
  {
    // If we're not using these, we get warnings about them not being used. However, we don't want
    // to completely hide the fields, because that might cause Unity to delete the serialized values
    // on other platforms, which would reset the data on the prefab back to the defaults. So, we just
    // squelch "unused variable" warnings here.
#pragma warning disable CS0414
    [SerializeField]
    private ARDKPermission[] _permissions = null;

    [SerializeField]
    private bool _requestOnUpdate = false;
#pragma warning restore CS0414

#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
    void Start()
    {
      RequestPermissions();
    }

    void Update()
    {
      if (_requestOnUpdate)
        RequestPermissions();
    }

    private void RequestPermissions()
    {
      var requestMade = false;
      foreach (var permission in _permissions)
      {
        if (!AndroidPermissionManager.HasPermission(permission))
        {
          AndroidPermissionManager.RequestPermission(permission);
          requestMade = true;
        }
      }

      _requestOnUpdate = requestMade;
    }
#endif
  }
}
