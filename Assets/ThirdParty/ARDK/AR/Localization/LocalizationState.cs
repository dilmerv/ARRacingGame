// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Localization
{
  /// @cond ARDK_VPS_BETA
  /// Possible states for a localization
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public enum LocalizationState
  {
    /// System is using device and GPS information to determine if localization is possible.
    Initializing = 0,
    
    /// Localization in process
    Localizing = 1,
    
    /// Localization succeeded.
    Localized = 2,
    
    /// Localization failed, a failure reason will be provided.
    Failed = 3
  }
}
