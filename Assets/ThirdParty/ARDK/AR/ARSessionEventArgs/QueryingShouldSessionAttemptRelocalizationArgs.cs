// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public sealed class QueryingShouldSessionAttemptRelocalizationArgs:
    IArdkEventArgs
  {
    /// <summary>
    /// Set this property to true if the session should attempt a relocalization.
    /// </summary>
    public bool ShouldSessionAttemptRelocalization { get; set; }
  }
}
