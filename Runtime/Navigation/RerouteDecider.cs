namespace Gley.NavigationSystem
{
    internal class RerouteDecider
    {
        private const float DefaultRerouteCooldown = 20f;
        private const float DefaultTurnedAroundDistance = 30f;

        public float DistanceSinceLastReroute { get; private set; }
        public float RerouteCooldown { get; set; }
        public float TurnedAroundDistance { get; set; }

        public RerouteDecider()
        {
            RerouteCooldown = DefaultRerouteCooldown;
            TurnedAroundDistance = DefaultTurnedAroundDistance;
        }

        public RerouteReason Decide(NavigationSession session, RoadMatcher matcher, bool teleported)
        {
            if (teleported)
            {
                if (matcher.IsOnRoad && session.IsRoadOnRoute(matcher.RoadIndex, matcher.MovingForward))
                {
                    return RerouteReason.None;
                }
                return RerouteReason.Teleported;
            }

            if (DistanceSinceLastReroute < RerouteCooldown)
            {
                return RerouteReason.None;
            }

            if (matcher.EnteredRoad)
            {
                if (session.WrongTurn)
                {
                    return RerouteReason.BackOnRoad;
                }
                return RerouteReason.None;
            }

            if (session.WrongTurn)
            {
                return RerouteReason.WrongTurn;
            }

            if (session.WrongWayDistance >= TurnedAroundDistance)
            {
                return RerouteReason.TurnedAround;
            }

            return RerouteReason.None;
        }

        public void AccumulateDrivenDistance(float drivenDistance)
        {
            DistanceSinceLastReroute += drivenDistance;
        }

        public void OnRerouted()
        {
            DistanceSinceLastReroute = 0f;
        }
    }
}
