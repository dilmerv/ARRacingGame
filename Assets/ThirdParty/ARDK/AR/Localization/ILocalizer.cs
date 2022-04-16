// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.Localization
{
  /// @cond ARDK_VPS_BETA
  /// Interface for Visual Positioning System (VPS) localization requests.
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @see [ILocalizableARSession](@ref ARDK.AR.ILocalizableARSession)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public interface ILocalizer:
    IDisposable
  {
    /// @cond ARDK_VPS_BETA
    /// Starts an attempt to detect a nearby world coordinate space.
    /// @note A location service must be attached to the session.
    /// @param config
    ///   Localization Configuration
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    void StartLocalization(ILocalizationConfiguration config);

    /// @cond ARDK_VPS_BETA
    /// Stops an ongoing process of detecting a world coordinate space.
    /// @param localization
    ///   The attempt to interrupt.
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    void StopLocalization();

    /// @cond ARDK_VPS_BETA
    /// Alerts subscribers when the localization process changes state.
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    event ArdkEventHandler<LocalizationProgressArgs> LocalizationProgressUpdated;
  }
}
