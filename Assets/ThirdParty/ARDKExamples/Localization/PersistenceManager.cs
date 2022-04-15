using System.Collections.Generic;

using Niantic.ARDK.AR.Localization;
using Niantic.ARDK.Extensions.Localization;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDKExamples.Localization
{
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public class PersistenceManager: MonoBehaviour
  {
    [SerializeField]
    private LocalizationEventManager _localizationEventManager;
        
    [SerializeField]
    private GameObject _persistedPrefab;

    private Dictionary<ARWorldCoordinateSpace.Identifier, Transform> _localizations;

    private void Start()
    {
      if (_localizationEventManager == null)
        return;

      _localizations = new Dictionary<ARWorldCoordinateSpace.Identifier, Transform>();

      _localizationEventManager.LocalizationSucceeded += OnLocalizationSucceeded;
      _localizationEventManager.LocalizationCleared += OnLocalizationCleared;
    }

    private void OnDestroy()
    {
      _localizationEventManager.LocalizationSucceeded -= OnLocalizationSucceeded;
      _localizationEventManager.LocalizationCleared -= OnLocalizationCleared;
    }

    void OnLocalizationSucceeded(LocalizationEventArgs args)
    {
      var coordinateSpace = args.CoordinateSpace;
      var id = coordinateSpace.Id;
      Debug.Log("Localization succeeded: " + id);
      Transform t;
      if (!_localizations.TryGetValue(id, out t))
      {
        GameObject go = new GameObject(id.ToString());
        t = go.transform;
        _localizations[id] = t;

        if (_persistedPrefab != null)
          GameObject.Instantiate(_persistedPrefab, t, false);
      }

      var matrix = coordinateSpace.Transform;
      t.SetPositionAndRotation(matrix.ToPosition(), matrix.ToRotation());
    }

    void OnLocalizationCleared(LocalizationEventArgs args)
    {
      foreach(var t in _localizations.Values)
      {
        Destroy(t.gameObject);
      }
      _localizations.Clear();
      
      Debug.Log("Localization cleared");
    }
  }
}
