using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadDrawPlanner
    {
        public const float DefaultDetailDistance = 150f;

        private readonly Dictionary<int, Bounds> roadBounds = new Dictionary<int, Bounds>();
        private RoadNetworkAuthoring cachedAuthoring;
        private int cachedVersion = -1;

        public void Plan(RoadNetworkAuthoring authoring, float unitsPerMeter, Plane[] frustum, Vector3 cameraPosition, float detailDistance, HashSet<int> typeFilter, List<int> visibleRoadIds, List<bool> detailed)
        {
            EnsureCache(authoring);

            visibleRoadIds.Clear();
            detailed.Clear();

            List<AuthoringRoad> roads = authoring.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                if (!PassesTypeFilter(road, typeFilter))
                {
                    continue;
                }

                Bounds trueBounds;
                if (!roadBounds.TryGetValue(road.Id, out trueBounds))
                {
                    continue;
                }

                Bounds worldBounds = new Bounds(trueBounds.center * unitsPerMeter, trueBounds.size * unitsPerMeter);
                if (!GeometryUtility.TestPlanesAABB(frustum, worldBounds))
                {
                    continue;
                }

                visibleRoadIds.Add(road.Id);
                float distance = Vector3.Distance(cameraPosition, worldBounds.ClosestPoint(cameraPosition));
                detailed.Add(distance < detailDistance);
            }
        }

        private void EnsureCache(RoadNetworkAuthoring authoring)
        {
            if (cachedAuthoring == authoring && cachedVersion == authoring.Version)
            {
                return;
            }

            roadBounds.Clear();
            List<AuthoringRoad> roads = authoring.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                roadBounds[road.Id] = ComputeBounds(road);
            }

            cachedAuthoring = authoring;
            cachedVersion = authoring.Version;
        }

        private Bounds ComputeBounds(AuthoringRoad road)
        {
            List<Vector3> points = road.Points;
            if (points.Count == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < points.Count; i++)
            {
                bounds.Encapsulate(points[i]);
            }
            return bounds;
        }

        private bool PassesTypeFilter(AuthoringRoad road, HashSet<int> typeFilter)
        {
            if (typeFilter == null || typeFilter.Count == 0)
            {
                return true;
            }
            return typeFilter.Contains(road.TypeId);
        }
    }
}
