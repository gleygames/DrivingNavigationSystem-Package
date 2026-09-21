namespace Gley.NavigationSystem
{
    internal class RouteSnapper
    {
        private readonly RoadQuery query;

        public RouteSnapper(RoadNetworkData data)
        {
            query = new RoadQuery(data);
        }

        public FailureReason SnapStart(RouteRequest request, out RoadPoint point)
        {
            bool found = query.FindNearest(request.From, request.StartSnapDistance, out point);
            if (!found)
            {
                return FailureReason.NoRoadNearStart;
            }
            return FailureReason.None;
        }

        public FailureReason SnapDestination(RouteRequest request, out RoadPoint point)
        {
            bool found = query.FindNearest(request.To, request.DestinationSnapDistance, out point);
            if (!found)
            {
                return FailureReason.NoRoadNearDestination;
            }
            return FailureReason.None;
        }
    }
}
