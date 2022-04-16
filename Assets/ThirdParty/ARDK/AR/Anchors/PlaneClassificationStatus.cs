// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Anchors
{
  /// <summary>
  /// The status values of "NotAvailable", "Undetermined", and "Unknown" are possible reasons why the
  /// plane's classification is "None." If the plane's classification is NOT None, status will be "Known"
  /// </summary>
  public enum PlaneClassificationStatus
  {
    /// ARKit cannot currently provide plane classification information because this device is
    /// not an iPhone XS/XR or higher
    NotAvailable = 0,

    /// ARKit has not yet produced a classification for the plane anchor. ARKit is still in the
    /// process of plane classification
    Undetermined = 1,

    /// ARKit has completed its classification process for the plane anchor, but the result is inconclusive.
    Unknown = 2,

    /// ARKit has completed its classfication process for the plane anchor. Use getClassification()
    /// to retrieve the plane's classification
    Known = 3
  }
}
