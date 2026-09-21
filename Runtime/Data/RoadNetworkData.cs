using UnityEngine;

namespace Gley.NavigationSystem
{
    public class RoadNetworkData : ScriptableObject
    {
        public const int CurrentFormatVersion = 1;

        [SerializeField] private RoadRecord[] roads = new RoadRecord[0];
        [SerializeField] private IntersectionRecord[] intersections = new IntersectionRecord[0];
        [SerializeField] private Vector3[] points = new Vector3[0];
        [SerializeField] private float[] pointDistances = new float[0];
        [SerializeField] private int[] links = new int[0];
        [SerializeField] private NavigationSettings settings;
        [SerializeField] private float maxSpeed;
        [SerializeField] private int formatVersion = CurrentFormatVersion;
        [SerializeField] private int sourceVersion;
        [SerializeField] private int settingsVersion;

        public NavigationSettings Settings { get { return settings; } }
        public float MaxSpeed { get { return maxSpeed; } }
        public int FormatVersion { get { return formatVersion; } }
        public int SourceVersion { get { return sourceVersion; } }
        public int SettingsVersion { get { return settingsVersion; } }
        public int RoadCount { get { return roads.Length; } }
        public int IntersectionCount { get { return intersections.Length; } }

        public RoadRecord GetRoad(int index)
        {
            return roads[index];
        }

        public IntersectionRecord GetIntersection(int index)
        {
            return intersections[index];
        }

        public int GetLink(int index)
        {
            return links[index];
        }

        public Vector3 GetPoint(int index)
        {
            return points[index];
        }

        public float GetPointDistance(int index)
        {
            return pointDistances[index];
        }

        public int GetOtherEnd(int roadIndex, int intersectionIndex)
        {
            RoadRecord road = roads[roadIndex];
            if (road.StartIntersection == intersectionIndex)
            {
                return road.EndIntersection;
            }
            return road.StartIntersection;
        }

        public bool IsDeadEnd(int intersectionIndex)
        {
            return intersections[intersectionIndex].LinkCount == 1;
        }

        internal void SetData(RoadRecord[] roads, IntersectionRecord[] intersections, int[] links, Vector3[] points, float[] pointDistances, NavigationSettings settings, int settingsVersion, float maxSpeed)
        {
            this.roads = roads;
            this.intersections = intersections;
            this.links = links;
            this.points = points;
            this.pointDistances = pointDistances;
            this.settings = settings;
            this.settingsVersion = settingsVersion;
            this.maxSpeed = maxSpeed;
        }
    }
}
