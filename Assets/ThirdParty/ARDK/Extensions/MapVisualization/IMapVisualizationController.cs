// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.AR.SLAM;
using UnityEngine.Rendering;

namespace Niantic.ARDK.Extensions.MapVisualization {
  /// @brief Controller for map visualization
  public interface IMapVisualizationController {
    /// <summary>
    /// Set the map that needs to be visualized
    /// </summary>
    void VisualizeMap(IARMap _map);

    /// <summary>
    /// If true, shows the map. Else hides the map.
    /// </summary>
    void SetVisibility(bool visibility);
  }

}