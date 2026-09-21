using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public enum SnapTargetKind
    {
        None,
        Intersection,
        Road
    }

    public readonly struct SnapTarget
    {
        public SnapTarget(SnapTargetKind kind, int intersectionId, int roadId, int segment, float t, Vector3 position)
        {
            Kind = kind;
            IntersectionId = intersectionId;
            RoadId = roadId;
            Segment = segment;
            T = t;
            Position = position;
        }

        public SnapTargetKind Kind { get; }
        public Vector3 Position { get; }
        public float T { get; }
        public int IntersectionId { get; }
        public int RoadId { get; }
        public int Segment { get; }
    }
}
