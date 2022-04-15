// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Localization
{
  /// @cond ARDK_VPS_BETA
  /// Configuration information for a VPS localization attempt. 
  /// Create using [LocalizationConfigurationFactory.Create()]
  /// (@ref ARDK.AR.Localization.LocalizationConfigurationFactory) and provide in calls to 
  /// [ILocalizer.StartLocalization(config)](@ref ARDK.AR.Localization.ILocalizer.StartLocalization)
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public interface ILocalizationConfiguration:
    IDisposable
  {
    /// @cond ARDK_VPS_BETA
    /// The identifier of the map to attempt to localize against. If MapIdentifier is null, the 
    /// localization attempt will be against all maps in the area (by GPS). If it is populated, 
    /// only the specified map will be localized against.
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    string MapIdentifier { get; set; }

    /// @cond ARDK_VPS_BETA
    /// The timeout in seconds for the entire localization attempt. An attempt will send
    /// localization requests until the localization succeeds, times out, or is canceled.
    /// The default is 30 seconds.
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    float LocalizationTimeout { get; set; }

    /// @cond ARDK_VPS_BETA
    /// The timeout in seconds for an individual request made during the overall localization attempt. 
    /// The default is 10 seconds.
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    float RequestTimeLimit { get; set; }

    /// @cond ARDK_VPS_BETA
    /// The endpoint for VPS localization API requests
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    string LocalizationEndpoint { get; set; }
  }
}
