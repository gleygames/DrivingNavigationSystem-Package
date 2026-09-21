using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadImportContext
    {
        private readonly RoadNetworkAuthoring asset;
        private readonly RoadEditOperations operations;
        private readonly string sourceTag;
        private readonly float mergeDistance;

        public RoadImportContext(RoadNetworkAuthoring asset, RoadEditOperations operations, string sourceTag, float mergeDistance)
        {
            this.asset = asset;
            this.operations = operations;
            this.sourceTag = sourceTag;
            this.mergeDistance = mergeDistance;
        }

        public int AddRoad(List<Vector3> keyPoints, RoadBrush brush)
        {
            if (keyPoints.Count < 2)
            {
                return 0;
            }

            int startIntersectionId = FindOrCreateIntersection(keyPoints[0], mergeDistance);
            int endIntersectionId = FindOrCreateIntersection(keyPoints[keyPoints.Count - 1], mergeDistance);

            int roadId = operations.CreateRoad(keyPoints, brush, startIntersectionId, endIntersectionId);
            if (roadId == 0)
            {
                return 0;
            }

            AuthoringRoad road = asset.FindRoad(roadId);
            road.SetSourceTag(sourceTag);

            return roadId;
        }

        public int FindOrCreateIntersection(Vector3 position, float mergeDistance)
        {
            List<AuthoringIntersection> intersections = asset.Intersections;
            for (int i = 0; i < intersections.Count; i++)
            {
                if (Vector3.Distance(intersections[i].Position, position) <= mergeDistance)
                {
                    return intersections[i].Id;
                }
            }

            int newId = asset.NewIntersectionId();
            asset.Intersections.Add(new AuthoringIntersection(newId, position));
            return newId;
        }
    }
}
