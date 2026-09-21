using UnityEngine;

namespace Gley.NavigationSystem
{
    public struct RoadPoint
    {
        public Vector3 Position { get; }
        public Vector3 Tangent { get; }
        public float DistanceAlong { get; }
        public float Distance { get; }
        public int RoadIndex { get; }
        public int SegmentIndex { get; }

        public RoadPoint(int roadIndex, int segmentIndex, float distanceAlong, Vector3 position, float distance, Vector3 tangent)
        {
            RoadIndex = roadIndex;
            SegmentIndex = segmentIndex;
            DistanceAlong = distanceAlong;
            Position = position;
            Distance = distance;
            Tangent = tangent;
        }
    }
}
