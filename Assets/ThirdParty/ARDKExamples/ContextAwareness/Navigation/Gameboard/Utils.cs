// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDKExamples.Il2Cpp;

using UnityEngine;

namespace Niantic.ARDKExamples.Gameboard
{
  public static class Utils
  {
    /// Calculates the euclidean distance between two nodes.
    /// @notes Used during path finding.
    public static float EuclideanDistance(PathNode from, PathNode to)
    {
      return Vector2Int.Distance(from.Coordinates, to.Coordinates);
    }

    /// Calculates the manhattan distance between two nodes.
    /// @notes Used during path finding.
    public static int ManhattanDistance(PathNode from, PathNode to)
    {
      return Math.Abs
          (from.Coordinates.x - to.Coordinates.x) +
        Math.Abs(from.Coordinates.y - to.Coordinates.y);
    }

    /// Calculates the standard deviation of the provided sample.
    public static float CalculateStandardDeviation(IEnumerable<float> samples)
    {
      var m = 0.0f;
      var s = 0.0f;
      var k = 1;
      foreach (var value in samples)
      {
        var tmpM = m;
        m += (value - tmpM) / k;
        s += (value - tmpM) * (value - m);
        k++;
      }

      return Mathf.Sqrt(s / Mathf.Max(1, k - 1));
    }

    /// Fits a plane to best align with the specified set of points.
    [Obsolete]
    public static void FitPlane
    (
      Vector3[] points,
      out Vector3 position,
      out Vector3 normal,
      int iterations = 100
    )
    {
      // Find the primary principal axis
      var primaryDirection = Vector3.forward;
      FitLine(points, out position, ref primaryDirection, iterations / 2);

      // Flatten the points along that axis
      var flattenedPoints = new Vector3[points.Length];
      Array.Copy(points, flattenedPoints, points.Length);
      var flattenedPointsLength = flattenedPoints.Length;
      for (var i = 0; i < flattenedPointsLength; i++)
      {
        flattenedPoints[i] = Vector3.ProjectOnPlane
            (points[i] - position, primaryDirection) +
          position;
      }

      // Find the secondary principal axis
      var secondaryDirection = Vector3.right;
      FitLine(flattenedPoints, out position, ref secondaryDirection, iterations / 2);

      normal = Vector3.Cross(primaryDirection, secondaryDirection).normalized;
    }

    /// Fits a plane to best align with the specified set of points.
    /// Source: https://www.ilikebigbits.com/2017_09_25_plane_from_points_2.html
    public static void FastFitPlane
    (
      Vector3[] points,
      out Vector3 position,
      out Vector3 normal
    )
    {
      position = default;
      normal = default;

      var n = points.Length;
      if (n < 3)
      {
        return;
      }

      var sum = Vector3.zero;
      for (var i = 0; i < points.Length; i++)
        sum += points[i];

      position = sum * (1.0f / n);
      
      var xx = 0.0f;
      var xy = 0.0f;
      var xz = 0.0f;
      var yy = 0.0f;
      var yz = 0.0f;
      var zz = 0.0f;

      for (var i = 0; i < points.Length; i++)
      {
        var r = points[i] - position;
        xx += r.x * r.x;
        xy += r.x * r.y;
        xz += r.x * r.z;
        yy += r.y * r.y;
        yz += r.y * r.z;
        zz += r.z * r.z;
      }

      xx /= n;
      xy /= n;
      xz /= n;
      yy /= n;
      yz /= n;
      zz /= n;

      var weightedDir = Vector3.zero;

      {
        var detX = yy * zz - yz * yz;
        var axisDir = new Vector3
        (
          x: detX,
          y: xz * yz - xy * zz,
          z: xy * yz - xz * yy
        );

        var weight = detX * detX;
        weightedDir += axisDir * weight;
      }

      {
        var detY = xx * zz - xz * xz;
        var axisDir = new Vector3
        (
          x: xz * yz - xy * zz,
          y: detY,
          z: xy * xz - yz * xx
        );

        var weight = detY * detY;
        weightedDir += axisDir * weight;
      }

      {
        var detZ = xx * yy - xy * xy;
        var axisDir = new Vector3
        (
          x: xy * yz - xz * yy,
          y: xy * xz - yz * xx,
          z: detZ
        );

        var weight = detZ * detZ;
        weightedDir += axisDir * weight;
      }
      
      float num = Vector3.Magnitude(weightedDir);
      normal = weightedDir / num;
    }

    // Compile this method without throwing any array exceptions.
    // This ultimately improves performance, since this method
    // is heavily used during GameBoard scans.
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    private static void FitLine
    (
      IList<Vector3> points,
      out Vector3 origin,
      ref Vector3 direction,
      int iterations = 100
    )
    {
      if (direction == Vector3.zero || float.IsNaN(direction.x) || float.IsInfinity(direction.x))
      {
        direction = Vector3.up;
      }

      // Calculate Average
      origin = Vector3.zero;
      var len = points.Count;
      for (var i = 0; i < len; i++)
      {
        origin += points[i];
      }

      origin /= len;

      // Step the optimal fitting line approximation:
      var newDirection = new Vector3(0.0f,0.0f, 0.0f);
      var zero = new Vector3(0.0f, 0.0f, 0.0f);
      var point = zero;
      for (var iter = 0; iter < iterations; iter++)
      {
        newDirection.x = 0.0f;
        newDirection.y = 0.0f;
        newDirection.z = 0.0f;

        for (var i = 0; i < len; i++)
        {
          var p = points[i];

          point.x = p.x - origin.x;
          point.y = p.y - origin.y;
          point.z = p.z - origin.z;

          var dot = (float)(direction.x * (double)point.x +
            direction.y * (double)point.y +
            direction.z * (double)point.z);

          newDirection = new Vector3
          (
            newDirection.x + point.x * dot,
            newDirection.y + point.y * dot,
            newDirection.z + point.z * dot
          );
        }

        var mag = Mathf.Sqrt
        (
          (float)(newDirection.x * (double)newDirection.x +
            newDirection.y * (double)newDirection.y +
            newDirection.z * (double)newDirection.z)
        );

        const double eps = 9.999999747378752E-06;
        direction = (double)mag > eps ? newDirection / mag : zero;
      }
    }

    /// Insert a value into an IList{T} that is presumed to be already sorted such that sort
    /// ordering is preserved.
    /// @notes https://www.jacksondunstan.com/articles/3189
    public static void InsertIntoSortedList<T>(this IList<T> list, T value, Comparison<T> comparison)
    {
      var startIndex = 0;
      var endIndex = list.Count;
      while (endIndex > startIndex)
      {
        var windowSize = endIndex - startIndex;
        var middleIndex = startIndex + (windowSize / 2);
        var middleValue = list[middleIndex];
        var compareToResult = comparison(middleValue, value);
        if (compareToResult == 0)
        {
          list.Insert(middleIndex, value);
          return;
        }

        if (compareToResult < 0)
        {
          startIndex = middleIndex + 1;
        }
        else
        {
          endIndex = middleIndex;
        }
      }
      list.Insert(startIndex, value);
    }
    
    /// Finds the nearest point on the game board to the specified reference, using linear search.
    /// @note This is way slower than FindNearest(), but it finds the exact
    /// nearest point and not an approximation.
    /// @param reference The position top fond the closest point to.
    /// @returns The nearest point on the game board to the reference.
    public static Vector3 FindNearestLinear(this IGameBoard gameBoard, Vector3 reference)
    {
      var minDistance = float.MaxValue;
      var result = Vector3.zero;
      
      foreach (var surface in gameBoard.Surfaces)
      {
        foreach (var entry in surface.Elements)
        {
          var current = gameBoard.GridNodeToPosition(entry);
          var distance = Vector3.Distance(current, reference);
          if (distance < minDistance)
          {
            minDistance = distance;
            result = current;
          }
        }
      }

      return result;
    }

    /// Calculates a walkable path between the two specified positions.
    /// @param fromPosition Start position.
    /// @param toPosition Destination position
    /// @param agent Configuration for the agent the path is calculated for.
    /// @param couldReachDestination True, if the path reaches the specified destination.
    /// @returns A list of waypoints in world coordinates.
    public static List<Waypoint> CalculatePath(this IGameBoard gameBoard, Vector3 from, Vector3 to, AgentConfiguration agent, out bool couldReachDestination)
    {
      couldReachDestination = false;
      
      var result = gameBoard.CalculatePath(from, to, agent);
      if (result.Count > 0)
      {
        const float error = 0.01f;
        var tail = gameBoard.SnapToGrid(result.Last().WorldPosition);
        couldReachDestination = Vector3.Distance(tail, gameBoard.SnapToGrid(to)) <= error;
      }
      
      return result;
    }

    /// Visualizes the game board in scene view.
    /// @param visualizeSpatialTree If true, the underlying spatial structure will be displayed.
    public static void DrawGizmos(this IGameBoard gameBoard, bool visualizeSpatialTree = false)
    {
      if (visualizeSpatialTree)
        gameBoard.SpatialTree.DrawGizmos(gameBoard.Configuration);

      // Draw discovered game board nodes
      var surfaces = gameBoard.Surfaces;
      foreach (Surface surface in surfaces)
      {
        foreach (var node in surface.Elements)
        {
          Gizmos.DrawCube(gameBoard.GridNodeToPosition(node), Vector3.one * 0.15f);
        }
      }
    }
  }
}