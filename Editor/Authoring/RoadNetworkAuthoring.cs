using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadNetworkAuthoring : ScriptableObject, IFormatVersioned
    {
        public const int CurrentFormatVersion = 1;

        [SerializeField] private List<AuthoringRoad> roads = new List<AuthoringRoad>();
        [SerializeField] private List<AuthoringIntersection> intersections = new List<AuthoringIntersection>();
        [SerializeField] private RoadNetworkData runtimeAsset;
        [SerializeField] private NavigationSettings settings;
        [SerializeField] private MapData mapAsset;
        [SerializeField] private float gridCellSize = RoadGrid.DefaultCellSize;
        [SerializeField] private int formatVersion = CurrentFormatVersion;
        [SerializeField] private int nextRoadId = 1;
        [SerializeField] private int nextIntersectionId = 1;
        [SerializeField] private int version;

        internal List<AuthoringRoad> Roads { get { return roads; } }
        internal List<AuthoringIntersection> Intersections { get { return intersections; } }
        public RoadNetworkData RuntimeAsset { get { return runtimeAsset; } }
        public NavigationSettings Settings { get { return settings; } }
        public MapData MapAsset { get { return mapAsset; } }
        public float GridCellSize { get { return gridCellSize; } }
        public int FormatVersion { get { return formatVersion; } }
        int IFormatVersioned.CurrentFormatVersion { get { return CurrentFormatVersion; } }
        public int NextRoadId { get { return nextRoadId; } }
        public int NextIntersectionId { get { return nextIntersectionId; } }
        public int Version { get { return version; } }

        public AuthoringRoad FindRoad(int id)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                if (roads[i].Id == id)
                {
                    return roads[i];
                }
            }
            return null;
        }

        public AuthoringIntersection FindIntersection(int id)
        {
            for (int i = 0; i < intersections.Count; i++)
            {
                if (intersections[i].Id == id)
                {
                    return intersections[i];
                }
            }
            return null;
        }

        public void GetRoadsAtIntersection(int id, List<AuthoringRoad> output)
        {
            output.Clear();
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                if (road.StartIntersectionId == id || road.EndIntersectionId == id)
                {
                    output.Add(road);
                }
            }
        }

        public int NewRoadId()
        {
            int id = nextRoadId;
            nextRoadId++;
            return id;
        }

        public int NewIntersectionId()
        {
            int id = nextIntersectionId;
            nextIntersectionId++;
            return id;
        }

        public void MarkChanged()
        {
            version++;
        }

        public void SetGridCellSize(float value)
        {
            gridCellSize = value;
            MarkChanged();
        }

        internal void SetRuntimeAsset(RoadNetworkData value)
        {
            runtimeAsset = value;
        }
    }
}
