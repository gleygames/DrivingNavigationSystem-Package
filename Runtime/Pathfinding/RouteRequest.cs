using UnityEngine;

namespace Gley.NavigationSystem
{
    public class RouteRequest
    {
        public Vector3 From { get; private set; }
        public Vector3 Heading { get; private set; }
        public Vector3 To { get; private set; }
        public RoutePreferences Preferences { get; }
        public float StartSnapDistance { get; set; }
        public float DestinationSnapDistance { get; set; }
        public float ArrivalDistance { get; set; }
        public bool HasHeading { get; private set; }

        public RouteRequest()
        {
            Preferences = new RoutePreferences();
            StartSnapDistance = 200f;
            DestinationSnapDistance = 50f;
            ArrivalDistance = 10f;
        }

        public void Set(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
        }

        public void SetHeading(Vector3 heading)
        {
            Heading = heading.normalized;
            HasHeading = true;
        }

        public void ClearHeading()
        {
            HasHeading = false;
        }
    }
}
