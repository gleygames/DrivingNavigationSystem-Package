using UnityEngine;

namespace Gley.NavigationSystem
{
    internal readonly struct MapViewPose
    {
        public MapViewPose(Vector2 position, float rotationDegrees, float scale)
        {
            Position = position;
            RotationDegrees = rotationDegrees;
            Scale = scale;
        }

        public Vector2 Position { get; }
        public float RotationDegrees { get; }
        public float Scale { get; }
    }

    internal class MapViewMath
    {
        public float ComputeScale(float viewportWidth, float zoomMeters)
        {
            return viewportWidth / zoomMeters;
        }

        public MapViewPose ComputeContainerPose(Vector2 centerMap, float rotationDegrees, float unitsPerMeter, Vector2 pivotInViewport)
        {
            Vector2 rotatedCenter = Rotate(centerMap * unitsPerMeter, rotationDegrees);
            Vector2 position = pivotInViewport - rotatedCenter;
            return new MapViewPose(position, rotationDegrees, unitsPerMeter);
        }

        public Vector2 MapToViewport(Vector2 mapPoint, MapViewPose pose)
        {
            return pose.Position + Rotate(mapPoint * pose.Scale, pose.RotationDegrees);
        }

        public Vector2 ViewportToMap(Vector2 viewportPoint, MapViewPose pose)
        {
            Vector2 relative = viewportPoint - pose.Position;
            Vector2 unrotated = Rotate(relative, -pose.RotationDegrees);
            return unrotated / pose.Scale;
        }

        private Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }
    }
}
