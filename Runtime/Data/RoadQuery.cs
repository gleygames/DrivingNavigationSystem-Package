using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class RoadQuery
    {
        private readonly RoadNetworkData data;

        public RoadQuery(RoadNetworkData data)
        {
            this.data = data;
        }

        public bool FindNearest(Vector3 truePos, float maxDistance, out RoadPoint result)
        {
            RoadGrid grid = data.Grid;

            int cellXMin;
            int cellXMax;
            int cellZMin;
            int cellZMax;
            grid.GetCellRange(truePos.x - maxDistance, truePos.z - maxDistance, truePos.x + maxDistance, truePos.z + maxDistance, out cellXMin, out cellXMax, out cellZMin, out cellZMax);

            bool found = false;
            RoadPoint best = default(RoadPoint);

            for (int cz = cellZMin; cz <= cellZMax; cz++)
            {
                for (int cx = cellXMin; cx <= cellXMax; cx++)
                {
                    int cellIndex = grid.GetCellIndex(cx, cz);
                    int start = grid.GetCellStart(cellIndex);
                    int count = grid.GetCellCount(cellIndex);
                    for (int e = start; e < start + count; e++)
                    {
                        int roadIndex = grid.GetEntryRoad(e);
                        int pointIndex = grid.GetEntryPoint(e);

                        RoadPoint candidate;
                        ProjectOntoSegment(truePos, roadIndex, pointIndex, out candidate);

                        if (candidate.Distance > maxDistance)
                        {
                            continue;
                        }

                        if (!found)
                        {
                            best = candidate;
                            found = true;
                        }
                        else if (candidate.Distance < best.Distance)
                        {
                            best = candidate;
                        }
                        else if (candidate.Distance == best.Distance && candidate.RoadIndex < best.RoadIndex)
                        {
                            best = candidate;
                        }
                    }
                }
            }

            result = best;
            return found;
        }

        public void FindAllWithin(Vector3 truePos, float radius, List<RoadPoint> output)
        {
            output.Clear();

            RoadGrid grid = data.Grid;

            int cellXMin;
            int cellXMax;
            int cellZMin;
            int cellZMax;
            grid.GetCellRange(truePos.x - radius, truePos.z - radius, truePos.x + radius, truePos.z + radius, out cellXMin, out cellXMax, out cellZMin, out cellZMax);

            for (int cz = cellZMin; cz <= cellZMax; cz++)
            {
                for (int cx = cellXMin; cx <= cellXMax; cx++)
                {
                    int cellIndex = grid.GetCellIndex(cx, cz);
                    int start = grid.GetCellStart(cellIndex);
                    int count = grid.GetCellCount(cellIndex);
                    for (int e = start; e < start + count; e++)
                    {
                        int roadIndex = grid.GetEntryRoad(e);
                        int pointIndex = grid.GetEntryPoint(e);

                        RoadPoint candidate;
                        ProjectOntoSegment(truePos, roadIndex, pointIndex, out candidate);

                        if (candidate.Distance > radius)
                        {
                            continue;
                        }

                        int existing = FindRoadInOutput(output, roadIndex);
                        if (existing < 0)
                        {
                            output.Add(candidate);
                        }
                        else if (candidate.Distance < output[existing].Distance)
                        {
                            output[existing] = candidate;
                        }
                    }
                }
            }
        }

        public void ProjectOnRoad(int roadIndex, Vector3 truePos, out RoadPoint result)
        {
            RoadRecord road = data.GetRoad(roadIndex);

            bool found = false;
            RoadPoint best = default(RoadPoint);

            for (int p = 0; p < road.PointCount - 1; p++)
            {
                int pointIndex = road.FirstPoint + p;

                RoadPoint candidate;
                ProjectOntoSegment(truePos, roadIndex, pointIndex, out candidate);

                if (!found)
                {
                    best = candidate;
                    found = true;
                }
                else if (candidate.Distance < best.Distance)
                {
                    best = candidate;
                }
            }

            result = best;
        }

        private void ProjectOntoSegment(Vector3 truePos, int roadIndex, int segmentPointIndex, out RoadPoint result)
        {
            Vector3 start = data.GetPoint(segmentPointIndex);
            Vector3 end = data.GetPoint(segmentPointIndex + 1);

            float deltaX = end.x - start.x;
            float deltaZ = end.z - start.z;
            float lengthSquared = deltaX * deltaX + deltaZ * deltaZ;

            float t;
            if (lengthSquared > 0f)
            {
                float toPointX = truePos.x - start.x;
                float toPointZ = truePos.z - start.z;
                t = (toPointX * deltaX + toPointZ * deltaZ) / lengthSquared;
                if (t < 0f)
                {
                    t = 0f;
                }
                if (t > 1f)
                {
                    t = 1f;
                }
            }
            else
            {
                t = 0f;
            }

            Vector3 position = Vector3.Lerp(start, end, t);
            float distanceX = truePos.x - position.x;
            float distanceZ = truePos.z - position.z;
            float distance = Mathf.Sqrt(distanceX * distanceX + distanceZ * distanceZ);

            float startDistance = data.GetPointDistance(segmentPointIndex);
            float endDistance = data.GetPointDistance(segmentPointIndex + 1);
            float distanceAlong = startDistance + t * (endDistance - startDistance);

            Vector3 tangent;
            if (lengthSquared > 0f)
            {
                float inverseLength = 1f / Mathf.Sqrt(lengthSquared);
                tangent = new Vector3(deltaX * inverseLength, 0f, deltaZ * inverseLength);
            }
            else
            {
                tangent = Vector3.zero;
            }

            result = new RoadPoint(roadIndex, segmentPointIndex, distanceAlong, position, distance, tangent);
        }

        private int FindRoadInOutput(List<RoadPoint> output, int roadIndex)
        {
            for (int i = 0; i < output.Count; i++)
            {
                if (output[i].RoadIndex == roadIndex)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
