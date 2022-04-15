using UnityEngine;

namespace Niantic.ARDK.AR.Camera
{
  /// Where (fx, fy) is the focal length and (px, py) is the principal point location,
  /// can be cast into a vector: [fx, fy, px, py]
  public struct CameraIntrinsics
  {
    public Vector2 FocalLength { get; }
    public Vector2 PrincipalPoint { get; }

    private readonly Vector4 _vector;

    internal CameraIntrinsics(float fx, float fy, float px, float py)
    {
      FocalLength = new Vector2(fx, fy);
      PrincipalPoint = new Vector2(px, py);

      _vector = new Vector4(FocalLength.x, FocalLength.y, PrincipalPoint.x, PrincipalPoint.y);
    }

    public static implicit operator Vector4(CameraIntrinsics o)
    {
      return o._vector;
    }

    public static implicit operator CameraIntrinsics(Vector4 v)
    {
      return new CameraIntrinsics(v.x, v.y, v.z, v.w);
    }
  }
}
