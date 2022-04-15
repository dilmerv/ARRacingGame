using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Utilities.Preloading
{
  internal class _MockFeaturePreloader:
    IFeaturePreloader
  {
    private HashSet<Feature> _cache = new HashSet<Feature>();
    private Dictionary<Feature, float> _progress = new Dictionary<Feature, float>();
    private bool _downloading;

    public void Dispose()
    {
      _cache.Clear();
      _UpdateLoop.Tick -= ProgressDownloads;
    }

    public float GetProgress(Feature feature)
    {
      if (_cache.Contains(feature))
        return 1;

      if (_progress.TryGetValue(feature, out float value))
        return value;

      return 0;
    }

    public PreloadedFeatureState GetStatus(Feature feature)
    {
      if (_cache.Contains(feature))
        return PreloadedFeatureState.Finished;

      if (_progress.ContainsKey(feature))
        return PreloadedFeatureState.Downloading;

      return PreloadedFeatureState.Invalid;
    }

    public bool ExistsInCache(Feature feature)
    {
      return _cache.Contains(feature);
    }

    public void Download(Feature[] features)
    {
      foreach (var feature in features)
      {
        if (!_cache.Contains(feature))
          _progress.Add(feature, 0f);
      }

      if (!_downloading)
      {
        _downloading = true;
        _UpdateLoop.FixedTick += ProgressDownloads;
      }
    }

    // Default value of Time.fixedDeltaTime is 0.02 (50 calls per second), meaning a download will
    // take about 2 seconds to complete (likely enough to see any UI changes dependent on progress).
    private const float _progressIncrement = 0.01f;
    private void ProgressDownloads()
    {
      foreach (var feature in _progress.Keys.ToArray())
      {
        var currProgress = _progress[feature] + _progressIncrement;

        if (currProgress > 1)
        {
          _progress.Remove(feature);
          _cache.Add(feature);

          if (_progress.Count == 0)
          {
            _downloading = false;
            _UpdateLoop.FixedTick -= ProgressDownloads;
          }
        }
        else
        {
          _progress[feature] = currProgress;
        }
      }
    }

    public void ClearCache(Feature feature)
    {
      if (_downloading)
        ARLog._Error("Attempted to clear the preloader cache while downloads were in progress.");

      _downloading = false;
      _UpdateLoop.Tick -= ProgressDownloads;
      _cache.Remove(feature);
    }
  }
}
