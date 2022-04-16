// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Preloading;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  public class FeaturePreloadManager:
    UnityLifecycleDriver
  {
    public sealed class PreloadProgressUpdatedArgs:
      IArdkEventArgs
    {
      public ReadOnlyCollection<Feature> FailedPreloads;
      public ReadOnlyCollection<Feature> SuccessfulPreloads;
      public float Progress;
      public bool PreloadAttemptFinished;
    }

    [SerializeField]
    private List<Feature> _features = null;

    private Action<bool> _finishedDownload;
    private Queue<Feature> _featuresRemaining;
    private Coroutine _preloadCoroutine;

    public ArdkEventHandler<PreloadProgressUpdatedArgs> ProgressUpdated;

    public IFeaturePreloader Preloader
    {
      get
      {
        if (_preloader != null)
          return _preloader;

        ARLog._Error("FeaturePreloadManager has not been initialized yet.");
        return null;
      }
    }

    private IFeaturePreloader _preloader;

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      _preloader = FeaturePreloaderFactory.Create();

      // Might not be null if initialized through Inspector.
      if (_features == null)
        _features = new List<Feature>();

      // Remove duplicate features if added (adding duplicates is only possible through Inspector).
      if (_features.Count > Enum.GetValues(typeof(Feature)).Length)
        _features = _features.Distinct().ToList();
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      if (_preloader != null)
        _preloader.Dispose();
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      StartDownload();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      StopDownload();
    }

    public void AddFeature(Feature feature)
    {
      if (!_features.Contains(feature))
        _features.Add(feature);
    }

    /// Clears ALL downloaded features, not just the ones added to this manager's features list.
    public void ClearCache()
    {
      foreach (Feature feature in Enum.GetValues(typeof(Feature)))
      {
        Preloader.ClearCache(feature);
      }
    }

    /// Starts asynchronous download of added features.
    public void StartDownload()
    {
      if (_preloadCoroutine != null)
      {
        ARLog._Warn("Preloading features is already in progress.");
        return;
      }

      _preloadCoroutine = StartCoroutine(DownloadFeatures());
    }

    /// Stops download of added features, and clears any partially downloaded features from the cache.
    public void StopDownload()
    {
      if (_preloadCoroutine == null)
        return;

      StopCoroutine(_preloadCoroutine);
      _preloadCoroutine = null;

      // Only clear the features that were only partially downloaded
      var featuresToClear = new HashSet<Feature>();
      foreach (var feature in _features)
      {
        var status = Preloader.GetStatus(feature);

        if (status != PreloadedFeatureState.Finished)
          featuresToClear.Add(feature);
      }

      // Todo: Change once canceling preloads is possible outside of just disposing the preloader
      Preloader.Dispose();
      _preloader = FeaturePreloaderFactory.Create();

      foreach (var feature in featuresToClear)
      {
        ARLog._DebugFormat("Cleared partial download of {0} feature from cache.", false, feature);
        _preloader.ClearCache(feature);
      }
    }

    public bool AreAllFeaturesDownloaded()
    {
      foreach (var feature in _features)
      {
        if (!Preloader.ExistsInCache(feature))
          return false;
      }

      return true;
    }

    private IEnumerator DownloadFeatures()
    {
      var featuresToDownload = new List<Feature>();

      if (_features.Count == 0)
      {
        ARLog._Warn("No features were added to download.");
        yield break;
      }

      if (Preloader == null)
        yield break;

      var successfulPreloads = new List<Feature>();
      var failedPreloads = new List<Feature>();

      var updateArgs =
        new PreloadProgressUpdatedArgs
        {
          FailedPreloads = new ReadOnlyCollection<Feature>(failedPreloads),
          SuccessfulPreloads = new ReadOnlyCollection<Feature>(successfulPreloads),
        };

      foreach (var feature in _features)
      {
        // Don't add feature to be downloaded if it was found cached.
        if (_preloader.ExistsInCache(feature))
        {
          ARLog._DebugFormat("{0} Feature was found in cache", false, feature);
          successfulPreloads.Add(feature);
          continue;
        }

        // Check cache again as workaround for bug where the first call to ExistsInCache might
        // return a false negative.
        if (_preloader.ExistsInCache(feature))
        {
          ARLog._DebugFormat("{0} Feature was found in cache", false, feature);
          successfulPreloads.Add(feature);
          continue;
        }

        featuresToDownload.Add(feature);
      }

      if (featuresToDownload.Count == 0)
      {
        _preloadCoroutine = null;

        ARLog._Debug("All features were found in cache. No need to download.");
        updateArgs.Progress = 1f;
        updateArgs.PreloadAttemptFinished = true;
        ProgressUpdated?.Invoke(updateArgs);

        _preloadCoroutine = null;
        yield break;
      }

      var featuresCount = (float) _features.Count;

      ARLog._Debug("Starting preload...");
      _preloader.Download(featuresToDownload.ToArray());

      while (true)
      {
        yield return null;

        var allFeaturesProgress = 0f;

        for (int i = featuresToDownload.Count - 1; i >= 0; i--)
        {
          var feature = featuresToDownload[i];

          switch (_preloader.GetStatus(feature))
          {
            case PreloadedFeatureState.Invalid:
            case PreloadedFeatureState.NotStarted:
            case PreloadedFeatureState.Downloading:
              var progress = _preloader.GetProgress(feature);
              ARLog._DebugFormat("Downloading {0} feature... {1}", false, feature, progress);
              allFeaturesProgress += progress;
              break;

            case PreloadedFeatureState.Finished:
              ARLog._DebugFormat("Successfully downloaded {0} feature", false, feature);
              successfulPreloads.Add(feature);
              featuresToDownload.RemoveAt(i);
              break;

            case PreloadedFeatureState.Failed:
              ARLog._DebugFormat("Failed to download {0} feature", false, feature);
              failedPreloads.Add(feature);
              featuresToDownload.RemoveAt(i);
              break;
          }
        }

        var completedDownloads = successfulPreloads.Count + failedPreloads.Count;
        updateArgs.Progress = (completedDownloads + allFeaturesProgress) / featuresCount;
        updateArgs.PreloadAttemptFinished = completedDownloads == _features.Count;

        ProgressUpdated?.Invoke(updateArgs);

        if (updateArgs.PreloadAttemptFinished)
        {
          _preloadCoroutine = null;
          yield break;
        }
      }
    }
  }
}
