// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;

using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Editor
{
  // UI for connecting to the ARDK Remote Feed app
  [Serializable]
  internal sealed class _RemoteConfigurationEditor
  {
    private const string REMOTE_METHOD = "ARDK_Connection_Method";
    private const string REMOTE_PIN = "ARDK_Pin";

    [SerializeField]
    private _RemoteConnection.ConnectionMethod _connectionMethod;

    [SerializeField]
    private string _pin;

    [SerializeField]
    private int _imageCompression;

    [SerializeField]
    private int _imageFramerate;

    [SerializeField]
    private int _awarenessFramerate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Startup()
    {
      if (!_RemoteConnection.IsEnabled)
        return;

      if (Application.isPlaying)
      {
        var pin = PlayerPrefs.GetString(REMOTE_PIN, "").ToUpper();
        var connectionMethod =
          (_RemoteConnection.ConnectionMethod) PlayerPrefs.GetInt
          (
            REMOTE_METHOD,
            (int)_RemoteConnection.ConnectionMethod.Internet
          );

        if (!string.IsNullOrEmpty(pin) || connectionMethod == _RemoteConnection.ConnectionMethod.USB)
        {
          _RemoteConnection.InitIfNone(connectionMethod);
          _RemoteConnection.Connect(pin);
        }
        else
        {
          ARLog._Release("Unable to create remote connection: No pin entered for Internet connection");
        }
      }
    }

    public void LoadPreferences()
    {
      _pin = PlayerPrefs.GetString(REMOTE_PIN, "");

      _connectionMethod =
        (_RemoteConnection.ConnectionMethod)PlayerPrefs.GetInt
        (
          REMOTE_METHOD,
          (int)_RemoteConnection.ConnectionMethod.Internet
        );

      _imageCompression = _RemoteBufferConfiguration.ImageCompression;
      _imageFramerate = _RemoteBufferConfiguration.ImageFramerate;
      _awarenessFramerate = _RemoteBufferConfiguration.AwarenessFramerate;
    }

    public void OnSelectionChange(bool isSelected)
    {
      _RemoteConnection.IsEnabled = isSelected;
    }

    public void DrawGUI()
    {
      EditorGUI.BeginDisabledGroup(Application.isPlaying);

      var newConnectionMethod =
        (_RemoteConnection.ConnectionMethod) EditorGUILayout.EnumPopup("Method:", _connectionMethod);

      if (newConnectionMethod != _connectionMethod)
      {
        _connectionMethod = newConnectionMethod;
        PlayerPrefs.SetInt(REMOTE_METHOD, (int)_connectionMethod);
      }

      var newPin = EditorGUILayout.TextField("PIN:", _pin);
      if (newPin != _pin)
      {
        _pin = newPin;
        PlayerPrefs.SetString(REMOTE_PIN, newPin);
      }

      var newImageCompression = EditorGUILayout.IntField("Image Compression:", _imageCompression);
      if (newImageCompression != _imageCompression)
      {
        _imageCompression = newImageCompression;
        _RemoteBufferConfiguration.ImageCompression = newImageCompression;
      }

      var newImageFramerate = EditorGUILayout.IntField("Image Framerate:", _imageFramerate);
      if (newImageFramerate != _imageFramerate)
      {
        _imageFramerate = newImageFramerate;
        _RemoteBufferConfiguration.ImageFramerate = newImageFramerate;
      }

      var newAwarenessFramerate = EditorGUILayout.IntField("Awareness Framerate:", _awarenessFramerate);
      if (newAwarenessFramerate != _imageFramerate)
      {
        _awarenessFramerate = newAwarenessFramerate;
        _RemoteBufferConfiguration.AwarenessFramerate = newAwarenessFramerate;
      }

      EditorGUI.EndDisabledGroup();

      GUIStyle s = new GUIStyle(EditorStyles.largeLabel);
      s.fontSize = 20;
      s.fixedHeight = 30;

      if (Application.isPlaying)
      {
        EditorGUILayout.LabelField(_RemoteConnection.CurrentConnectionMethod.ToString());

        if (!_RemoteConnection.IsReady)
        {
          if (_RemoteConnection.IsEnabled)
          {
            s.normal.textColor = Color.magenta;
            EditorGUILayout.LabelField("Waiting for Remote Connection to be ready...", s);
          }
          else
          {
            s.normal.textColor = Color.gray;
            EditorGUILayout.LabelField("Not active...", s);
          }
        }
        else if (!_RemoteConnection.IsConnected)
        {
          s.normal.textColor = Color.blue;
          EditorGUILayout.LabelField("Waiting for remote device to connect...", s);
        }
        else
        {
          s.normal.textColor = Color.green;
          EditorGUILayout.LabelField("Connected!", s);
        }
      }
      else
      {
        if (_RemoteConnection.IsEnabled)
          EditorGUILayout.LabelField("Waiting for play mode...", s);
        else
          EditorGUILayout.LabelField("Not using remote...", s);
      }
    }
  }
}
