using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class SnapFinder
    {
        private const float SampleSpacing = 0.5f;
        private const float EndEpsilon = 0.0001f;
        private const int MinSamplesPerSegment = 8;
        private const int MaxSamplesPerSegment = 256;
        private const int RefineIterations = 24;

        private readonly RoadCurve curve;

        public SnapFinder()
        {
            curve = new RoadCurve();
        }

        public bool FindSnap(Vector3 truePos, float snapDistance, RoadNetworkAuthoring asset, int ignoreRoadId, out SnapTarget target)
        {
            int ignoredStartId = 0;
            int ignoredEndId = 0;
            if (ignoreRoadId != 0)
            {
                AuthoringRoad ignoredRoad = asset.FindRoad(ignoreRoadId);
                if (ignoredRoad != null)
                {
                    ignoredStartId = ignoredRoad.StartIntersectionId;
                    ignoredEndId = ignoredRoad.EndIntersectionId;
                }
            }

            if (FindIntersection(truePos, snapDistance, asset, ignoredStartId, ignoredEndId, out target))
            {
                return true;
            }

            if (FindRoadPoint(truePos, snapDistance, asset, ignoreRoadId, out target))
            {
                if (target.Kind == SnapTargetKind.Intersection && IsIgnoredIntersection(target.IntersectionId, ignoredStartId, ignoredEndId))
                {
                    target = new SnapTarget();
                    return false;
                }
                return true;
            }

            target = new SnapTarget();
            return false;
        }

        private bool FindIntersection(Vector3 truePos, float snapDistance, RoadNetworkAuthoring asset, int ignoredStartId, int ignoredEndId, out SnapTarget target)
        {
            List<AuthoringIntersection> intersections = asset.Intersections;
            float bestSqrDistance = snapDistance * snapDistance;
            AuthoringIntersection best = null;

            for (int i = 0; i < intersections.Count; i++)
            {
                AuthoringIntersection intersection = intersections[i];
                if (IsIgnoredIntersection(intersection.Id, ignoredStartId, ignoredEndId))
                {
                    continue;
                }

                float sqrDistance = (intersection.Position - truePos).sqrMagnitude;
                if (sqrDistance <= bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = intersection;
                }
            }

            if (best == null)
            {
                target = new SnapTarget();
                return false;
            }

            target = new SnapTarget(SnapTargetKind.Intersection, best.Id, 0, 0, 0f, best.Position);
            return true;
        }

        private bool IsIgnoredIntersection(int intersectionId, int ignoredStartId, int ignoredEndId)
        {
            if (intersectionId == 0)
            {
                return false;
            }
            return intersectionId == ignoredStartId || intersectionId == ignoredEndId;
        }

        private bool FindRoadPoint(Vector3 truePos, float snapDistance, RoadNetworkAuthoring asset, int ignoreRoadId, out SnapTarget target)
        {
            List<AuthoringRoad> roads = asset.Roads;
            float bestSqrDistance = snapDistance * snapDistance;
            AuthoringRoad bestRoad = null;
            int bestSegment = 0;
            float bestT = 0f;

            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                if (road.Id == ignoreRoadId)
                {
                    continue;
                }

                int segmentCount = road.KeyPoints.Count - 1;
                for (int segment = 0; segment < segmentCount; segment++)
                {
                    if (!IsSegmentInRange(road, segment, truePos, snapDistance))
                    {
                        continue;
                    }

                    float t = FindClosestT(road, segment, truePos);
                    float sqrDistance = (curve.Evaluate(road, segment, t) - truePos).sqrMagnitude;
                    if (sqrDistance <= bestSqrDistance)
                    {
                        bestSqrDistance = sqrDistance;
                        bestRoad = road;
                        bestSegment = segment;
                        bestT = t;
                    }
                }
            }

            if (bestRoad == null)
            {
                target = new SnapTarget();
                return false;
            }

            target = BuildRoadTarget(bestRoad, bestSegment, bestT);
            return true;
        }

        private bool IsSegmentInRange(AuthoringRoad road, int segment, Vector3 truePos, float snapDistance)
        {
            AuthoringKeyPoint start = road.KeyPoints[segment];
            AuthoringKeyPoint end = road.KeyPoints[segment + 1];

            Vector3 p0 = start.Position;
            Vector3 p1 = start.Position + start.OutHandle;
            Vector3 p2 = end.Position + end.InHandle;
            Vector3 p3 = end.Position;

            Vector3 min = Vector3.Min(Vector3.Min(p0, p1), Vector3.Min(p2, p3));
            Vector3 max = Vector3.Max(Vector3.Max(p0, p1), Vector3.Max(p2, p3));

            if (truePos.x < min.x - snapDistance || truePos.x > max.x + snapDistance)
            {
                return false;
            }
            if (truePos.y < min.y - snapDistance || truePos.y > max.y + snapDistance)
            {
                return false;
            }
            if (truePos.z < min.z - snapDistance || truePos.z > max.z + snapDistance)
            {
                return false;
            }
            return true;
        }

        private float FindClosestT(AuthoringRoad road, int segment, Vector3 truePos)
        {
            AuthoringKeyPoint start = road.KeyPoints[segment];
            AuthoringKeyPoint end = road.KeyPoints[segment + 1];
            float polygonLength = start.OutHandle.magnitude
                + ((end.Position + end.InHandle) - (start.Position + start.OutHandle)).magnitude
                + end.InHandle.magnitude;

            int samples = Mathf.CeilToInt(polygonLength / SampleSpacing);
            samples = Mathf.Clamp(samples, MinSamplesPerSegment, MaxSamplesPerSegment);

            float bestT = 0f;
            float bestSqrDistance = float.MaxValue;
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                float sqrDistance = (curve.Evaluate(road, segment, t) - truePos).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestT = t;
                }
            }

            float step = 1f / samples;
            float low = Mathf.Max(0f, bestT - step);
            float high = Mathf.Min(1f, bestT + step);
            for (int i = 0; i < RefineIterations; i++)
            {
                float third = (high - low) / 3f;
                float left = low + third;
                float right = high - third;
                float leftSqrDistance = (curve.Evaluate(road, segment, left) - truePos).sqrMagnitude;
                float rightSqrDistance = (curve.Evaluate(road, segment, right) - truePos).sqrMagnitude;
                if (leftSqrDistance < rightSqrDistance)
                {
                    high = right;
                }
                else
                {
                    low = left;
                }
            }

            float refinedT = (low + high) * 0.5f;
            float refinedSqrDistance = (curve.Evaluate(road, segment, refinedT) - truePos).sqrMagnitude;
            if (refinedSqrDistance < bestSqrDistance)
            {
                return refinedT;
            }
            return bestT;
        }

        private SnapTarget BuildRoadTarget(AuthoringRoad road, int segment, float t)
        {
            int lastSegment = road.KeyPoints.Count - 2;

            if (segment == 0 && t <= EndEpsilon)
            {
                return new SnapTarget(SnapTargetKind.Intersection, road.StartIntersectionId, 0, 0, 0f, road.KeyPoints[0].Position);
            }

            if (segment == lastSegment && t >= 1f - EndEpsilon)
            {
                return new SnapTarget(SnapTargetKind.Intersection, road.EndIntersectionId, 0, 0, 0f, road.KeyPoints[road.KeyPoints.Count - 1].Position);
            }

            if (t >= 1f - EndEpsilon)
            {
                segment++;
                t = 0f;
            }

            Vector3 position = curve.Evaluate(road, segment, t);
            return new SnapTarget(SnapTargetKind.Road, 0, road.Id, segment, t, position);
        }
    }
}
