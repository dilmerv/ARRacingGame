// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDKExamples.Gameboard
{
  public interface IGameBoard
  {
    /// The configuration this game board was created with.
    BoardConfiguration Configuration { get; }
    
    /// The number of distinct planes discovered.
    int NumberOfPlanes { get; }
    
    /// Creates a copy of the internal surface container to inspect.
    ReadOnlyCollection<Surface> Surfaces { get; }
    
    /// The tree structure used to locate grid nodes.
    SpatialTree SpatialTree { get; }

    /// Searches for walkable areas in the environment.
    /// @param origin Origin of the scan in world position.
    /// @param range Range from origin to consider.
    void Scan(Vector3 origin, float range);
    
    /// Removes all surfaces from the board.
    void Clear();

    /// Removes nodes outside the specified area.
    /// @param keepNodesFromOrigin Defines an origin in world position from which nodes will be retained.
    /// @param withinExtent Extent (metric) of the area where nodes need to be retained.
    void Clear(Vector3 keepNodesFromOrigin, float withinExtent);

    /// Checks whether an area is free to occupy (by a game object).
    /// @param center Origin of the area in world position.
    /// @param extent Width/Length (metric) of the object's estimated footprint.
    bool CanFitObject(Vector3 center, float extent);

    /// Checks whether the specified (projected) world position is on the game board.
    /// @param x World coordinate on the right axis.
    /// @param z World coordinate ob the forward axis.
    /// @param elevation The elevation of the surface the point of part of, if any.
    /// @returns True, if the specified point is on the game board.
    bool IsWalkable(float x, float z, out float elevation);

    /// Finds the nearest walkable world position on the game board within a specified range.
    /// @param sourcePosition The origin of the search.
    /// @param range Defines the search window (size = 2 * range).
    /// @param nearestPosition The resulting nearest position, if any.
    /// @returns True, if a nearest point could be found.
    bool SamplePosition(Vector3 sourcePosition, float range, out Vector3 nearestPosition);

    /// Finds the nearest walkable world position on the game board to the specified reference position.
    /// @param reference The position top fond the closest point to.
    /// @param nearest The approximate nearest position, if any.
    /// @returns True, if a nearest point could be found.
    bool FindNearest(Vector3 reference, out Vector3 nearest);

    /// Finds a random walkable world position on the game board within a specified range.
    /// @param sourcePosition The origin of the search.
    /// @param range Defines the search window (size = 2 * range).
    /// @param nearestPosition The resulting nearest position, if any.
    /// @returns True, if a point could be found.
    bool FindRandomPosition(Vector3 sourcePosition, float range, out Vector3 randomPosition);
    
    /// Calculates a walkable path between the two specified positions.
    /// @param fromPosition Start position.
    /// @param toPosition Destination position
    /// @param agent Configuration for the agent the path is calculated for.
    /// @returns A list of waypoints in world coordinates.
    List<Waypoint> CalculatePath
    (
      Vector3 fromPosition,
      Vector3 toPosition,
      AgentConfiguration agent
    );
    
    /// Raycasts the specified plane of the GameBoard.
    /// @param surface The surface within the game board to raycast.
    /// @param ray Ray to perform this function with.
    /// @param hitPoint Hit point in world coordinates, if any.
    /// @returns True if the ray hit a point on the target plane.
    bool RayCast(Surface surface, Ray ray, out Vector3 hitPoint);
    
    /// Raycasts the GameBoard.
    /// @param ray Ray to perform this function with.
    /// @param surface The surface hit by the ray, if any.
    /// @param hitPoint Hit point in world coordinates, if any.
    /// @returns True if the ray hit a point on any plane within the game board.
    bool RayCast(Ray ray, out Surface surface, out Vector3 hitPoint);
    
    /// Builds the geometry of the provided plane and copies it to the mesh.
    /// @note Any previous data stored in the mesh will be cleared.
    /// @param surface Surface to visualize.
    /// @param mesh A pre-allocated mesh.
    void UpdateSurfaceMesh(Surface surface, Mesh mesh);
    
    /// Returns the closest point on the specified surface to the reference point.
    /// @param surface The surface to find the closest point on to the reference.
    /// @param reference The reference point to find the closest point to.
    /// @returns The closest point to the reference in world coordinates.
    [Obsolete("Use FindNearest(Vector3 reference, out Vector3 nearest) instead.")]
    Vector3 GetClosestPointOnSurface(Surface surface, Vector3 reference);
    
    /// Converts a node on the game board to its corresponding world position.
    /// @param node A grid node acquired from an existing plane of the game board.
    /// @returns World position of the centroid of the node.
    Vector3 GridNodeToPosition(GridNode node);

    /// Centers the specified world position on the underlying grid tile.
    /// If the specified position is walkable, the elevation will be adjusted as well.
    /// @param position The world position to center to its tile.
    Vector3 SnapToGrid(Vector3 position);
  }
}
