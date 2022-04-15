// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities;

using UnityEngine;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Utilities.VersionUtilities
{
  internal sealed class _NativeARDKVersion:
    _IARDKVersion
  {
    private bool _networkingConnected;
    private bool _isNativeNetworking;

    public _NativeARDKVersion()
    {
      MultipeerNetworkingFactory.NetworkingInitialized += OnNetworkingInitialized;
      _CallbackQueue.ApplicationWillQuit += OnApplicationQuit;
    }

    private void OnApplicationQuit()
    {
      _networkingConnected = false;
      MultipeerNetworkingFactory.NetworkingInitialized -= OnNetworkingInitialized;
    }

    private void OnNetworkingInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      args.Networking.Connected += _ =>
      {
        _networkingConnected = true;
        _isNativeNetworking = args.Networking is _NativeMultipeerNetworking;
      };
      args.Networking.Disconnected += _ => _networkingConnected = false;
    }

    public string GetARDKVersion()
    {
      var ptr = _NAR_VersionInfo_GetARDKVersion();
      return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
    }

    public string GetARBEVersion()
    {
      string version = null;

      if (_networkingConnected)
      {
        if (_isNativeNetworking)
        {
          var ptr = _NAR_VersionInfo_GetARBEVersion();
          version = ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }
        else
        {
          version = "Editor";
        }
      }

      return string.IsNullOrEmpty(version) ? "Unavailable" : version;
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NAR_VersionInfo_GetARDKVersion();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NAR_VersionInfo_GetARBEVersion();
  }
}
