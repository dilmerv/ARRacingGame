// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDKExamples.Gameboard
{
  /// Encloses data for grid elements used during path finding.
  public struct PathNode: IEquatable<PathNode>, IComparable<PathNode>
  {
    /// The coordinates of this parent.
    public readonly Vector2Int Coordinates;
    
    /// The coordinates of this node's parent.
    public readonly Vector2Int ParentCoordinates;
    
    /// Elevation of the node.
    public readonly float Elevation;
    
    /// Whether this path node has a parent assign.
    public readonly bool HasParent;

    /// Cost to get to this node for the source in a path finding context.
    public int CostToThis;

    /// Cost to get from this node to the destination in a path finding context.
    public float CostToGoal;

    /// The number of continuous nodes without a surface.
    public int AggregateOffSurface;

    /// The surface this node belongs to. Could be null.
    public readonly Surface Surface;
    
    /// Combined cost of this node.
    public float Cost
    {
      get
      {
        return CostToThis + CostToGoal;
      }
    }

    public PathNode(Vector2Int coordinates, Surface surface)
      : this()
    {
      Coordinates = coordinates;
      Elevation = surface.Elevation;
      Surface = surface;
    }
    
    public PathNode(Vector2Int coordinates, float elevation)
      : this()
    {
      Coordinates = coordinates;
      Elevation = elevation;
      Surface = null;
    }

    public PathNode(Vector2Int coordinates, Surface surface, Vector2Int parentCoordinates)
      : this(coordinates, surface)
    {
      ParentCoordinates = parentCoordinates;
      HasParent = true;
    }
    
    public PathNode(Vector2Int coordinates, float elevation, Vector2Int parentCoordinates)
      : this(coordinates, elevation)
    {
      ParentCoordinates = parentCoordinates;
      HasParent = true;
    }

    public bool Equals(PathNode other)
    {
      return Coordinates.Equals(other.Coordinates);
    }

    public override int GetHashCode()
    {
      return Coordinates.GetHashCode();
    }

    public int CompareTo(PathNode other)
    {
      return Cost.CompareTo(other.Cost);
    }
  }
}