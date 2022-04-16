// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

/// @namespace Niantic.ARDK.Utilities.Marker
/// @brief Tools supporting the MarkerSync feature that enables users to scan a physical marker to join a networked AR session.
/// @note This is part of an experimental feature that is not advised to be used in release builds.
namespace Niantic.ARDK.Utilities.Marker
{
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public interface IMarkerScanner:
    IDisposable
  {
    event ArdkEventHandler<ARFrameMarkerScannerReadyArgs> Ready;
    event ArdkEventHandler<ARFrameMarkerScannerStatusChangedArgs> StatusChanged;
    event ArdkEventHandler<ARFrameMarkerScannerGotResultArgs> GotResult;

    MarkerScannerStatus Status { get; }

    IMarkerParser MarkerParser { get; }

    void Scan();
    void Stop();
    void Update();
  }
}
