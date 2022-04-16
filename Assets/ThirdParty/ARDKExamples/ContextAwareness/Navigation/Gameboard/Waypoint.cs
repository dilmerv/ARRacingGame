// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDKExamples.Gameboard
{
    public readonly struct Waypoint
    {
        /// The type of movement of a waypoint
        public enum MovementType
        {
            /// Walk node.
            Walk = 0,

            /// The first node of a new surface on the path.
            SurfaceEntry = 1
        }

        /// Reference to the surface this point is part of.
        public readonly Surface Surface;

        /// The position of this point in world coordinates.
        public readonly Vector3 WorldPosition;

        /// The type of movement of this waypoint.
        public readonly MovementType Type;

        public Waypoint(Surface surface, Vector3 worldPosition, MovementType type )
        {
            Surface = surface;
            WorldPosition = worldPosition;
            Type = type;
        }
    }
}
