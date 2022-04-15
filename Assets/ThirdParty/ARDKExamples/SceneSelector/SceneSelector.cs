// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;
using UnityEngine.SceneManagement;
using Niantic.ARDK.Utilities.VersionUtilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneSelector:
  MonoBehaviour
{
  [SerializeField]
  private float fontSize = 47.0f;

  private Vector2 scrollPos;
  private int scenes;
  private float entryHeight, entryWidth, screenHeight, screenWidth;
  private GUIStyle fontStyle, vertStyle, horizStyle;
  private Rect scrollviewPos, viewport;
  private string ardkVersion;
  private string ardkIntOrExt;

  void Start()
  {
#if UNITY_EDITOR
    scenes = EditorBuildSettings.scenes.Length;
#else
    scenes = SceneManager.sceneCountInBuildSettings;
#endif

    screenHeight = Screen.height;
    screenWidth = Screen.width;
    entryWidth = Screen.width - screenWidth * 0.1f;
    entryHeight = Screen.height * 0.1f;
    ardkVersion =  ARDKGlobalVersion.GetARDKVersion();
    ardkIntOrExt = Niantic.ARDK.Networking.ServerConfiguration.AuthRequired ? "EXT" : "INT";

  }

  private void OnGUI()
  {
    if (fontStyle == null)
    {
      fontStyle = new GUIStyle(GUI.skin.button);
      fontStyle.fontSize = Mathf.FloorToInt(fontSize * screenWidth / 1080.0f);
      scrollviewPos = new Rect(0, 0, screenWidth, screenHeight);
      viewport = new Rect(0, 0, entryWidth, entryHeight * scenes);
      vertStyle = new GUIStyle(GUI.skin.verticalScrollbar);
      vertStyle.fixedWidth = screenWidth * 0.1f;
      GUI.skin.verticalScrollbarThumb.fixedWidth = screenWidth * 0.1f;
      horizStyle = new GUIStyle(GUI.skin.horizontalScrollbar);
    }

    var dy = 0.0f;
    scrollPos = GUI.BeginScrollView(scrollviewPos, scrollPos, viewport, horizStyle, vertStyle);

    for (var i = 0; i < scenes; i++)
    {
#if UNITY_EDITOR
      var scene = EditorBuildSettings.scenes[i].path;
#else
      var scene = SceneUtility.GetScenePathByBuildIndex(i);
#endif
      var text = scene.Substring(scene.LastIndexOf('/') + 1);

      if (GUI.Button(new Rect(0, dy, screenWidth, entryHeight), text, fontStyle))
      {
        Debug.LogFormat("ARDK Examples: Loading scene {0}", text);
#if UNITY_EDITOR
        SceneManager.LoadScene(scene);
#else
        SceneManager.LoadScene(i);
#endif

        break;
      }

      dy += entryHeight;
    }

    GUI.EndScrollView();
    GUIStyle ardkVersionFontStyle = new GUIStyle(GUI.skin.button);
    float ardkVersionFontSize = 40.0f;
    ardkVersionFontStyle.fontSize = Mathf.FloorToInt(ardkVersionFontSize * screenWidth / 1080.0f);
    GUI.Label
    (
      new Rect (10, 10, screenWidth/2, screenHeight/10), 
      $"ARDK Version: {ardkVersion} \n ARDK Int/Ext: {ardkIntOrExt} \n Unity Version: {Application.unityVersion}", 
      ardkVersionFontStyle
    );
  }
}
