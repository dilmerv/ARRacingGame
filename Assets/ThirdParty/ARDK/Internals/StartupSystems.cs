// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif
#if (UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_DESKTOP) && !UNITY_EDITOR
#define AR_NATIVE_SUPPORT
#endif

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Configuration.Authentication;

using Niantic.ARDK.Configuration;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace Niantic.ARDK.Internals
{
  /// Controls the startup systems for ARDK.
  public static class StartupSystems
  {
#if UNITY_EDITOR_OSX
    [InitializeOnLoadMethod]
    private static void EditorStartup()
    {
#if !REQUIRE_MANUAL_STARTUP
      ManualStartup();
#endif
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Startup()
    {
#if AR_NATIVE_SUPPORT
#if !REQUIRE_MANUAL_STARTUP
      ManualStartup();
#endif
#endif
    }

    /// <summary>
    /// Starts up the ARDK startup systems if they haven't been started yet.
    /// </summary>
    public static void ManualStartup()
    {
#if (AR_NATIVE_SUPPORT || UNITY_EDITOR_OSX)
      try
      {
        _ROR_CREATE_STARTUP_SYSTEMS();
        SetAuthenticationParameters();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to create ARDK startup systems: {0}", false, e);
      }
#endif
    }

    private static void SetAuthenticationParameters()
    {
      // We always try to find an api key
      var apiKey = "";
      var authConfigs = Resources.LoadAll<ArdkAuthConfig>("ARDK/ArdkAuthConfig");
      bool haveSetNewApiKey = false;
      foreach (var authConfig in authConfigs)
      {
        if (haveSetNewApiKey)
        {
          Resources.UnloadAsset(authConfig);
          continue;
        }

        apiKey = authConfig.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
          ArdkGlobalConfig.SetApiKey(apiKey);
          haveSetNewApiKey = true;
        }

        Resources.UnloadAsset(authConfig);
      }

      if (authConfigs.Length > 1)
      {
        var errorMessage = "There are multiple ArdkAuthConfigs in Resources/ARDK/ " +
                           "directories, loading the first API key found. Remove extra" +
                           " ArdkAuthConfigs to prevent API key problems";
        ARLog._Error(errorMessage);
      }
      else if (authConfigs.Length == 0)
      {
        ARLog._Error
        (
          "Could not load an ArdkAuthConfig, please add one under Resources/ARDK/"
        );
      }

      /// Only continue if needed
      if (!ServerConfiguration.AuthRequired)
      {
        return;
      }

      if (string.IsNullOrEmpty(ServerConfiguration.ApiKey))
      {

        if (!string.IsNullOrEmpty(apiKey))
        {
          ServerConfiguration.ApiKey = apiKey;
        }
        else
        {
          ARLog._Error
          (
            "No API Key was found, please add one to the ArdkAuthConfig in Resources/ARDK/"
          );
        }
      }

      var authUrl = ArdkGlobalConfig.GetAuthenticationUrl();
      if (string.IsNullOrEmpty(authUrl))
      {
        ArdkGlobalConfig.SetAuthenticationUrl(ArdkGlobalConfig._DEFAULT_AUTH_URL);
        authUrl = ArdkGlobalConfig.GetAuthenticationUrl();
      }

      ServerConfiguration.AuthenticationUrl = authUrl;

#if UNITY_EDITOR
      if (!string.IsNullOrEmpty(apiKey))
      {
        var authResult = ArdkGlobalConfig.VerifyApiKeyWithFeature("unity_editor");
        if(authResult == NetworkingErrorCode.Ok)
          ARLog._Debug("Successfully authenticated ARDK Api Key");
        else
        {
          ARLog._Error("Attempted to authenticate ARDK Api Key, but got error: " + authResult);
        }
      }
#endif
      
    }

    // TODO(bpeake): Find a way to shutdown gracefully and add shutdown here.

#if (AR_NATIVE_SUPPORT || UNITY_EDITOR_OSX)
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _ROR_CREATE_STARTUP_SYSTEMS();
#endif
  }
}
