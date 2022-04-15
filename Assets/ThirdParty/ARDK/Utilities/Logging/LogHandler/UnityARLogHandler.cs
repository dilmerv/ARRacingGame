// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Utilities.Logging
{
  /// <summary>
  /// Implementation of IARLogHandler that uses UnityEngine.Debug as its target
  /// </summary>
  public sealed class UnityARLogHandler: 
    IARLogHandler
  {
    /// Gets the singleton instance of this log handler.
    public static readonly UnityARLogHandler Instance = new UnityARLogHandler();

    private UnityARLogHandler()
    {
    }

    /// <inheritdoc />
    public void Debug(string log)
    {
      UnityEngine.Debug.Log(log);
    }

    /// <inheritdoc />
    public void Warn(string log)
    {
      UnityEngine.Debug.LogWarning(log);
    }

    /// <inheritdoc />
    public void Release(string log)
    {
      UnityEngine.Debug.Log(log);
    }

    /// <inheritdoc />
    public void Error(string log)
    {
      UnityEngine.Debug.LogError(log);
    }
  }
}
