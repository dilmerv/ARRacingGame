// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Marker
{
  public struct ARFrameMarkerScannerStatusChangedArgs:
    IArdkEventArgs
  {
    public readonly MarkerScannerStatus Status;

    public ARFrameMarkerScannerStatusChangedArgs(MarkerScannerStatus status)
    {
      Status = status;
    }
  }
}