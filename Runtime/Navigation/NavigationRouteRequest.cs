using UnityEngine;

namespace Gley.NavigationSystem
{
    public class NavigationRouteRequest
    {
        public RoutePreferences Preferences { get; set; }
        public Vector3 From { get; set; }
        public Vector3 To { get; set; }
        public Vector3 Heading { get; set; }
        public bool HasHeading { get; set; }

        public NavigationRouteRequest()
        {
        }

        public NavigationRouteRequest(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
        }
    }
}
