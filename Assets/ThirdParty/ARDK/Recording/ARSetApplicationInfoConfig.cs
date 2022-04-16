// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Niantic.ARDK.Recording {
  /// <summary>
  /// Configs for setting application information.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARSetApplicationInfoConfig {
    /// <summary>
    /// The name of the application making the recording.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)] public string ApplicationName;
  }
}