// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

/// @namespace Niantic.ARDK.Utilities.Logging
/// @brief Handles debug logging for ARDK systems
namespace Niantic.ARDK.Utilities.Logging
{
  /// <summary>
  /// Provides a common log for ARDK systems.
  /// </summary>
  public static class ARLog
  {
    private const string ARDK_LogMessage = "#{0}#: {1}";

    private static IARLogHandler _logHandler = UnityARLogHandler.Instance;
    /// <summary>
    /// Gets or sets the log handler used by ARLog.
    /// @note If no handler is set or null is passed in, ARLog will default to using UnityEngine's
    /// Debug.Log.
    /// </summary>
    public static IARLogHandler LogHandler
    {
      get { return _logHandler; }
      set
      {
        _StaticMemberValidator._FieldContainsSpecificValueWhenScopeEnds
        (
          () => _logHandler,
          UnityARLogHandler.Instance
        );

        if (value == null)
          value = UnityARLogHandler.Instance;

        _logHandler = value;
      }
    }

    /// <summary>
    /// Whether or not to enable logs that print every frame (peer messages, frame updates, etc)
    /// Enabling this will decrease performance, as printing multiple logs per frame is expensive
    /// </summary>
    public static bool PerFrameLogsEnabled { get; set; }

    /// <summary>
    /// Prints a debug level log.
    /// </summary>
    /// <param name="log">Log to print</param>
    /// <param name="perFrame">Whether the log will occur every frame</param>
    [Conditional("ARDK_DEBUG")]
    internal static void _Debug(string log, bool perFrame = false)
    {
      if(perFrame && !PerFrameLogsEnabled)
        return;

      var caller = _GetCallerFromStack(2);
      if (!_IsFeatureEnabled(caller))
        return;

      var str = String.Format(ARDK_LogMessage, caller, log);
      _logHandler.Debug(str);
    }

    /// <summary>
    /// Prints a debug level log with String.Format.
    /// </summary>
    /// <param name="log">Formatted string to print</param>
    /// <param name="perFrame">Whether the log will occur every frame</param>
    /// <param name="objs">List of objects to format into the log</param>
    [Conditional("ARDK_DEBUG")]
    internal static void _DebugFormat(string log, bool perFrame = false, params object[] objs)
    {
      if(perFrame && !PerFrameLogsEnabled)
        return;

      var caller = _GetCallerFromStack(2);
      if (!_IsFeatureEnabled(caller))
        return;

      var logFormat = String.Format(log, objs);
      var str = String.Format(ARDK_LogMessage, caller, logFormat);
      _logHandler.Debug(str);
    }

    /// <summary>
    /// Prints a warning level log.
    /// </summary>
    /// <param name="log">Warning to print</param>
    /// <param name="perFrame">Whether the log will occur every frame</param>
    [Conditional("ARDK_DEBUG")]
    internal static void _Warn(string log, bool perFrame = false)
    {
      if(perFrame && !PerFrameLogsEnabled)
        return;

      var caller = _GetCallerFromStack(2);
      if (!_IsFeatureEnabled(caller))
        return;

      var str = String.Format(ARDK_LogMessage, caller, log);
      _logHandler.Warn(str);
    }

    /// <summary>
    /// Prints a warning level log with String.Format.
    /// </summary>
    /// <param name="log">Formatted string to print</param>
    /// <param name="perFrame">Whether the log will occur every frame</param>
    /// <param name="objs">List of objects to format into the log</param>
    [Conditional("ARDK_DEBUG")]
    internal static void _WarnFormat(string log, bool perFrame = false, params object[] objs)
    {
      if(perFrame && !PerFrameLogsEnabled)
        return;

      var caller = _GetCallerFromStack(2);
      if (!_IsFeatureEnabled(caller))
        return;

      var logFormat = String.Format(log, objs);
      var str = String.Format(ARDK_LogMessage, caller, logFormat);
      _logHandler.Warn(str);
    }

    /// <summary>
    /// Prints a release level log.
    /// </summary>
    /// <param name="log">Log to print</param>
    /// <param name="perFrame">Whether the log will occur every frame</param>
    internal static void _Release(string log, bool perFrame = false)
    {
      if(perFrame && !PerFrameLogsEnabled)
        return;

      var caller = _GetCallerFromStack(2);
      if (!_IsFeatureEnabled(caller))
        return;

      var str = String.Format(ARDK_LogMessage, caller, log);
      _logHandler.Release(str);
    }

    /// <summary>
    /// Prints a release level log with String.Format.
    /// </summary>
    /// <param name="log">Formatted string to print</param>
    /// <param name="perFrame">Whether the log will occur every frame</param>
    /// <param name="objs">List of objects to format into the log</param>
    internal static void _ReleaseFormat(string log, bool perFrame = false, params object[] objs)
    {
      if(perFrame && !PerFrameLogsEnabled)
        return;

      var caller = _GetCallerFromStack(2);
      if (!_IsFeatureEnabled(caller))
        return;

      var logFormat = String.Format(log, objs);
      var str = String.Format(ARDK_LogMessage, caller, logFormat);
      _logHandler.Release(str);
    }

    /// <summary>
    /// Prints a release level warning log.
    /// </summary>
    /// <param name="log">Log to print</param>
    internal static void _WarnRelease(string log)
    {
      var caller = _GetCallerFromStack(2);

      var str = String.Format(ARDK_LogMessage, caller, log);
      _logHandler.Warn(str);
    }

    /// <summary>
    /// Prints a release level warning log with String.Format.
    /// </summary>
    /// <param name="log">Formatted string to print</param>
    /// <param name="objs">List of objects to format into the log</param>
    internal static void _WarnFormatRelease(string log, params object[] objs)
    {
      var caller = _GetCallerFromStack(2);

      var logFormat = String.Format(log, objs);
      var str = String.Format(ARDK_LogMessage, caller, logFormat);
      _logHandler.Warn(str);
    }

    /// <summary>
    /// Prints an error log.
    /// </summary>
    /// <param name="log">Error to print</param>
    internal static void _Error(string log)
    {
      var caller = _GetCallerFromStack(2);

      var str = String.Format(ARDK_LogMessage, caller, log);
      _logHandler.Error(str);
    }

    /// <summary>
    /// Prints an error log with String.Format.
    /// </summary>
    /// <param name="log">Formatted error to print</param>
    /// <param name="objs">List of objects to format into the log</param>
    internal static void _ErrorFormat(string log, params object[] objs)
    {
      var caller = _GetCallerFromStack(2);

      var logFormat = String.Format(log, objs);
      var str = String.Format(ARDK_LogMessage, caller, logFormat);
      _logHandler.Error(str);
    }

    /// <summary>
    /// Prints the contents of an exception and the provided context. Does not rethrow the exception
    ///   or halt execution.
    /// </summary>
    /// <param name="exception">Exception to print</param>
    /// <param name="context">Context for the exception</param>
    internal static void _Exception(Exception exception, object context = null)
    {
      var message = "{0} from context: {1}";
      var str = String.Format(message, exception, context);

      _logHandler.Error(str);
    }

    /// <summary>
    /// Enable logging for a specific feature
    /// </summary>
    /// <param name="feature">Feature to log</param>
    public static void EnableLogFeature(string feature)
    {
      _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _enabledFeatures);

      var changed = _enabledFeatures.TryAdd(feature, true);
      if (changed)
        _cachedFeatureEnabledChecks.Clear();
    }

    /// <summary>
    /// Enable logging for a set of features
    /// </summary>
    /// <param name="features">Features to log</param>
    public static void EnableLogFeatures(IEnumerable<string> features)
    {
      _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _enabledFeatures);

      bool changed = false;
      foreach (var feature in features)
        if (_enabledFeatures.TryAdd(feature, true))
          changed = true;

      if (changed)
        _cachedFeatureEnabledChecks.Clear();
    }

    /// <summary>
    /// Disable logging for a feature
    /// </summary>
    /// <param name="feature">Feature to no longer log</param>
    public static void DisableLogFeature(string feature)
    {
      var changed = _enabledFeatures.TryRemove(feature, out _);
      if(changed)
        _cachedFeatureEnabledChecks.Clear();
    }

    /// <summary>
    /// Disable logging for a set of features
    /// </summary>
    /// <param name="features">Features to no longer log</param>
    public static void DisableLogFeatures(IEnumerable<string> features)
    {
      var changed = false;

      foreach (var feature in features)
        if (_enabledFeatures.TryRemove(feature, out _))
          changed = true;

      if (changed)
        _cachedFeatureEnabledChecks.Clear();
    }

    private static readonly ConcurrentDictionary<string, bool> _cachedFeatureEnabledChecks =
      new ConcurrentDictionary<string, bool>();

    // ConcurrentDictionary works better than a list for its lock free reading.
    // Using a list + Contains instead of trie/radix tree to merge the API for now
    // TODO: implement a fast multistring search solution
    private static readonly ConcurrentDictionary<string, bool> _enabledFeatures =
      new ConcurrentDictionary<string, bool>();

    private static readonly Func<string, bool> _checkEnabledFeaturesDelegate =
      (caller) =>
      {
        foreach (var enabledFeature in _enabledFeatures.Keys)
          if (caller.StartsWith(enabledFeature))
            return true;

        return false;
      };

    /// <summary>
    /// Determine if the specified caller should display its logs
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    private static bool _IsFeatureEnabled(string caller)
    {
      return _cachedFeatureEnabledChecks.GetOrAdd(caller, _checkEnabledFeaturesDelegate);
    }

    /// <summary>
    /// Gets the full name (Namespace.Class) of the method calling this.
    /// </summary>
    /// <param name="nestedLevel">Level of nested-ness of this call. For example, to get the direct
    ///   caller of this method, the level would be 1. To get the caller that calls this through
    ///   a helper, the level would be 2. </param>
    /// <returns>A string with the full name of the calling class, or "Niantic.ARDK" if something fails</returns>
    private static string _GetCallerFromStack(int nestedLevel)
    {
      // Get the frame above the current one (the caller of this method)
      var callerFrame = new StackFrame(nestedLevel, false);

      var caller = callerFrame?.GetMethod()?.ReflectedType;

      if (caller == null)
        return "Niantic.ARDK";

      return caller.FullName;
    }
  }
}
