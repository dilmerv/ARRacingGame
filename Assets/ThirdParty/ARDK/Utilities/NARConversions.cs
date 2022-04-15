// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  /// A utility class to help convert between NAR and Unity coordinate frames
  public static class NARConversions
  {
    private static readonly Vector3 _signVector3 = new Vector3(1, -1, 1);
    
    public static Vector3 FromNARToUnity(Vector3 point)
    {
      return Vector3.Scale(point, _signVector3);
    }

    private static readonly Matrix4x4 _signMatrix4x4 =
      new Matrix4x4
      (
        new Vector4(1, 0, 0, 0),
        new Vector4(0, -1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );
    
    public static Matrix4x4 FromNARToUnity(Matrix4x4 matrix)
    {
      // Sy [R|T] Sy
      //    [0|1]
      return _signMatrix4x4 * matrix * _signMatrix4x4;
    }

    public static Vector3 FromUnityToNAR(Vector3 point)
    {
      return FromNARToUnity(point);
    }

    public static Matrix4x4 FromUnityToNAR(Matrix4x4 matrix)
    {
      return FromNARToUnity(matrix);
    }
  }
}
