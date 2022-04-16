// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using UnityEngine;

using Random = UnityEngine.Random;

namespace Niantic.ARDKExamples.Gameboard
{
  /// Provides information for navigation. Requires meshing to be enabled.
  public class GameBoard: IGameBoard
  {
    private readonly BoardConfiguration _settings;

    // Contains walkable grid positions in quad-tree structures
    private readonly SpatialTree _spatialTree;

    // Internal container for all surfaces.
    private readonly List<Surface> _surfaces = new List<Surface>();

    /// Returns the number of distinct planes that have been discovered.
    public int NumberOfPlanes
    {
      get => _surfaces.Count;
    }

    /// Creates a copy of the internal surface container.
    public ReadOnlyCollection<Surface> Surfaces
    {
      get => _surfaces.AsReadOnly();
    }

    /// The tree structure used to locate grid nodes.
    public SpatialTree SpatialTree
    {
      get => _spatialTree;
    }

    /// The configuration this game board was created with.
    public BoardConfiguration Configuration
    {
      get => _settings;
    }

    /// Allocates a new game board.
    /// @param settings Settings to calibrate walkable area detection.
    public GameBoard(BoardConfiguration settings)
    {
      if (settings.TileSize <= 0)
        throw new ArgumentException("Tile size must be greater than zero.");

      if (settings.KernelSize % 2 == 0)
        throw new ArgumentException("Kernel size must be an odd number.");

      if (settings.MaxSlope > 40.0f)
        throw new ArgumentException("MaxSlope must be less than  or equal to 40 degrees.");

      _settings = settings;

      // Allocate a grid for storing walkable nodes
      _spatialTree = new SpatialTree(Mathf.FloorToInt(settings.SpatialChunkSize / settings.TileSize));
    }

    /// Checks whether the specified (projected) world position is on the game board.
    /// @param x World coordinate on the right axis.
    /// @param z World coordinate ob the forward axis.
    /// @param elevation The elevation of the surface the point of part of, if any.
    /// @returns True, if the specified position is on the game board.
    public bool IsWalkable(float x, float z, out float elevation)
    {
      // Convert world position to grid coordinates
      var tile = new Vector2Int
      (
        Mathf.FloorToInt(x / _settings.TileSize),
        Mathf.FloorToInt(z / _settings.TileSize)
      );

      if (_spatialTree.GetElement(tile, out var node))
      {
        elevation = node.Elevation;
        return true;
      }

      elevation = float.MinValue;
      return false;
    }

    /// Finds the nearest walkable world position on the game board within a specified range.
    /// @param sourcePosition The origin of the search.
    /// @param range Defines the search window (size = 2 * range).
    /// @param nearestPosition The resulting nearest position, if any.
    /// @returns True, if a nearest point could be found.
    public bool SamplePosition(Vector3 sourcePosition, float range, out Vector3 nearestPosition)
    {
      var referencePoint = PositionToTile(sourcePosition);

      // Define the search window
      var halfSize = Mathf.FloorToInt(range / _settings.TileSize);
      var anchor = new Vector2Int(referencePoint.x - halfSize, referencePoint.y - halfSize);
      var boundsOfSearch = new Bounds
      (
        bottomLeft: anchor,
        size: halfSize * 2
      );

      // Extract points within the search bounds
      var pointsOfInterest = _spatialTree.Query(withinBounds: boundsOfSearch).ToList();

      // Find the closest point from candidates
      var success = GetNearestNode(pointsOfInterest, referencePoint, out var nearestNode);

      // Convert to world position
      nearestPosition = GridNodeToPosition(nearestNode);
      return success;
    }

    /// Finds the nearest walkable world position on the game board to the specified reference position.
    /// @param reference The position top fond the closest point to.
    /// @param nearest The approximate nearest position, if any.
    /// @returns True, if a nearest point could be found.
    public bool FindNearest(Vector3 reference, out Vector3 nearest)
    {
      // Get reference coordinates
      var referencePoint = PositionToTile(reference);

      // Get neighboring points
      var pointsOfInterest = _spatialTree.Query(neighboursTo: referencePoint).ToList();

      // Find the closest point from candidates
      var success = GetNearestNode(pointsOfInterest, referencePoint, out var nearestNode);

      // Convert to world position
      nearest = GridNodeToPosition(nearestNode);
      return success;
    }

    /// Finds a random walkable world position on the game board within a specified range.
    /// @param sourcePosition The origin of the search.
    /// @param range Defines the search window (size = 2 * range).
    /// @param nearestPosition The resulting nearest position, if any.
    /// @returns True, if a point could be found.
    public bool FindRandomPosition(Vector3 sourcePosition, float range, out Vector3 randomPosition)
    {
      var referencePoint = PositionToTile(sourcePosition);

      // Define the search window
      var halfSize = Mathf.FloorToInt(range / _settings.TileSize);
      var anchor = new Vector2Int(referencePoint.x - halfSize, referencePoint.y - halfSize);
      var boundsOfSearch = new Bounds
      (
        bottomLeft: anchor,
        size: halfSize * 2
      );

      // Extract points within the search bounds
      var pointsOfInterest = _spatialTree.Query(withinBounds: boundsOfSearch).ToList();
      if (pointsOfInterest.Count > 0)
      {
        // Get random walkable position
        var idx = Random.Range(0, pointsOfInterest.Count - 1);
        randomPosition = GridNodeToPosition(pointsOfInterest[idx]);
        return true;
      }

      // The search didn't yield any walkable nodes within bounds
      randomPosition = default;
      return false;
    }

    /// Centers the specified world position on the underlying grid tile.
    /// If the specified position is walkable, the elevation will be adjusted as well.
    /// @param position The world position to center to its tile.
    public Vector3 SnapToGrid(Vector3 position)
    {
      // Convert world position to grid coordinates
      var tile = PositionToTile(position);

      return _spatialTree.GetElement(tile, out var node)
        ? GridNodeToPosition(node)
        : TileToPosition(tile, elevation: position.y);
    }

    /// Raycasts the specified plane of the GameBoard.
    /// @param surface The surface within the game board to raycast.
    /// @param ray Ray to perform this function with.
    /// @param hitPoint Hit point in world coordinates, if any.
    /// @returns True if the ray hit a point on the target plane.
    public bool RayCast(Surface surface, Ray ray, out Vector3 hitPoint)
    {
      // Initialize resulting point
      hitPoint = Vector3.zero;

      if (surface.IsEmpty)
      {
        return false;
      }

      // Construct a mathematical plane
      var position = TileToPosition(surface.Elements.FirstOrDefault().Coordinates);
      var p = new UnityEngine.Plane
        (Vector3.up, new Vector3(position.x, surface.Elevation, position.y));

      // Raycast plane
      if (p.Raycast(ray, out float enter))
      {

        // Check whether the hit point refers to a valid tile on the plane
        hitPoint = ray.GetPoint(enter);
        return surface.ContainsElement(PositionToGridNode(hitPoint));
      }

      return false;
    }

    /// Raycasts the GameBoard.
    /// @param ray Ray to perform this function with.
    /// @param surface The surface hit by the ray, if any.
    /// @param hitPoint Hit point in world coordinates, if any.
    /// @returns True if the ray hit a point on any plane within the game board.
    public bool RayCast(Ray ray, out Surface surface, out Vector3 hitPoint)
    {
      hitPoint = Vector3.zero;
      surface = null;

      var didHit = false;
      var minDistance = float.MaxValue;
      foreach (var entry in _surfaces)
      {
        if (RayCast(entry, ray, out Vector3 raycastHit))
        {
          didHit = true;
          var dist = Vector3.Distance(ray.origin, raycastHit);
          if (dist < minDistance)
          {
            minDistance = dist;
            hitPoint = raycastHit;
            surface = entry;
          }
        }
      }

      return didHit;
    }

    /// Builds the geometry of the provided plane and copies it to the mesh.
    /// @note Any previous data stored in the mesh will be cleared.
    /// @param surface Surface to visualize.
    /// @param mesh A pre-allocated mesh.
    public void UpdateSurfaceMesh(Surface surface, Mesh mesh)
    {
      var vertices = new List<Vector3>();
      var triangles = new List<int>();
      var vIndex = 0;
      var halfSize = _settings.TileSize / 2.0f;
      foreach (var center in surface.Elements.Select
        (node => TileToPosition(node.Coordinates, surface.Elevation)))
      {
        // Vertices
        vertices.Add(center + new Vector3(-halfSize, 0.0f, -halfSize));
        vertices.Add(center + new Vector3(halfSize, 0.0f, -halfSize));
        vertices.Add(center + new Vector3(halfSize, 0.0f, halfSize));
        vertices.Add(center + new Vector3(-halfSize, 0.0f, halfSize));


        // Indices
        triangles.Add(vIndex + 2);
        triangles.Add(vIndex + 1);
        triangles.Add(vIndex);

        triangles.Add(vIndex);
        triangles.Add(vIndex + 3);
        triangles.Add(vIndex + 2);

        vIndex += 4;
      }

      mesh.Clear();
      mesh.vertices = vertices.ToArray();
      mesh.triangles = triangles.ToArray();
      mesh.UploadMeshData(markNoLongerReadable: false);
    }

    /// Returns the closest point on the specified surface to the reference point.
    /// @param surface The surface to find the closest point on to the reference.
    /// @param reference The reference point to find the closest point to.
    /// @returns The closest point to the reference in world coordinates.
    public Vector3 GetClosestPointOnSurface(Surface surface, Vector3 reference)
    {
      var result = surface.GetClosestElement(PositionToTile(reference));
      return TileToPosition(result.Coordinates, result.Elevation);
    }

    /// Calculates a walkable path between the two specified positions.
    /// @param fromPosition Start position.
    /// @param toPosition Destination position
    /// @param agent The configuration of the agent is path is calculated for.
    /// @returns A list of waypoints in world coordinates.
    public List<Waypoint> CalculatePath
      (Vector3 fromPosition, Vector3 toPosition, AgentConfiguration agent)
    {
      // Convert world positions to coordinates on the grid
      var source = PositionToGridNode(fromPosition);
      var destination = PositionToGridNode(toPosition);

      // Attempting to get path to the same tile
      if (source.Equals(destination))
      {
        Debug.LogWarning("Attempted to calculate path to the same position.");
        return new List<Waypoint>();
      }

      // Find the subject surface on the GameBoard
      var startSurface = _surfaces.FirstOrDefault(p => p.ContainsElement(source));
      if (startSurface == null)
      {
        Debug.LogWarning("Could not locate start position on any surface.");
        return new List<Waypoint>();
      }

      switch (agent.Behaviour)
      {
        case PathFindingBehaviour.SingleSurface:
          return CalculatePathOnSurface(startSurface, source, destination, out Vector2Int _);

        case PathFindingBehaviour.InterSurfacePreferPerformance:
          return CalculatePathOnBoardLocal(startSurface, source, destination, agent);

        case PathFindingBehaviour.InterSurfacePreferResults:
          return CalculatePathOnBoardGlobal(startSurface, source, destination, agent);

        default:
          throw new NotImplementedException();
      }
    }

    private List<Waypoint> CalculatePathOnSurface
    (
      Surface surface,
      GridNode source,
      GridNode destination,
      out Vector2Int closestCoordinateToDestination
    )
    {
      var costToGoal = Vector2Int.Distance(source.Coordinates, destination.Coordinates);
      var start = new PathNode(source.Coordinates, surface)
      {
        CostToGoal = costToGoal
      };

      var open = new List<PathNode>
      {
        start
      };

      var closed = new HashSet<PathNode>();

      // This is a substitute for the destination if it cannot be found on the surface
      var closestNodeToGoal = start;

      while (open.Count > 0)
      {
        // Get the most eligible node to continue traversal
        var current = open[0];
        open.RemoveAt(0);
        closed.Add(current);

        if (current.CostToGoal < closestNodeToGoal.CostToGoal)
        {
          closestNodeToGoal = current;
        }

        // Find neighbours on the plane
        var neighbours = GetNeighbours(current.Coordinates);
        foreach (var coords in neighbours)
        {
          if (!surface.ContainsElement(new GridNode(coords)))
          {
            // Discard this neighbour, since it cannot be found on the same plane
            continue;
          }

          // Potential successor
          var successor = new PathNode(coords, surface, current.Coordinates);

          // We arrived at the goal
          if (successor.Coordinates.Equals(destination.Coordinates))
          {
            closestCoordinateToDestination = successor.Coordinates;
            return GeneratePath(nodes: closed, traceStart: successor);
          }

          // We have already processed this grid cell.
          if (closed.Contains(successor))
          {
            continue;
          }

          // Calculate costs
          successor.CostToThis = current.CostToThis + Utils.ManhattanDistance(successor, current);
          successor.CostToGoal = Vector2Int.Distance
            (destination.Coordinates, successor.Coordinates);

          var existingIndex = open.FindIndex
            (openNode => openNode.Coordinates.Equals(successor.Coordinates));

          if (existingIndex >= 0)
          {
            var existing = open[existingIndex];
            if (existing.Cost <= successor.Cost)
            {
              continue;
            }

            open.RemoveAt(existingIndex);
          }

          open.InsertIntoSortedList(successor, (a, b) => a.CompareTo(b));
        }
      }

      // We have reached the closest position to our destination on this surface
      closestCoordinateToDestination = closestNodeToGoal.Coordinates;
      return GeneratePath(nodes: closed, traceStart: closestNodeToGoal);
    }

    private List<Waypoint> CalculatePathOnBoardLocal
      (Surface startSurface, GridNode source, GridNode destination, AgentConfiguration agent)
    {
      // We will use this origin to test its neighbouring nodes on the grid for validity to be jumped over or onto
      var result = CalculatePathOnSurface(startSurface, source, destination, out Vector2Int searchOrigin);

      var currentSurface = startSurface;
      var closestCoordinateToDestination = searchOrigin;
      var nextOrigin = searchOrigin;

      while (!searchOrigin.Equals(destination.Coordinates))
      {
        var continueSearch = false;
        var neighbours = GetNeighbours(searchOrigin)
          .OrderBy(coords => Vector2Int.Distance(coords, destination.Coordinates))
          .ToArray();

        foreach (var neighbour in neighbours)
        {
          var inspectedNeighbour = new GridNode(neighbour);

          // The inspected node can't belong to the same plane we're currently on and it has to be within the specified jump distance
          var isValidNeighbour = !currentSurface.ContainsElement(inspectedNeighbour) &&
            Vector2Int.Distance
              (inspectedNeighbour.Coordinates, closestCoordinateToDestination) *
            _settings.TileSize <
            agent.JumpDistance &&
            Vector2Int.Distance
              (inspectedNeighbour.Coordinates, destination.Coordinates) <
            Vector2Int.Distance(searchOrigin, destination.Coordinates);

          if (!isValidNeighbour)
          {
            continue;
          }

          if (!continueSearch)
          {
            // We store the new closest node to the destination
            // in case we can't find a valid node to jump to in this range...
            nextOrigin = inspectedNeighbour.Coordinates;
            continueSearch = true;
          }

          // Check whether the inspected node belongs to any other existing plane
          var nextSurface = _surfaces.FirstOrDefault
            (surface => surface.ContainsElement(inspectedNeighbour));

          if (nextSurface != null)
          {
            // Can we jump here?
            if (Vector3.Distance
              (
                TileToPosition(closestCoordinateToDestination, currentSurface.Elevation),
                TileToPosition(nextOrigin, nextSurface.Elevation)
              ) <
              agent.JumpDistance)
            {
              // New surface found!
              inspectedNeighbour.Elevation = nextSurface.Elevation;
              var subRoute = CalculatePathOnSurface
                (nextSurface, inspectedNeighbour, destination, out nextOrigin);

              closestCoordinateToDestination = nextOrigin;
              currentSurface = nextSurface;
              result.AddRange(subRoute);

              break;
            }
          }
        }

        if (!continueSearch)
        {
          break;
        }

        searchOrigin = nextOrigin;
      }

      return result;
    }

    private List<Waypoint> CalculatePathOnBoardGlobal
      (Surface startSurface, GridNode source, GridNode destination, AgentConfiguration agent)
    {
      var costToGoal = Vector2Int.Distance(source.Coordinates, destination.Coordinates);
      var start = new PathNode(source.Coordinates, startSurface)
      {
        CostToGoal = costToGoal
      };

      var open = new List<PathNode>
      {
        start
      };

      var closed = new HashSet<PathNode>();

      var closestNodeToGoal = start;

      while (open.Count > 0)
      {
        // Get the most eligible node to continue traversal
        var current = open[0];
        open.RemoveAt(0);
        closed.Add(current);

        if (current.CostToGoal < closestNodeToGoal.CostToGoal && current.Surface != null)
        {
          closestNodeToGoal = current;
        }

        // Find neighbours on the plane
        var neighbours = GetNeighbours(current.Coordinates);
        foreach (var coords in neighbours)
        {
          var node = new GridNode(coords);
          var surfaceOfNeighbour = current.Surface == null || !current.Surface.ContainsElement(node)
            ? _surfaces.FirstOrDefault(s => s.ContainsElement(node))
            : current.Surface;

          var offSurface = surfaceOfNeighbour == null;

          // Potential successor
          var successor = offSurface
            ? new PathNode(coords, current.Elevation, parentCoordinates: current.Coordinates)
            : new PathNode(coords, surfaceOfNeighbour, parentCoordinates: current.Coordinates);

          var aggregateOffSurface = offSurface
            ? current.AggregateOffSurface + 1
            : 0;

          var elevationDiff = Mathf.Abs(successor.Elevation - current.Elevation);
          var offSurfaceDist = aggregateOffSurface * _settings.TileSize;
          var jumpDistance = Mathf.Sqrt
            (elevationDiff * elevationDiff + offSurfaceDist * offSurfaceDist);

          if (jumpDistance > agent.JumpDistance)
          {
            continue;
          }

          // We arrived at the goal
          if (!offSurface && successor.Coordinates.Equals(destination.Coordinates))
          {
            return GeneratePath(nodes: closed, traceStart: successor);
          }

          // We have already processed this grid cell.
          if (closed.Contains(successor))
          {
            continue;
          }

          // Calculate costs
          var isJump = elevationDiff > 0 || offSurface;
          successor.AggregateOffSurface = aggregateOffSurface;
          successor.CostToThis = current.CostToThis +
            Utils.ManhattanDistance(successor, current) +
            (isJump ? agent.JumpPenalty : 0);

          successor.CostToGoal = Vector2Int.Distance
            (destination.Coordinates, successor.Coordinates);

          var existingIndex = open.FindIndex
            (openNode => openNode.Coordinates.Equals(successor.Coordinates));

          if (existingIndex >= 0)
          {
            var existing = open[existingIndex];
            if (existing.Cost <= successor.Cost)
            {
              continue;
            }

            open.RemoveAt(existingIndex);
          }

          open.InsertIntoSortedList(successor, (a, b) => a.CompareTo(b));
        }
      }

      // We have reached the closest position to our destination on this surface
      return GeneratePath(nodes: closed, traceStart: closestNodeToGoal);
    }

    /// Searches for walkable areas in the environment.
    /// @param origin Origin of the scan in world position.
    /// @param range Range from origin to consider
    public void Scan(Vector3 origin, float range)
    {
      // Cache parameters
      var kernelSize = _settings.KernelSize;
      var kernelHalfSize = kernelSize / 2;
      var tileSize = _settings.TileSize;
      var tileHalfSize = tileSize / 2.0f;
      const float rayLength = 100.0f;

      // Calculate bounds for this scan on the grid
      var lowerBounds = PositionToTile(new Vector2(origin.x - range, origin.z - range));
      var upperBounds = PositionToTile(new Vector2(origin.x + range, origin.z + range));
      if (upperBounds.x - lowerBounds.x < kernelSize ||
        upperBounds.y - lowerBounds.y < kernelSize)
      {
        throw new ArgumentException("Range is too short for the specified kernel size.");
      }

      // Bounds of the search area
      var w = upperBounds.x - lowerBounds.x;
      var h = upperBounds.y - lowerBounds.y;

      // Array to store information on the nodes resulting from this scan
      var scanArea = new GridNode[w * h];

      // Scan heights
      for (var x = 0; x < w; x++)
      {
        for (var y = 0; y < h; y++)
        {

          // Calculate the world position of the ray
          var coords = new Vector2Int(lowerBounds.x + x, lowerBounds.y + y);
          var position = new Vector3
          (
            coords.x * tileSize + tileHalfSize,
            origin.y,
            coords.y * tileSize + tileHalfSize
          );

          var arrayIndex = y * w + x;

          // Raycast for height
          var elevation =
            Physics.Raycast
            (
              new Ray(position, Vector3.down),
              out RaycastHit hit,
              rayLength,
              layerMask: _settings.LayerMask
            )
              ? hit.point.y
              : -100; // TODO: What should be the default value in case of no hit?

          scanArea[arrayIndex] = new GridNode(coords)
          {
            DiffFromNeighbour = float.MaxValue, Elevation = elevation
          };
        }
      }

      // This set is used to register nodes that are obviously not walkable
      var invalidate = new HashSet<GridNode>();

      // Calculate areal properties
      var kernel = new Vector3[kernelSize * kernelSize];
      for (var x = kernelHalfSize; x < w - kernelHalfSize; x++)
      {
        for (var y = kernelHalfSize; y < h - kernelHalfSize; y++)
        {
          // Construct kernel for this grid cell using its neighbours
          var kernelIndex = 0;
          for (var kx = -kernelHalfSize; kx <= kernelHalfSize; kx++)
          {
            for (var ky = -kernelHalfSize; ky <= kernelHalfSize; ky++)
            {
              var x1 = Mathf.Clamp(kx + x, 0, w - 1);
              var y1 = Mathf.Clamp(ky + y, 0, h - 1);
              kernel[kernelIndex++] = GridNodeToPosition(scanArea[y1 * w + x1]);
            }
          }

          var idx = y * w + x;

          // Try to fit a plane on the neighbouring points
          Utils.FastFitPlane(kernel, out Vector3 _, out Vector3 normal);

          // Assign standard deviation and slope angle
          var slope = Mathf.Abs(90.0f - Vector3.Angle(Vector3.forward, normal));
          var std = Utils.CalculateStandardDeviation(kernel.Select(pos => pos.y));
          scanArea[idx].Deviation = std;

          // Collect nodes that are not walkable
          var isWalkable = std < _settings.KernelStdDevTol &&
            slope < _settings.MaxSlope &&
            scanArea[idx].Elevation > _settings.MinElevation;

          if (!isWalkable)
          {
            invalidate.Add(scanArea[idx]);
          }
        }
      }

      // Remove nodes that are not walkable from existing planes
      InvalidateNodes(invalidate);

      var open = new Queue<GridNode>();
      var closed = new HashSet<GridNode>();
      var eligible = new HashSet<GridNode>();

      // Define seed as the center of the search area
      open.Enqueue(scanArea[(h / 2) * w + (w / 2)]);
      while (open.Count > 0)
      {
        // Extract current tile
        var currentNode = open.Dequeue();

        // Consider this node to be visited
        closed.Add(currentNode);

        if (invalidate.Contains(currentNode))
        {
          continue; // Skip this node as it is not walkable
        }

        // Register this tile as walkable...
        eligible.Add(currentNode);

        var neighbours = GetNeighbours(currentNode.Coordinates);
        foreach (var neighbour in neighbours)
        {

          // Get the coordinates transformed to our local scan area
          var transformedNeighbour = neighbour - lowerBounds;
          if (transformedNeighbour.x < kernelHalfSize ||
            transformedNeighbour.x >= w - kernelHalfSize ||
            transformedNeighbour.y < kernelHalfSize ||
            transformedNeighbour.y >= h - kernelHalfSize)
          {
            continue; // Out of bounds
          }

          var arrayIndex = transformedNeighbour.y * w + transformedNeighbour.x;

          // If we've been here before
          if (closed.Contains(scanArea[arrayIndex]))
          {
            continue;
          }

          var diff = Mathf.Abs(currentNode.Elevation - scanArea[arrayIndex].Elevation);
          if (scanArea[arrayIndex].DiffFromNeighbour > diff)
          {
            scanArea[arrayIndex].DiffFromNeighbour = diff;
          }

          // Can we walk from the current node to this neighbour?
          var isEligible = !open.Contains(scanArea[arrayIndex]) &&
            scanArea[arrayIndex].DiffFromNeighbour <= _settings.StepHeight;

          if (isEligible)
          {
            open.Enqueue(scanArea[arrayIndex]);
          }
        }
      }

      if (eligible.Count >= 2)
      {
        // Merge newly found walkable areas with existing planes
        MergeNodes(eligible);
      }
    }

    /// Removes all surfaces from the board.
    public void Clear()
    {
      _surfaces.Clear();
      _spatialTree.Clear();
    }

    /// Removes nodes outside the specified area.
    /// @param keepNodesFromOrigin Defines an origin in world position from which nodes will be retained.
    /// @param withinExtent Extent (metric) of the area where nodes need to be retained.
    public void Clear(Vector3 keepNodesFromOrigin, float withinExtent)
    {
      var topRight = keepNodesFromOrigin +
        Vector3.right * withinExtent +
        Vector3.forward * withinExtent;

      var bottomLeft = keepNodesFromOrigin +
        Vector3.left * withinExtent +
        Vector3.back * withinExtent;

      var min = PositionToGridNode(bottomLeft);
      var max = PositionToGridNode(topRight);

      var bounds = new Bounds(min.Coordinates, max.Coordinates.x - min.Coordinates.x);
      var toKeep = _spatialTree.Query(withinBounds: bounds).ToList();

      _spatialTree.Clear();
      _spatialTree.Insert(toKeep);

      // Remove tiles for surfaces
      _surfaces.ForEach(surface => surface.Intersect(toKeep));

      // Clean empty surfaces
      _surfaces.RemoveAll(surface => surface.IsEmpty);
    }

    /// Checks whether an area is free to occupy (by a game object).
    /// @param center Origin of the area in world position.
    /// @param extent Width/Length (metric) of the object's estimated footprint.
    public bool CanFitObject(Vector3 center, float extent)
    {
      var surface = _surfaces.FirstOrDefault(s => s.ContainsElement(PositionToGridNode(center)));
      if (surface == null)
      {
        return false;
      }

      var r = (Vector3.right + Vector3.forward) * (extent * 0.5f);
      var min = PositionToGridNode(center - r);
      var max = PositionToGridNode(center + r);
      var position = min.Coordinates;
      for (position.x = min.Coordinates.x; position.x <= max.Coordinates.x; position.x += 1)
      {
        for (position.y = min.Coordinates.y; position.y <= max.Coordinates.y; position.y += 1)
        {
          if (!surface.ContainsElement(new GridNode(position)))
          {
            return false;
          }
        }
      }

      return true;
    }

    /// Traces a path from a pre-computed PathNode collection.
    /// @param nodesA collection containing parental relationships.
    /// @param traceStart The source node of the trace.
    private List<Waypoint> GeneratePath(HashSet<PathNode> nodes, PathNode traceStart)
    {
      var path = new List<Waypoint>();

      // Trace path
      var node = traceStart;
      while (node.HasParent)
      {
        var parent = nodes.FirstOrDefault
          (entry => entry.Coordinates.Equals(node.ParentCoordinates));

        // Extract node position
        if (node.Surface != null)
        {
          var type = parent.Surface == node.Surface
            ? Waypoint.MovementType.Walk
            : Waypoint.MovementType.SurfaceEntry;

          var pos = TileToPosition(node.Coordinates);
          path.Add(new Waypoint(node.Surface, new Vector3(pos.x, node.Elevation, pos.y), type));
        }

        // Go to parent
        node = parent;
      }

      // The resulting array should start with the source node
      path.Reverse();

      return path;
    }

    /// Invalidates the specified nodes of existing planes.
    private void InvalidateNodes(HashSet<GridNode> nodes)
    {
      // Remove nodes from registry
      _spatialTree.Remove(nodes);

      // Remove nodes from its respective surfaces
      _surfaces.ForEach(entry => entry.Except(nodes));

      // Clean up empty planes
      _surfaces.RemoveAll(entry => entry.IsEmpty);
    }

    /// Merges new walkable nodes with existing planes. If the nodes cannot be merged, a new plane is created.
    private void MergeNodes(HashSet<GridNode> nodes)
    {
      // Register new walkable nodes
      _spatialTree.Insert(nodes);

      // Create a new planes from the provided (walkable) nodes
      var candidate = new Surface(nodes);

      // Just add the candidate plane to the list if this is the first one we found
      if (_surfaces.Count == 0)
      {
        _surfaces.Add(candidate);
        return;
      }

      // Gather overlapping planes
      var overlappingPlanes = _surfaces.Where(entry => entry.Overlaps(candidate)).ToList();

      // No overlap, add candidate as a new plane
      if (!overlappingPlanes.Any())
      {
        _surfaces.Add(candidate);
        return;
      }

      // Find an overlapping plane that satisfies the merging conditions
      var anchorPlane = overlappingPlanes.FirstOrDefault
      (
        entry =>
          entry.CanMerge(candidate, _settings.StepHeight * 2.0f)
      );

      // No such plane
      if (anchorPlane == null)
      {
        // Exclude its nodes from existing planes
        overlappingPlanes.ForEach(p => p.Except(candidate));

        // Remove planes that were a subset of the candidate
        _surfaces.RemoveAll(p => p.IsEmpty);

        // Add candidate as a new plane
        _surfaces.Add(candidate);
        return;
      }

      // Base plane found to merge the new nodes to
      anchorPlane.Merge(candidate);

      // Iterate through other overlapping planes except this base plane
      overlappingPlanes.Remove(anchorPlane);
      foreach (var entry in overlappingPlanes)
      {
        // Either merge or exclude nodes
        if (anchorPlane.CanMerge(entry, _settings.StepHeight * 2.0f))
        {
          anchorPlane.Merge(entry);
          _surfaces.Remove(entry);
        }
        else
        {
          entry.Except(candidate);
        }
      }
    }

    /// Converts a world position to grid coordinates.
    private Vector2Int PositionToTile(Vector2 position)
    {
      return new Vector2Int
      (
        Mathf.FloorToInt(position.x / _settings.TileSize),
        Mathf.FloorToInt(position.y / _settings.TileSize)
      );
    }

    /// Converts a world position to grid coordinates.
    private Vector2Int PositionToTile(Vector3 position)
    {
      return new Vector2Int
      (
        Mathf.FloorToInt(position.x / _settings.TileSize),
        Mathf.FloorToInt(position.z / _settings.TileSize)
      );
    }

    /// Converts a grid coordinate to world position.
    /// @param tile Tile coordinates to convert.
    private Vector2 TileToPosition(Vector2Int tile)
    {
      var halfSize = _settings.TileSize / 2.0f;
      return new Vector2
        (tile.x * _settings.TileSize + halfSize, tile.y * _settings.TileSize + halfSize);
    }

    /// Converts a grid coordinate to world position.
    /// @param tile Tile coordinates to converts.
    /// @param elevation Height of the resulting position.
    private Vector3 TileToPosition(Vector2Int tile, float elevation)
    {
      var halfSize = _settings.TileSize / 2.0f;
      return new Vector3
      (
        tile.x * _settings.TileSize + halfSize,
        elevation,
        tile.y * _settings.TileSize + halfSize
      );
    }

    /// Converts a world position to a node on the game board.
    private GridNode PositionToGridNode(Vector3 worldPosition)
    {
      return new GridNode(PositionToTile(worldPosition));
    }

    /// Converts a node on the game board to its corresponding world position.
    /// @param node A grid node acquired from an existing plane of the game board.
    /// @returns World position of the centroid of the node.
    public Vector3 GridNodeToPosition(GridNode node)
    {
      return TileToPosition(node.Coordinates, node.Elevation);
    }

    /// Returns the 8 neighbouring tiles of the specified coordinate.
    /// @param vertex Tile coordinate.
    /// @returns Array containing the coordinates of the neighbouring tiles.
    private static IEnumerable<Vector2Int> GetNeighbours(Vector2Int vertex)
    {
      return new[]
      {
        new Vector2Int(vertex.x + 1, vertex.y),
        new Vector2Int(vertex.x - 1, vertex.y),
        new Vector2Int(vertex.x, vertex.y + 1),
        new Vector2Int(vertex.x, vertex.y - 1),
        new Vector2Int(vertex.x - 1, vertex.y + 1),
        new Vector2Int(vertex.x + 1, vertex.y + 1),
        new Vector2Int(vertex.x - 1, vertex.y - 1),
        new Vector2Int(vertex.x + 1, vertex.y - 1)
      };
    }

    /// Finds the closest node to the specified reference in candidates.
    private static bool GetNearestNode(IList<GridNode> candidates, Vector2Int reference, out GridNode nearestNode)
    {
      // Helpers
      var minDistance = float.MaxValue;
      var success = false;

      // Initialize result
      nearestNode = default;

      // Find nearest
      for (int i = 0; i < candidates.Count; i++)
      {
        var point = candidates[i];
        var distance = Vector2Int.Distance(point.Coordinates, reference);

        if (distance < minDistance)
        {
          // Found a candidate
          success = true;
          minDistance = distance;
          nearestNode = point;
        }
      }

      return success;
    }
  }
}