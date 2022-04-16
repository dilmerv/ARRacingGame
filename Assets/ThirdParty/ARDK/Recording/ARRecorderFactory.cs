// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Factory for creating <see cref="IARRecorder"/> based on the current platform and
  /// <see cref="IARSession"/>.
  /// </summary>
  public static class ARRecorderFactory
  {
    /// <summary>
    /// Creates the correct <see cref="IARRecorder"/> based on the given session and current platform.
    /// </summary>
    public static IARRecorder CreateRecorder(IARSession session)
    {
      var arSession = session as _NativeARSession;

      // TODO: Either implement an EditorARRecorder or change NativeARRecorder to accept an
      // IARSession
      return arSession != null ? new NativeARRecorder(arSession) : null;
    }
  }
}