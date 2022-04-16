// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Niantic.ARDK.Configuration;
using Niantic.ARDK.Utilities.Collections;

using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Niantic.ARDK.Networking.Authentication {
  /// <summary>
  /// Component that manages obtaining an Auth Token from the ARBE on startup. Will fill in a
  /// ServerConfiguration object upon successful authentication which is necessary to make a
  /// connection to the ARBEs.
  /// </summary>
  [Obsolete("This component is obsolete as of ARDK 0.8.0, add an ARDKAuthConfig to your Resources/ARDK directory instead")]
  public sealed class ARDKAuthRegistrar : MonoBehaviour 
  {
  }
}
