// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Utilities.Marker
{
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public class ImageDataUtils
  {
    public enum TransformType
    {
      FlipHorizontal,
      FlipVertical,
      RotateClockwise,
      RotateCounterclockwise
    }

    public static T[] Transform<T>(T[] original, TransformType type, int width, int height)
    {
      if (original.Length != width * height)
      {
        Debug.LogError("Dimensions of transformed array must match length of the original array.");
        return null;
      }

      var newCol = 0;
      var newRow = 0;
      var newWidth = width;

      var transformed = new T[original.Length];
      for (var col = 0; col < width; col++)
      {
        for (var row = 0; row < height; row++)
        {
          switch (type)
          {
            case TransformType.FlipVertical:
              newCol = width - 1 - col;
              newRow = row;
              break;

            case TransformType.FlipHorizontal:
              newCol = col;
              newRow = height - 1 - row;
              break;

            case TransformType.RotateClockwise:
              newCol = height - 1 - row;
              newRow = col;
              newWidth = height;
              break;

            case TransformType.RotateCounterclockwise:
              newCol = row;
              newRow = width - 1 - col;
              newWidth = height;
              break;
          }

          transformed[newCol + (newRow * newWidth)] = original[col + (row * width)];
        }
      }

      return transformed;
    }
  }
}
