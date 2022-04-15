// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if ARDK_HAS_URP

using System;
using System.Collections.Generic;
using System.Reflection;

using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Niantic.ARDK.Rendering.SRP
{
  /// <summary>
  /// Provides reflection-based access to the internal interface of the rendering pipeline.
  /// </summary>
  internal static class _RenderPipelineInternals
  {
    public const string ARDK_RENDERER_NAME = "ArdkUrpAssetRenderer";
    public const string REPLACEMENT_RENDERER_NAME = "ArdkReplacementRenderer";

    private static Dictionary<Type, ScriptableRendererFeature> _features =
      new Dictionary<Type, ScriptableRendererFeature>();

    public static bool IsUniversalRenderPipelineEnabled
    {
      get
      {
        var asset = UniversalRenderPipeline.asset;
        return asset != null && UniversalRenderPipeline.asset.scriptableRenderer != null;
      }
    }

    public static void SetFeatureActive<T>(bool isActive)
      where T:ScriptableRendererFeature
    {
      var feature = GetFeatureOfType<T>();

      if (feature == null)
      {
        var messageFormat =
          "No {0} was found added to the active Universal Render Pipeline Renderer.";
        Debug.LogWarningFormat(messageFormat, typeof(T).Name);
      }

      feature.SetActive(isActive);
    }

    public static T GetFeatureOfType<T>()
      where T: ScriptableRendererFeature
    {
      if (!IsUniversalRenderPipelineEnabled)
        return null;

      if (_features.ContainsKey(typeof(T)))
        return _features[typeof(T)] as T;

      var renderer = UniversalRenderPipeline.asset.scriptableRenderer;

      var propertyInfo =
        renderer
          .GetType()
          .GetProperty("rendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);

      var features = (List<ScriptableRendererFeature>) propertyInfo.GetValue(renderer);
      var item = features.Find(x => x is T);

      _features.Add(typeof(T), item);

      return item as T;
    }

    public static int GetRendererIndex(string rendererName)
    {
      if (!IsUniversalRenderPipelineEnabled)
        return -1;

      var index = 0;

      var pipeline = UniversalRenderPipeline.asset;
      var fieldInfo =
        pipeline
          .GetType()
          .GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);

      var data = (ScriptableRendererData[]) fieldInfo.GetValue(pipeline);

      while (index < data.Length)
      {
        if (string.Equals(data[index].name, rendererName))
          return index;

        index++;
      }

      return -1;
    }
  }
}

#endif
