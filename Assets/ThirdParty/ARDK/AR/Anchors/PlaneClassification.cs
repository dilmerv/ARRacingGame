// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Anchors
{
  /// <summary>
  /// Values describing possible characterizations of real-world surfaces represented by plane anchors.
  /// </summary>
  public enum PlaneClassification
  {
    /// No classification is available for the plane anchor.
    None = 0,

    /// The plane anchor represents a real-world wall or similar large vertical surface.
    Wall = 1,

    /// The plane anchor represents a real-world floor, ground plane, or similar large
    /// horizontal surface.
    Floor = 2,

    /// The plane anchor represents a real-world ceiling or similar overhead horizontal surface.
    Ceiling = 3,

    /// The plane anchor represents a real-world table, desk, bar, or similar flat surface.
    Table = 4,

    /// The plane anchor represents a real-world chair, stool, bench or similar flat surface.
    Seat = 5,

    /// The plane anchor represents a real-world door or similar vertical surface.
    Door = 6,

    /// The plane anchor represents a real-world window or similar vertical surface.
    Window = 7
  }
}
