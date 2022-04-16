// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Logging
{
  /// <summary>
  /// Interface for a log handler used by the ARLog. Implement this class to provide
  ///   alternative logging behaviour.
  /// </summary>
  public interface IARLogHandler
  {
    /// <summary>
    /// Debug level log, only enabled if #ARDK_DEBUG is defined
    /// </summary>
    /// <param name="log">Log to print</param>
    void Debug(string log);
    
    /// <summary>
    /// Warning level log, only enabled if #ARDK_DEBUG is defined
    /// </summary>
    /// <param name="warning">Warning to print</param>
    void Warn(string warning);
    
    /// <summary>
    /// Release level log, will still check against enabled features
    /// </summary>
    /// <param name="log">Log to print</param>
    void Release(string log);
    
    /// <summary>
    /// Error level log, will always print
    /// </summary>
    /// <param name="error">Error to print</param>
    void Error(string error);
  }
}
