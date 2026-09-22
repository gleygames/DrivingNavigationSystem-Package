using UnityEngine;

namespace Gley.NavigationSystem
{
    internal enum MarkerEntryKind
    {
        Object,
        Point
    }

    internal class MarkerEntry
    {
        public MarkerEntryKind Kind { get; set; }
        public MapMarker Marker { get; set; }
        public Transform Transform { get; set; }
        public GameObject Prefab { get; set; }
        public MarkerRotationMode RotationMode { get; set; }
        public Vector3 TruePosition { get; set; }
        public Vector3 TrueHeading { get; set; }
        public long GridCell { get; set; }
        public int ChannelMask { get; set; }
        public bool IsStatic { get; set; }
        public bool Initialized { get; set; }
        public bool CanBeDestination { get; set; }
        public bool ShowArrow { get; set; }
        public bool IsPlayer { get; set; }
        public bool Alive { get; set; }
    }
}
