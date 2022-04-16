// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToSceneSelector:
  MonoBehaviour
{
  private string _sceneName;
  private static ReturnToSceneSelector _instance;

  void Start()
  {
    if (_instance == null)
    {
      _instance = this;
      _sceneName = gameObject.scene.name;
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  void Update()
  {
    var hasInput = Input.touches.Length >= 5 || Input.GetKeyDown(KeyCode.B);

    if (hasInput && SceneManager.GetActiveScene().name != _sceneName)
    {
      Debug.Log("ARDK Examples: Returning to Scene Selector");
      SceneManager.LoadScene(_sceneName);
    }
  }
}
