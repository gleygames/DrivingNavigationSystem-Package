namespace Gley.NavigationSystem.Editor
{
    public enum SelectionHitKind
    {
        None,
        Road,
        KeyPoint,
        Intersection
    }

    public readonly struct SelectionHit
    {
        public SelectionHit(SelectionHitKind kind, int roadId, int keyPointIndex, int intersectionId)
        {
            Kind = kind;
            RoadId = roadId;
            KeyPointIndex = keyPointIndex;
            IntersectionId = intersectionId;
        }

        public SelectionHitKind Kind { get; }
        public int RoadId { get; }
        public int KeyPointIndex { get; }
        public int IntersectionId { get; }
    }
}
