// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities.Marker
{
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  [Serializable]
  public class MarkerMetadata
  {
    public enum MarkerSource
    {
      Device,
      Stationary
    }

    public MarkerSource Source { get; protected set; }

    public string SessionIdentifier;

    public byte[] Data;

    protected IMetadataSerializer Serializer { get; set; }

    protected MarkerMetadata()
    {
    }

    /// <summary>
    /// Base constructor.
    /// </summary>
    /// <param name="sessionIdentifier">Name of networking session scanners of this marker will join.</param>
    /// <param name="source">How this marker will be displayed
    ///   (On device or on something stationary in the real physical world).</param>
    /// <param name="data">Any user defined data that should be also embedded in the barcode.</param>
    /// <param name="serializer">Will use the BasicMetadataSerializer if this arg is left null.</param>
    public MarkerMetadata
    (
      string sessionIdentifier,
      MarkerSource source,
      byte[] data,
      IMetadataSerializer serializer = null
    )
    {
      SessionIdentifier = sessionIdentifier;
      Source = source;
      Data = data;

      Serializer = serializer ?? new BasicMetadataSerializer();
    }

    public byte[] Serialize()
    {
      return Serializer.Serialize(this);
    }

    public override string ToString()
    {
      return
        string.Format
        (
          "MarkerMetadata (SessionIdentifier: {0}, Source: {1}, Data Length: {2})",
          SessionIdentifier,
          Source,
          Data == null ? 0 : Data.Length
        );
    }
  }
}
