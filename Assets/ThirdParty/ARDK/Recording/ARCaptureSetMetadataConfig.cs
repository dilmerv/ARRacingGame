// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Niantic.ARDK.Recording {
  /// <summary>
  /// Configs for setting capture metadata.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARCaptureSetMetadataConfig {
    /// <summary>
    /// JSON-formatted dictionary of metadata fields to add to the capture.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)] public string Metadata;
  }
}