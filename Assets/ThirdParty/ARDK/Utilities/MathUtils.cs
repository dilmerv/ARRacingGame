using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Camera;

using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  /// A collection of math helpers to calculate fundamental
  /// transformations required for augmented reality.
  internal static class MathUtils
  {
    /// Returns an affine transform for converting between
    /// normalized image coordinates and a coordinate space
    /// appropriate for rendering the camera image onscreen.
    /// @note The width and height arguments must conform
    ///   with the specified viewport orientation.
    /// @param camera The IARCamera that captured the AR background image.
    /// @param viewportOrientation The orientation of the viewport.
    /// @param viewportWidth The width of the viewport in pixels.
    /// @param viewportHeight The height of the viewport in pixels.
    /// @returns An affine 4x4 transformation matrix.
    internal static Matrix4x4 CalculateDisplayTransform
    (
      IARCamera camera,
      ScreenOrientation viewportOrientation,
      int viewportWidth,
      int viewportHeight
    )
    {
      return CalculateDisplayTransform
      (
        camera.ImageResolution.width,
        camera.ImageResolution.height,
        viewportWidth,
        viewportHeight,
        viewportOrientation
      );
    }
    
    /// Returns an affine transform for converting between
    /// normalized image coordinates and a coordinate space
    /// appropriate for rendering the camera image onscreen.
    /// @note The width and height arguments must conform
    ///   with the specified viewport orientation.
    /// @param imageWidth The width of the raw AR background image in pixels.
    /// @param imageHeight The height of the raw AR background image in pixels.
    /// @param viewportOrientation The orientation of the viewport.
    /// @param viewportWidth The width of the viewport in pixels.
    /// @param viewportHeight The height of the viewport in pixels.
    /// @returns An affine 4x4 transformation matrix.
    internal static Matrix4x4 CalculateDisplayTransform
    (
      int imageWidth,
      int imageHeight,
      int viewportWidth,
      int viewportHeight,
      ScreenOrientation viewportOrientation,
      bool invertVertically = true
    )
    {
      // Infer image orientation
      var imageOrientation = imageWidth > imageHeight
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // We invert the y coordinate because Unity's 2D coordinate system is
      // upside-down compared to the native systems.
      var invert = invertVertically ? AffineInvertVertical() : Matrix4x4.identity;
      
      return invert *
        AffineFit
        (
          imageWidth,
          imageHeight,
          imageOrientation,
          viewportWidth,
          viewportHeight,
          viewportOrientation
        );
    }
    
    /// Returns a view matrix for the specified screen orientation using the native convention.
    /// @param camera The camera to convert world space coordinates to.
    /// @param orientation The orientation of the viewport.
    internal static Matrix4x4 CalculateNarViewMatrix
    (
      IARCamera camera,
      ScreenOrientation orientation
    )
    {
#if UNITY_EDITOR
      var currentOrientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
#else
      var currentOrientation = Screen.orientation;
#endif

      // Get the view matrix for the current orientation in nar convention
      var viewMatrixForCurrentOrientation = camera.GetViewMatrix(currentOrientation);
      
      // Calculate the required rotation for the target orientation
      var rotation = CalculateViewRotation
      (
        from: currentOrientation,
        to: orientation
      );
      
      return rotation * viewMatrixForCurrentOrientation;
    }
    
    /// Returns a view matrix for the specified screen orientation using the native convention.
    /// @param camera The camera to convert world space coordinates to.
    /// @param orientation The orientation of the viewport.
    internal static Matrix4x4 CalculateNarViewMatrix
    (
      Camera camera,
      ScreenOrientation orientation
    )
    { 
#if UNITY_EDITOR
      var currentOrientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
#else
      var currentOrientation = Screen.orientation;
#endif

      // Get the view matrix for the current orientation in Unity convention
      var viewMatrixForCurrentOrientation = camera.worldToCameraMatrix;

      // Flip the forward axis to conform with nar
      var narView = viewMatrixForCurrentOrientation.ConvertViewMatrixBetweenNarAndUnity();

      // Calculate the required rotation for the target orientation
      var rotation = CalculateViewRotation
      (
        from: currentOrientation,
        to: orientation
      );

      return rotation * narView;
    }

    /// Returns a view matrix for the specified screen orientation using Unity's convention.
    /// @param camera The camera to convert world space coordinates to.
    /// @param orientation The orientation of the viewport.
    internal static Matrix4x4 CalculateUnityViewMatrix
    (
      IARCamera camera,
      ScreenOrientation orientation
    )
    {
#if UNITY_EDITOR
      var currentOrientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
#else
      var currentOrientation = Screen.orientation;
#endif

      // Get the view matrix for the current orientation in nar convention
      var viewMatrixForCurrentOrientation = camera.GetViewMatrix(currentOrientation);
      
      // Calculate the required rotation for the target orientation
      var rotation = CalculateViewRotation
      (
        from: currentOrientation,
        to: orientation
      );

      // Rotate the view
      var rotatedView = rotation * viewMatrixForCurrentOrientation;
      
      // Flip the forward axis to conform with unity
      return rotatedView.ConvertViewMatrixBetweenNarAndUnity();
    }

    /// The forward vector is flipped between Unity camera matrices and IARCamera
    /// view matrices. When called on a native view matrix, this extension method
    /// converts to unity convention and vice-versa.
    internal static Matrix4x4 ConvertViewMatrixBetweenNarAndUnity(this Matrix4x4 view)
    {
      var result = view;
      
      result.m20 *= -1.0f;
      result.m21 *= -1.0f;
      result.m22 *= -1.0f;
      result.m23 *= -1.0f;
      
      return result;
    }

    /// Calculates the intrinsic parameters of a Unity camera.
    internal static CameraIntrinsics CalculateIntrinsics(Camera camera)
    {
      var f = (camera.focalLength * camera.pixelWidth) / camera.sensorSize.x;
      var p = new Vector2(camera.pixelWidth / 2f, camera.pixelHeight / 2f);
      return new CameraIntrinsics(f, f, p.x, p.y);
    }
    
    /// Calculates camera intrinsics from the specified properties.
    /// @param imageWidth Width of the captured image.
    /// @param imageHeight Height of the captured image.
    /// @param focalLength The focal length of the camera.
    /// @param sensorWidth Width of the camera sensor.
    /// @returns Intrinsic parameters of the described camera.
    internal static CameraIntrinsics CalculateIntrinsics
    (
      int imageWidth,
      int imageHeight,
      float focalLength,
      float sensorWidth
    )
    {
      var f = (focalLength * imageWidth) / sensorWidth;
      var p = new Vector2(imageWidth / 2f, imageHeight / 2f);
      return new CameraIntrinsics(f, f, p.x, p.y);
    }

    /// Returns a transform matrix appropriate for rendering
    /// 3D content to match the image captured by the camera,
    /// using the specified parameters.
    /// @param camera The AR camera that captured the AR background image.
    /// @param viewportOrientation The orientation of the viewport.
    /// @param viewportWidth The width of the viewport in pixels.
    /// @param viewportHeight The height of the viewport in pixels.
    /// @param near The distance from the camera to the near clipping plane.
    /// @param far The distance from the camera to the far clipping plane.
    internal static Matrix4x4 CalculateProjectionMatrix
    (
      IARCamera camera,
      ScreenOrientation viewportOrientation,
      int viewportWidth,
      int viewportHeight,
      float near,
      float far
    )
    {
      var imageResolution = camera.ImageResolution;

      return CalculateProjectionMatrix
      (
        camera.Intrinsics,
        imageResolution.width,
        imageResolution.height,
        viewportWidth,
        viewportHeight,
        viewportOrientation,
        near,
        far
      );
    }

    /// Returns a transform matrix appropriate for rendering
    /// 3D content to match the image captured by the camera,
    /// using the specified parameters.
    /// @param intrinsics The intrinsics for the raw AR background image.
    /// @param imageWidth The width of the raw AR background image.
    /// @param imageHeight The height of the raw AR background image.
    /// @param viewportOrientation The orientation of the viewport.
    /// @param viewportWidth The width of the viewport in pixels.
    /// @param viewportHeight The height of the viewport in pixels.
    /// @param near The distance from the camera to the near clipping plane.
    /// @param far The distance from the camera to the far clipping plane.
    internal static Matrix4x4 CalculateProjectionMatrix
    (
      CameraIntrinsics intrinsics,
      int imageWidth,
      int imageHeight,
      int viewportWidth,
      int viewportHeight,
      ScreenOrientation viewportOrientation,
      float near,
      float far
    )
    {
      // Get the corners of the captured image
      var right = imageWidth - 1;
      var top = imageHeight - 1;
      var left = right - 2.0f * intrinsics.PrincipalPoint.x;
      var bottom = top - 2.0f * intrinsics.PrincipalPoint.y;

      // Get a resolution in the original image's orientation
      // that matches the viewport's aspect ratio
      var croppedFrame = CalculateDisplayFrame
      (
        imageWidth,
        imageHeight,
        viewportWidth,
        viewportHeight
      );

      // Calculate the image origin in landscape
      Vector2 origin = new Vector2
      (
        x: left / croppedFrame.width,
        y: -bottom / croppedFrame.height
      );

      // Rotate the image origin to the specified orientation
      origin = RotateVector(origin, (float)GetAngle(viewportOrientation, ScreenOrientation.LandscapeLeft));

      Vector2 f = new Vector2
      (
        x: 1.0f / (croppedFrame.width * 0.5f / intrinsics.FocalLength.x),
        y: 1.0f / (croppedFrame.height * 0.5f / intrinsics.FocalLength.y)
      );

      // Swap for portrait
      if (viewportOrientation == ScreenOrientation.Portrait ||
        viewportOrientation == ScreenOrientation.PortraitUpsideDown)
      {
        (f.x, f.y) = (f.y, f.x);
      }

      // Calculate the depth of the frustum
      var depth = near - far;

      Matrix4x4 projection = Matrix4x4.zero;
      projection[0, 0] = f.x;
      projection[1, 1] = f.y;
      projection[0, 2] = origin.x;
      projection[1, 2] = origin.y;
      projection[2, 2] = far / depth;
      projection[2, 3] = far * near / depth;
      projection[3, 2] = -1.0f;

      return projection;
    }

    /// Calculates a projective transformation to sync normalized image coordinates
    /// with the target pose.
    /// @param referencePose The original pose associated with the image.
    /// @param targetPose The the pose to synchronize with.
    /// @param backProjectionPlane The normalized distance of the back-projection plane
    ///        between the near and far clipping planes. Lower values make translations
    ///        more perceptible.
    /// @returns A projective transformation matrix to be applied to normalized image coordinates.
    internal static Matrix4x4 CalculateHomography
    (
      Matrix4x4 referencePose,
      Matrix4x4 targetPose,
      Matrix4x4 projection,
      float backProjectionPlane
    )
    {
      Vector3 worldPosition00 = ViewportToWorldPoint
        (Vector2.zero, referencePose, projection, backProjectionPlane);

      Vector3 worldPosition01 = ViewportToWorldPoint
        (Vector2.up, referencePose, projection, backProjectionPlane);

      Vector3 worldPosition11 = ViewportToWorldPoint
        (Vector2.one, referencePose, projection, backProjectionPlane);

      Vector3 worldPosition10 = ViewportToWorldPoint
        (Vector2.right, referencePose, projection, backProjectionPlane);

      Vector2 p00 = WorldToViewportPoint(worldPosition00, targetPose, projection);
      Vector2 p01 = WorldToViewportPoint(worldPosition01, targetPose, projection);
      Vector2 p11 = WorldToViewportPoint(worldPosition11, targetPose, projection);
      Vector2 p10 = WorldToViewportPoint(worldPosition10, targetPose, projection);

      float a = p10.x - p11.x;
      float b = p01.x - p11.x;
      float c = p00.x - p01.x - p10.x + p11.x;
      float d = p10.y - p11.y;
      float e = p01.y - p11.y;
      float f = p00.y - p01.y - p10.y + p11.y;

      float g = (c * d - a * f) / (b * d - a * e);
      float h = (c * e - b * f) / (a * e - b * d);

      return new Matrix4x4
      (
        new Vector4(p10.x - p00.x + h * p10.x, p10.y - p00.y + h * p10.y, h),
        new Vector4(p01.x - p00.x + g * p01.x, p01.y - p00.y + g * p01.y, g),
        new Vector4(p00.x, p00.y, 1.0f),
        new Vector4(0, 0, 0, 1)
      ).inverse;
    }
    
    // Returns an affine transformation such that if multiplied
    // with normalized coordinates of the target coordinate frame,
    // the results are normalized coordinates in the source
    // coordinate frame.
    // @notes
    //  E.g. if source is defined by an awareness buffer and
    //  target is defined by the viewport, normalized viewport
    //  coordinates multiplied with this transform will result
    //  in normalized coordinates in the awareness buffer.
    //  Further more, if source is defined by the AR image and
    //  the target is the viewport, this matrix will be equivalent
    //  to the display transform provided by the ARKit framework.
    private static Matrix4x4 AffineFit
    (
      float sourceWidth,
      float sourceHeight,
      ScreenOrientation sourceOrientation,
      float targetWidth,
      float targetHeight,
      ScreenOrientation targetOrientation
    )
    {
      var rotatedContainer = RotateResolution
        (sourceWidth, sourceHeight, sourceOrientation, targetOrientation);

      // Calculate scaling
      var targetRatio = targetWidth / targetHeight;
      var s = targetRatio < 1.0f
        ? new Vector2(targetWidth / (targetHeight / rotatedContainer.y * rotatedContainer.x), 1.0f)
        : new Vector2(1.0f, targetHeight / (targetWidth / rotatedContainer.x * rotatedContainer.y));

      var rotate = GetAffineRotation(from: sourceOrientation, to: targetOrientation);
      var scale = AffineScale(s);
      var translate = AffineTranslation(new Vector2((1.0f - s.x) * 0.5f, (1.0f - s.y) * 0.5f));

      return rotate * translate * scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineRotation(double rad)
    {
      return new Matrix4x4
      (
        new Vector4((float)Math.Cos(rad), (float)Math.Sin(rad), 0, 0),
        new Vector4((float)-Math.Sin(rad), (float)Math.Cos(rad), 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineTranslation(Vector2 translation)
    {
      return new Matrix4x4
      (
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(translation.x, translation.y, 0, 1)
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineInvertHorizontal()
    {
      return _affineInvertHorizontalMatrix;
    }
    private static readonly Matrix4x4 _affineInvertHorizontalMatrix = new Matrix4x4
    (
      new Vector4(-1, 0, 0, 0),
      new Vector4(0, 1, 0, 0),
      new Vector4(0, 0, 1, 0),
      new Vector4(1, 0, 0, 1)
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineInvertVertical()
    {
      return _affineInvertVerticalMatrix;
    }
    private static readonly Matrix4x4 _affineInvertVerticalMatrix = new Matrix4x4
    (
      new Vector4(1, 0, 0, 0),
      new Vector4(0, -1, 0, 0),
      new Vector4(0, 0, 1, 0),
      new Vector4(0, 1, 0, 1)
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 AffineScale(Vector2 scale)
    {
      return new Matrix4x4
      (
        new Vector4(scale.x, 0, 0, 0),
        new Vector4(0, scale.y, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );
    }
    
    internal static Resolution CalculateDisplayFrame
    (
      float sourceWidth,
      float sourceHeight,
      float viewportWidth,
      float viewportHeight
    )
    {
      // Infer target orientation
      var targetOrientation = viewportWidth > viewportHeight
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Infer source orientation
      var sourceOrientation = sourceWidth > sourceHeight
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      Vector2 target = RotateResolution
      (
        viewportWidth,
        viewportHeight,
        targetOrientation,
        sourceOrientation
      );

      // Calculate scaling
      var s = sourceOrientation == ScreenOrientation.Portrait
        ? new Vector2(target.x / (target.y / sourceHeight * sourceWidth), 1.0f)
        : new Vector2(1.0f, target.y / (target.x / sourceWidth * sourceHeight));

      return new Resolution
      {
        width = Mathf.FloorToInt(sourceWidth * s.x), height = Mathf.FloorToInt(sourceHeight * s.y)
      };
    }
    
    /// Calculates a rotation matrix that transforms a view
    /// matrix from one screen orientation to another.
    internal static Matrix4x4 CalculateViewRotation(ScreenOrientation from, ScreenOrientation to)
    {
      // Get the rotation between the screen orientations
      var angle = (float)GetAngle(from, to) * Mathf.Rad2Deg;
      
      return Matrix4x4.Rotate
      (
        // The view rotation the opposite of the UI rotation
        Quaternion.AngleAxis(-angle, Vector3.forward)
      );
    }
    
    /// Calculates an affine rotation matrix that transforms
    /// an image from one screen orientation to another.
    internal static Matrix4x4 CalculateScreenRotation(ScreenOrientation from, ScreenOrientation to)
    {
      return AffineRotation(GetAngle(from, to));
    }

    /// Transforms a viewport coordinate to world space.
    /// @param viewPosition Coordinate to transform.
    /// @param view View matrix.
    /// @param projection Projection matrix.
    /// @param distanceNormalized Defines how far the transformed point should be from the view.
    /// @returns The point in world space.
    private static Vector3 ViewportToWorldPoint
    (
      Vector2 viewPosition,
      Matrix4x4 view,
      Matrix4x4 projection,
      float distanceNormalized
    )
    {
      var clipCoordinates = new Vector4
      (
        viewPosition.x * 2.0f - 1.0f,
        viewPosition.y * 2.0f - 1.0f,
        distanceNormalized * 2.0f - 1.0f,
        1.0f
      );

      var viewProjectionInverted = (projection * view).inverse;
      var result = viewProjectionInverted * clipCoordinates;

      if (Mathf.Approximately(result.w, 0.0f))
      {
        return Vector3.zero;
      }

      result.w = 1.0f / result.w;
      result.x *= result.w;
      result.y *= result.w;
      result.z *= result.w;

      return result;
    }

    /// Projects the specified world position to view space.
    /// @param worldPosition Position to transform.
    /// @param view View matrix.
    /// @param projection Projection matrix.
    /// @returns The world position in view space.
    private static Vector2 WorldToViewportPoint
    (
      Vector3 worldPosition,
      Matrix4x4 view,
      Matrix4x4 projection
    )
    {
      var position = new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 1.0f);
      var transformed = view * position;
      var projected = projection * transformed;

      if (Mathf.Approximately(projected.w, 0.0f))
      {
        return Vector2.zero;
      }

      projected.w = 1.0f / projected.w;
      projected.x *= projected.w;
      projected.y *= projected.w;
      projected.z *= projected.w;

      return new Vector2(projected.x * 0.5f + 0.5f, projected.y * 0.5f + 0.5f);
    }

    /// Calculates the angle to rotate from one screen orientation to another in radians.
    /// @param from Original orientation.
    /// @param to Target orientation.
    /// @returns Angle to rotate to get from one orientation to the other. 
    private static double GetAngle(ScreenOrientation from, ScreenOrientation to)
    {
      const double rotationUnit = Math.PI / 2.0;
      return (ScreenOrientationLookup[to] - ScreenOrientationLookup[from]) * rotationUnit;
    }

    private static readonly IDictionary<ScreenOrientation, int> ScreenOrientationLookup =
      new Dictionary<ScreenOrientation, int>
      {
        {
          ScreenOrientation.LandscapeLeft, 0
        },
        {
          ScreenOrientation.Portrait, 1
        },
        {
          ScreenOrientation.LandscapeRight, 2
        },
        {
          ScreenOrientation.PortraitUpsideDown, 3
        }
      };

    /// Calculates an affine transformation to rotate from one screen orientation to another
    /// around the pivot.
    /// @param from Original orientation.
    /// @param to Target orientation.
    /// @returns An affine matrix to be applied to normalized image coordinates.
    private static Matrix4x4 GetAffineRotation(ScreenOrientation from, ScreenOrientation to)
    {
      // Rotate around the center
      var pivot = new Vector2(0.5f, 0.5f);
      return AffineTranslation(pivot) *
        AffineRotation(GetAngle(from, to)) *
        AffineTranslation(-pivot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 RotateResolution
    (
      float sourceWidth,
      float sourceHeight,
      ScreenOrientation sourceOrientation,
      ScreenOrientation targetOrientation
    )
    {
      if (sourceOrientation == ScreenOrientation.LandscapeLeft)
      {
        return
          targetOrientation == ScreenOrientation.LandscapeLeft ||
          targetOrientation == ScreenOrientation.LandscapeRight
            ? new Vector2(sourceWidth, sourceHeight)
            : new Vector2(sourceHeight, sourceWidth);
      }

      return
        targetOrientation == ScreenOrientation.Portrait ||
        targetOrientation == ScreenOrientation.PortraitUpsideDown
          ? new Vector2(sourceWidth, sourceHeight)
          : new Vector2(sourceHeight, sourceWidth);
    }

    private static Vector2 RotateVector(Vector2 vector, float radians)
    {
      float sin = Mathf.Sin(radians);
      float cos = Mathf.Cos(radians);

      float x = vector.x;
      float y = vector.y;
      vector.x = cos * x - sin * y;
      vector.y = sin * x + cos * y;
      
      return vector;
    }
  }
}