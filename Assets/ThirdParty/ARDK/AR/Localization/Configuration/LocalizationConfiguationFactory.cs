// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Localization
{
  /// @cond ARDK_VPS_BETA
  /// Class factory for [LocalizationConfiguration]
  /// (@ref ARDK.AR.Localization.ILocalizationConfiguration)
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public static class LocalizationConfigurationFactory
  {
    /// Initializes a new instance of the LocalizationConfiguration class.
    public static ILocalizationConfiguration Create()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return new _NativeLocalizationConfiguration();

#pragma warning disable 0162
      return new _SerializableLocalizationConfiguration();
#pragma warning restore 0162
    }
  }
}
