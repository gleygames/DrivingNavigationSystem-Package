using System.Collections.Generic;
using Gley.Common;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class RoadNetworkBuilder
    {
        public void Build(RoadNetworkBuildInput input, NavigationSettings settings, RoadNetworkData target, float cellSize = RoadGrid.DefaultCellSize)
        {
            int intersectionCount = input.Intersections.Count;
            Dictionary<int, int> intersectionIndexById = new Dictionary<int, int>(intersectionCount);
            IntersectionRecord[] intersections = new IntersectionRecord[intersectionCount];
            List<int>[] linksPerIntersection = new List<int>[intersectionCount];
            for (int i = 0; i < intersectionCount; i++)
            {
                BuildIntersection buildIntersection = input.Intersections[i];
                intersectionIndexById[buildIntersection.Id] = i;
                intersections[i] = new IntersectionRecord(buildIntersection.Id, buildIntersection.Position, 0, 0);
                linksPerIntersection[i] = new List<int>();
            }

            List<RoadRecord> roads = new List<RoadRecord>(input.Roads.Count);
            List<Vector3> points = new List<Vector3>();
            List<float> pointDistances = new List<float>();
            float maxSpeed = 0f;

            for (int i = 0; i < input.Roads.Count; i++)
            {
                BuildRoad buildRoad = input.Roads[i];

                int startIndex;
                int endIndex;
                if (!intersectionIndexById.TryGetValue(buildRoad.StartIntersectionId, out startIndex) || !intersectionIndexById.TryGetValue(buildRoad.EndIntersectionId, out endIndex))
                {
                    CustomLogger.LogError("RoadNetworkBuilder: road " + buildRoad.Id + " references a missing intersection and was skipped.");
                    continue;
                }

                RoadType roadType = settings.FindRoadType(buildRoad.TypeId);
                int typeId = buildRoad.TypeId;
                if (roadType == null)
                {
                    CustomLogger.LogWarning("RoadNetworkBuilder: road " + buildRoad.Id + " has an unknown road type " + buildRoad.TypeId + ". Falling back to the first road type.");
                    roadType = settings.RoadTypes[0];
                    typeId = roadType.Id;
                }

                float speed;
                if (buildRoad.SpeedOverride > 0f)
                {
                    speed = buildRoad.SpeedOverride;
                }
                else
                {
                    speed = roadType.SpeedMetersPerSecond;
                }

                float width;
                if (buildRoad.WidthOverride > 0f)
                {
                    width = buildRoad.WidthOverride;
                }
                else
                {
                    width = roadType.WidthMeters;
                }

                int firstPoint = points.Count;
                int pointCount = buildRoad.Points.Count;
                float length = 0f;
                for (int p = 0; p < pointCount; p++)
                {
                    Vector3 point = buildRoad.Points[p];
                    points.Add(point);
                    if (p == 0)
                    {
                        pointDistances.Add(0f);
                    }
                    else
                    {
                        Vector3 previous = buildRoad.Points[p - 1];
                        float deltaX = point.x - previous.x;
                        float deltaZ = point.z - previous.z;
                        length += Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
                        pointDistances.Add(length);
                    }
                }

                if (speed > maxSpeed)
                {
                    maxSpeed = speed;
                }

                int roadIndex = roads.Count;
                roads.Add(new RoadRecord(buildRoad.Id, typeId, buildRoad.OneWay, startIndex, endIndex, firstPoint, pointCount, length, speed, width));

                linksPerIntersection[startIndex].Add(roadIndex);
                linksPerIntersection[endIndex].Add(roadIndex);
            }

            List<int> links = new List<int>();
            for (int i = 0; i < intersectionCount; i++)
            {
                int firstLink = links.Count;
                List<int> intersectionLinks = linksPerIntersection[i];
                for (int l = 0; l < intersectionLinks.Count; l++)
                {
                    links.Add(intersectionLinks[l]);
                }
                intersections[i] = new IntersectionRecord(intersections[i].Id, intersections[i].Position, firstLink, intersectionLinks.Count);
            }

            RoadRecord[] roadArray = roads.ToArray();
            Vector3[] pointArray = points.ToArray();

            RoadGrid grid = new RoadGrid();
            grid.Build(roadArray, pointArray, cellSize);

            target.SetData(roadArray, intersections, links.ToArray(), pointArray, pointDistances.ToArray(), grid, settings, settings.Version, maxSpeed);
        }
    }
}
