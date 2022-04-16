// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using UnityEngine;

namespace Niantic.ARDK.Utilities.Marker
{
  public class BarcodeParserResult:
    IParserResult
  {
    public byte[] Data { get; set; }

    public Vector2[] DetectedPoints { get; set; }

    public IARCamera ARCamera { get; set; }

    public double Timestamp
    {
      get
      {
        return _timestamp;
      }
      set
      {
        _timestamp = value;
      }
    }

    private double _timestamp = -1;

    public override string ToString()
    {
      return
        string.Format
        (
          "MarkerParserResult: [{0}, {1}, {2}, {3}], with data len {4}",
          DetectedPoints == null ? "null" : DetectedPoints[0].ToString(),
          DetectedPoints == null ? "null" : DetectedPoints[1].ToString(),
          DetectedPoints == null ? "null" : DetectedPoints[2].ToString(),
          DetectedPoints == null ? "null" : DetectedPoints[3].ToString(),
          Data == null ? 0 : Data.Length
        );
    }
  }
}
