using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Localization;

namespace Niantic.ARDK.AR
{
  /// @cond ARDK_VPS_BETA
  /// An AR session that includes an ILocalizer.
  /// Cast your current IARSession to a ILocalizableARSession to access the 
  /// ILocalizer interface, for example:
  /// @code
  /// if (_arSession is ILocalizableARSession localizableARSession)
  /// {
  ///    var localizer = localizableARSession.Localizer;
  ///    // ...configure and start localization request...
  /// }
  /// @endcode
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @see [ILocalizer](@ref ARDK.AR.Localization.ILocalizer)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public interface ILocalizableARSession : 
    IARSession
  {
    /// @cond ARDK_VPS_BETA
    /// Get the localizer associated with this session
    /// @endcond
    /// @note This is an experimental feature, and may be changed or removed in a future release.
    ///   This feature is currently not functional or supported.
    ILocalizer Localizer { get; }
  }
}
