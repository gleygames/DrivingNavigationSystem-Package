using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public class BuildRoad
    {
        public List<Vector3> Points { get; }
        public float SpeedOverride { get; set; }
        public float WidthOverride { get; set; }
        public int Id { get; set; }
        public int TypeId { get; set; }
        public int StartIntersectionId { get; set; }
        public int EndIntersectionId { get; set; }
        public bool OneWay { get; set; }

        public BuildRoad()
        {
            Points = new List<Vector3>();
        }
    }

    public class BuildIntersection
    {
        public Vector3 Position { get; set; }
        public int Id { get; set; }
    }

    public class RoadNetworkBuildInput
    {
        public List<BuildRoad> Roads { get; }
        public List<BuildIntersection> Intersections { get; }

        public RoadNetworkBuildInput()
        {
            Roads = new List<BuildRoad>();
            Intersections = new List<BuildIntersection>();
        }
    }
}
