// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.SLAM;

using UnityEngine;

namespace Niantic.ARDK.AR
{
  /// <summary>
  /// @brief A video image, with position-tracking information, captured as part of an AR session.
  /// @remarks A running AR session continuously captures video frames from the device camera.
  /// For each frame, the image is analyzed together with data from the device's motion
  /// sensing hardware to estimate the device's real-world position.
  /// </summary>
  public interface IARFrame:
    IDisposable
  {
    /// Gets or sets a value telling the retain policy of this frame.
    /// If unset (that is, null) uses the value set at the session that created this frame.
    ARFrameDisposalPolicy? DisposalPolicy { get; set; }

    /// <summary>
    /// One or more native GPU textures.
    /// @remark In iOS, this will be two textures, together they represent an image of format
    /// [YCbCr](https://wiki.multimedia.cx/index.php/YCbCr_4:2:0).
    /// @remark In Android, this will be a single texture of format BGRA.
    /// @note Not supported in Remote Debugging.
    /// </summary>
    IntPtr[] CapturedImageTextures { get; }

    /// <summary>
    /// The CPU-side image.
    /// @remark In Android, initiates a copy from the native GPU texture into CPU-accessible memory.
    /// @note **May be null**.
    /// </summary>
    IImageBuffer CapturedImageBuffer { get; }

    /// <summary>
    /// The depth buffer.
    /// @note **May be null**.
    /// </summary>
    IDepthBuffer Depth { get; }

    /// <summary>
    /// The semantic buffer.
    /// @note **May be null**.
    /// </summary>
    ISemanticBuffer Semantics { get; }

    /// <summary>
    /// Information about the camera position, orientation, and imaging parameters used to
    /// capture the frame.
    /// </summary>
    IARCamera Camera { get; }

    /// <summary>
    /// An estimate of lighting conditions based on the camera image.
    /// @note **May be null**, e.g. when not tracking.
    /// </summary>
    IARLightEstimate LightEstimate { get; }

    /// <summary>
    /// The list of anchors representing positions tracked or objects at a point in time.
    /// </summary>
    ReadOnlyCollection<IARAnchor> Anchors { get; }

    /// <summary>
    /// The list of maps from the Computer Vision system
    /// </summary>
    ReadOnlyCollection<IARMap> Maps { get; }

    /// <summary>
    /// Raw 3D positions that are used in scene understanding.
    /// @note **May be null**.
    /// @note Not currently supported in Remote Debugging.
    /// </summary>
    IARPointCloud RawFeaturePoints { get; }

    /// <summary>
    /// 3D positions generated based on the Depth Buffer.
    /// @note **May be null**
    /// </summary>
    IDepthPointCloud DepthPointCloud { get; }

    /// <summary>
    /// The scaling factor applied to this frame's data.
    /// </summary>
    float WorldScale { get; }

    /// Using a screen location, find points on real-world surfaces and objects in the camera view.
    /// @param viewportWidth Width of the screen in pixels.
    /// @param viewportHeight Height of the screen in pixels.
    /// @param screenPoint A 2D point in screen (pixel) space.
    /// @param types
    ///   The types of results to search for. Certain values are not supported on some platforms.
    ///   See ARHitTestResultType documentation for details.
    /// @returns An array of hit test results in order of closest to furthest. May be zero-length.
    ReadOnlyCollection<IARHitTestResult> HitTest
    (
      int viewportWidth,
      int viewportHeight,
      Vector2 screenPoint,
      ARHitTestResultType types
    );

    /// <summary>
    /// Returns an affine transform for converting between normalized image coordinates
    /// and a coordinate space appropriate for rendering the camera image onscreen.
    /// @param orientation The current interface orientation.
    /// @param viewportWidth Viewport width, in pixels.
    /// @param viewportHeight Viewport height, in pixels.
    /// @note Returns a pre-calculated value in Remote Debugging.
    /// </summary>
    Matrix4x4 CalculateDisplayTransform
    (
      ScreenOrientation orientation,
      int viewportWidth,
      int viewportHeight
    );

    // TODO: Review if those should really be here:
    /// <summary>
    /// Releases the captured image and textures.
    /// @remark Due to Unity's Garbage Collector (GC), we can't be sure when our memory will be
    /// deallocated -- even if there are no more references to it. This method serves as a way to
    /// release the largest memory suck in this class -- the images and textures.
    /// @remark This might clear some dictionaries that cache accessed values, which is not thread
    ///   safe, so this should only be called from the Unity main thread
    /// </summary>
    void ReleaseImageAndTextures();

    /// <summary>
    /// Get a serialized representation of this ARFrame, with variable compression levels.
    /// Defaults to a compression factor of 70%.
    /// </summary>
    /// <param name="compressionLevel">The quality of the compressed buffers (1 = worst, 100 = best).</param>
    /// <param name="includeImageBuffers">If false, image buffers will not be serialized.</param>
    /// <param name="includeAwarenessBuffers">If false, awareness buffers will not be serialized.</param>
    [Obsolete("This method is deprecated. Please use the ARFrameFactory to serialise the frame.")]
    IARFrame Serialize(bool includeImageBuffers = true, bool includeAwarenessBuffers = true, int compressionLevel = 70);
  }
}
