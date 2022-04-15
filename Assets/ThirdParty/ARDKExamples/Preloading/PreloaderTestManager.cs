// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Extensions;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Preloading;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Preloading
{
  public class PreloaderTestManager:
    MonoBehaviour
  {
    [SerializeField]
    private FeaturePreloadManager _preloadManager = null;

    [SerializeField]
    private Button _dbowButton = null;

    [SerializeField]
    private Button _contextAwarenessButton = null;

    [Header("Status UI")]
    [SerializeField]
    private Text _dbowStatusText = null;

    [SerializeField]
    private Text _contextAwarenessStatusText = null;

    [SerializeField]
    private Text _preloadStatusText = null;

    [SerializeField]
    private Slider _percentageSlider = null;

    [SerializeField]
    private Text _percentageText = null;

    private void Awake()
    {
      ARLog.EnableLogFeature("Niantic");
      _preloadManager.Initialize();
      _preloadManager.ProgressUpdated += OnProgressUpdated;

      _dbowButton.onClick.AddListener
      (
        () =>
        {
          _preloadManager.AddFeature(Feature.Dbow);
          _dbowButton.interactable = false;
        }
      );

      _contextAwarenessButton.onClick.AddListener
      (
        () =>
        {
          _preloadManager.AddFeature(Feature.ContextAwareness);
          _contextAwarenessButton.interactable = false;
        }
      );
    }

    private string GetFeatureStatus(Feature feature)
    {
      if (_preloadManager.Preloader.ExistsInCache(feature))
        return "In cache";

      return _preloadManager.Preloader.GetStatus(feature).ToString();
    }

    private void Update()
    {
      _dbowStatusText.text = GetFeatureStatus(Feature.Dbow);
      _contextAwarenessStatusText.text = GetFeatureStatus(Feature.ContextAwareness);
    }

    private void OnProgressUpdated(FeaturePreloadManager.PreloadProgressUpdatedArgs args)
    {
      _percentageText.text = args.Progress.ToString();
      _percentageSlider.value = args.Progress;

      if (args.PreloadAttemptFinished)
      {
        var success = args.FailedPreloads.Count == 0;
        _preloadStatusText.text = string.Format("Downloads complete: {0}", success ? "success" : "failed");
      }
    }
  }
}
