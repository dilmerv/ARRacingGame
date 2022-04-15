// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.AR.Localization
{
  /// @cond ARDK_VPS_BETA
  /// Represents an identified coordinate space anchored to the real world.
  /// The Identifier will be persisted across sessions, so assets can be persistently pinned to
  ///   the world through an Identifier and transform relative to the coordinate space.
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public sealed class ARWorldCoordinateSpace:
    IDisposable
  {
    /// Persistent identifier for a particular coordinate space.
    /// Serializable, should be stored game-side in order to localize against this coordinate space
    /// in future sessions.
    public Identifier Id { get; }

    /// The coordinate space's transform relative to the local (i.e. device tracking system’s)
    /// coordinate space.
    /// Changes to this value will be broadcast by the WorldCoordinateSpaceUpdated event.
    /// @note This is a transform to an arbitrary point in the real world, but is guaranteed to
    ///   be persistent (subsequent localization results to the same coordinate space will return
    ///   a transform to the same real world point again).
    public Matrix4x4 Transform { get; }

    /// Creates an ARWorldCoordinateSpace object from the provided parameters.
    ///
    /// @param id
    ///   the unique string identifying this world anchor.
    /// @param
    ///   transform the world anchor's transform relative to the world.
    public ARWorldCoordinateSpace(Identifier id, Matrix4x4 transform)
    {
      Id = id;
      Transform = transform;
    }

    /// Converts a pose from this coordinate space to world coordinates.
    ///
    /// @param pose
    ///   the pose to be transformed.
    public Matrix4x4 ConvertToWorld(Matrix4x4 pose)
    {
      return ConvertToTransform(Matrix4x4.identity, pose);
    }

    /// Converts a pose from this coordinate space to a target coordinate system.
    ///
    /// @param targetTransform
    ///   the target coordinate system's transform, relatively to the world.
    /// @param pose
    ///   the pose to be transformed.
    public Matrix4x4 ConvertToTransform(Matrix4x4 targetTransform, Matrix4x4 pose)
    {
      return targetTransform.inverse * Transform * pose;
    }

    /// Converts a pose from world coordinates to this coordinate space.
    ///
    /// @param pose
    ///   the pose to be transformed.
    public Matrix4x4 ConvertFromWorld(Matrix4x4 pose)
    {
      return ConvertFromTransform(Matrix4x4.identity, pose);
    }

    /// Converts a pose from another coordinate space to this coordinate space.
    ///
    /// @param sourceCoordinateSpace
    ///   the pose's source coordinate space.
    /// @param pose
    ///   the pose to be transformed.
    public Matrix4x4 ConvertFromCoordinateSpace
      (ARWorldCoordinateSpace sourceCoordinateSpace, Matrix4x4 pose)
    {
      return ConvertFromTransform(sourceCoordinateSpace.Transform, pose);
    }

    /// Converts a pose from a source coordinate system to this coordinate space.
    ///
    /// @param sourceTransform
    ///   the transform of the coordinate system of the pose, relatively to the world.
    /// @param pose
    ///   the pose to be transformed.
    public Matrix4x4 ConvertFromTransform(Matrix4x4 sourceTransform, Matrix4x4 pose)
    {
      return Transform.inverse * sourceTransform * pose;
    }

    /// Converts a vector from this coordinate space to world coordinates.
    ///
    /// @param vector
    ///   the vector to be transformed.
    public Vector3 ConvertToWorld(Vector3 vector)
    {
      return ConvertToTransform(Matrix4x4.identity, vector);
    }

    /// Converts a vector from this coordinate space to a target coordinate system.
    ///
    /// @param targetTransform
    ///   the target coordinate system's transform, relatively to the world.
    /// @param vector
    ///   the vector to be transformed.
    public Vector3 ConvertToTransform(Matrix4x4 targetTransform, Vector3 vector)
    {
      return targetTransform.inverse * Transform * vector;
    }

    /// Converts a vector from world coordinates to this coordinate space.
    ///
    /// @param vector
    ///   the vector to be transformed.
    public Vector3 ConvertFromWorld(Vector3 vector)
    {
      return ConvertFromTransform(Matrix4x4.identity, vector);
    }

    /// Converts a vector from another coordinate space to this coordinate space.
    ///
    /// @param sourceCoordinateSpace
    ///   the vector's source coordinate space.
    /// @param vector
    ///   the vector to be transformed.
    public Vector3 ConvertFromCoordinateSpace
      (ARWorldCoordinateSpace sourceCoordinateSpace, Vector3 vector)
    {
      return ConvertFromTransform(sourceCoordinateSpace.Transform, vector);
    }

    /// Converts a vector from a source coordinate system to this coordinate space.
    ///
    /// @param sourceTransform
    ///   the transform of the coordinate system of the vector, relatively to the world.
    /// @param vector
    ///   the vector to be transformed.
    public Vector3 ConvertFromTransform(Matrix4x4 sourceTransform, Vector3 vector)
    {
      return Transform.inverse * sourceTransform * vector;
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }

    /// Coordinate space identifier that should be stored in game-side database for players to
    /// use to localize against in the future.
    [Serializable]
    public readonly struct Identifier
      : IEquatable<Identifier>
    {
      private readonly string _id;

      public Identifier(string id)
      {
        if (id == null)
          throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(id))
          throw new ArgumentException("Argument cannot be empty", nameof(id));

        _id = id;
      }

      public override string ToString()
      {
        return _id.ToString();
      }

      public override bool Equals(object other)
      {
        return other is Identifier id && this.Equals(id);
      }

      public override int GetHashCode()
      {
        return (_id != null ? _id.GetHashCode() : 0);
      }

      public bool Equals(Identifier other)
      {
        return _id.Equals(other._id);
      }
    }
  }
}
