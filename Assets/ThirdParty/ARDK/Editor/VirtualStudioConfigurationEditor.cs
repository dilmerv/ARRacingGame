// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Linq;
using System.Text;

using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR.Mock;
using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.VirtualStudio.Networking.Mock;

using UnityEditor;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Editor
{
  public sealed class VirtualStudioConfigurationEditor : EditorWindow
  {
    private const string VS_MODE_KEY = "ARDK_VirtualStudio_Mode";

    private int _vsModeSelection;
    private RuntimeEnvironment _selectedARInfoSource;

    [SerializeField]
    private _RemoteConfigurationEditor _remoteConfigEditor;

    [SerializeField]
    private _MockPlayConfigurationEditor _mockPlayConfigEditor;

    private bool _enabled;

    private static GUIStyle _headerStyle;
    internal static GUIStyle _HeaderStyle
    {
      get
      {
        if (_headerStyle == null)
        {
          _headerStyle = new GUIStyle(EditorStyles.boldLabel);
          _headerStyle.fontSize = 18;
          _headerStyle.fixedHeight = 36;
        }

        return _headerStyle;
      }
    }

    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("ARDK/Virtual Studio")]
    public static void Init()
    {
      var window = GetWindow<VirtualStudioConfigurationEditor>(false, "Virtual Studio");
      window.Show();

      window._mockPlayConfigEditor = new _MockPlayConfigurationEditor();
      window._remoteConfigEditor = new _RemoteConfigurationEditor();

      window.LoadPreferences();
    }

    private void LoadPreferences()
    {
      // Valid ARInfoSource values start at 1
      _vsModeSelection = PlayerPrefs.GetInt(VS_MODE_KEY, 0);
      _selectedARInfoSource = (RuntimeEnvironment) _vsModeSelection + 1;

      _remoteConfigEditor.OnSelectionChange(_selectedARInfoSource == RuntimeEnvironment.Remote);

      switch (_selectedARInfoSource)
      {
        case RuntimeEnvironment.Mock:
          _mockPlayConfigEditor.LoadPreferences();
          break;
        case RuntimeEnvironment.Remote:
          _remoteConfigEditor.LoadPreferences();
          break;
      }
    }

    private void OnGUI()
    {
      using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
      {
        scrollPos = scrollView.scrollPosition;

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        DrawEnabledGUI();
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(50);

        switch (_selectedARInfoSource)
        {
          case RuntimeEnvironment.Remote:
            EditorGUILayout.LabelField("Remote Connection", _HeaderStyle);
            GUILayout.Space(10);
            _remoteConfigEditor.DrawGUI();
            break;

          case RuntimeEnvironment.Mock:
            EditorGUILayout.LabelField("Mock Play Configuration", _HeaderStyle);
            GUILayout.Space(10);
            _mockPlayConfigEditor.DrawGUI();
            break;
        }
      }
    }

    private static readonly string[] _modeSelectionGridStrings = { "None", "Remote", "Mock" };
    private void DrawEnabledGUI()
    {
      var newModeSelection =
        GUI.SelectionGrid
        (
          new Rect(10, 20, 300, 20),
          _vsModeSelection,
          _modeSelectionGridStrings,
          3
        );

      if (newModeSelection != _vsModeSelection)
      {
        _vsModeSelection = newModeSelection;
        PlayerPrefs.SetInt(VS_MODE_KEY, _vsModeSelection);
        LoadPreferences();
      }
    }
  }
}
