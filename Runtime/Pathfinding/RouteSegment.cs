namespace Gley.NavigationSystem
{
    public struct RouteSegment
    {
        public float FromDistance { get; }
        public float ToDistance { get; }
        public int RoadIndex { get; }
        public int RoadId { get; }
        public bool Forward { get; }

        public RouteSegment(int roadIndex, int roadId, bool forward, float fromDistance, float toDistance)
        {
            RoadIndex = roadIndex;
            RoadId = roadId;
            Forward = forward;
            FromDistance = fromDistance;
            ToDistance = toDistance;
        }
    }
}
