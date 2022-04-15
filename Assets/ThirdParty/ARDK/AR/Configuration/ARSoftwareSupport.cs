// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Configuration
{
  /// Possible states of support by the currently installed software for a particular
  /// configuration.
  public enum ARSoftwareSupport
  {
    /// The current software level does not support a particular configuration.
    NotSupported = 0,

    /// Software of operating system or native AR service too low for a particular configuration.
    /// Or specific software is not installed (e.g. ArCore)
    SupportedNeedsUpdate = 1,

    /// Software supports a particular configuration.
    Supported = 2,
  }
}
