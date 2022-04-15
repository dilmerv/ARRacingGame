// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using UnityEngine;

namespace Niantic.ARDK.Utilities.Marker
{
  public interface IParserResult
  {
    byte[] Data { get; set; }

    Vector2[] DetectedPoints { get; set; }

    IARCamera ARCamera { get; set; }

    double Timestamp { get; set; }
  }
}
