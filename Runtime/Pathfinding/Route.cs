using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public class Route
    {
        private readonly List<RouteSegment> segments;

        private WorldConverter converter;

        internal RoadNetworkData Network { get; set; }

        public IReadOnlyList<RouteSegment> Segments { get { return segments; } }
        public FailureReason Failure { get; internal set; }
        public RoadPoint Start { get; internal set; }
        public RoadPoint End { get; internal set; }
        public Vector3 Destination { get; internal set; }
        public float Length { get; internal set; }
        public float Eta { get; internal set; }
        public bool Success { get; internal set; }
        public bool ArrivedImmediately { get; internal set; }

        public Route()
        {
            segments = new List<RouteSegment>();
        }

        public void Clear()
        {
            Success = false;
            Failure = FailureReason.None;
            ArrivedImmediately = false;
            Start = default(RoadPoint);
            End = default(RoadPoint);
            Destination = Vector3.zero;
            Length = 0f;
            Eta = 0f;
            Network = null;
            segments.Clear();
        }

        internal void AddSegment(RouteSegment segment)
        {
            segments.Add(segment);
        }

        public void GetTruePoints(List<Vector3> output)
        {
            output.Clear();
            if (Network == null)
            {
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                AppendSegmentPoints(segments[i], i == 0, output);
            }
        }

        private void AppendSegmentPoints(RouteSegment segment, bool includeFirstPoint, List<Vector3> output)
        {
            RoadRecord road = Network.GetRoad(segment.RoadIndex);

            if (segment.Forward)
            {
                AppendPointsForward(road, segment.FromDistance, segment.ToDistance, includeFirstPoint, output);
            }
            else
            {
                AppendPointsBackward(road, segment.FromDistance, segment.ToDistance, includeFirstPoint, output);
            }
        }

        private void AppendPointsForward(RoadRecord road, float fromDistance, float toDistance, bool includeFirstPoint, List<Vector3> output)
        {
            if (includeFirstPoint)
            {
                output.Add(GetPositionAtDistance(road, fromDistance));
            }

            int firstIndex = road.FirstPoint;
            int lastIndex = road.FirstPoint + road.PointCount - 1;
            for (int i = firstIndex; i <= lastIndex; i++)
            {
                float distance = Network.GetPointDistance(i);
                if (distance > fromDistance && distance < toDistance)
                {
                    output.Add(Network.GetPoint(i));
                }
            }

            output.Add(GetPositionAtDistance(road, toDistance));
        }

        private void AppendPointsBackward(RoadRecord road, float fromDistance, float toDistance, bool includeFirstPoint, List<Vector3> output)
        {
            if (includeFirstPoint)
            {
                output.Add(GetPositionAtDistance(road, fromDistance));
            }

            int firstIndex = road.FirstPoint;
            int lastIndex = road.FirstPoint + road.PointCount - 1;
            for (int i = lastIndex; i >= firstIndex; i--)
            {
                float distance = Network.GetPointDistance(i);
                if (distance < fromDistance && distance > toDistance)
                {
                    output.Add(Network.GetPoint(i));
                }
            }

            output.Add(GetPositionAtDistance(road, toDistance));
        }

        private Vector3 GetPositionAtDistance(RoadRecord road, float distance)
        {
            int firstIndex = road.FirstPoint;
            int lastIndex = road.FirstPoint + road.PointCount - 1;

            float firstDistance = Network.GetPointDistance(firstIndex);
            float lastDistance = Network.GetPointDistance(lastIndex);

            if (distance <= firstDistance)
            {
                return Network.GetPoint(firstIndex);
            }
            if (distance >= lastDistance)
            {
                return Network.GetPoint(lastIndex);
            }

            for (int i = firstIndex; i < lastIndex; i++)
            {
                float startDistance = Network.GetPointDistance(i);
                float endDistance = Network.GetPointDistance(i + 1);
                if (distance >= startDistance && distance <= endDistance)
                {
                    float segmentLength = endDistance - startDistance;
                    float t;
                    if (segmentLength > 0f)
                    {
                        t = (distance - startDistance) / segmentLength;
                    }
                    else
                    {
                        t = 0f;
                    }
                    return Vector3.Lerp(Network.GetPoint(i), Network.GetPoint(i + 1), t);
                }
            }

            return Network.GetPoint(lastIndex);
        }

        public void GetPoints(List<Vector3> output)
        {
            GetTruePoints(output);
            if (converter == null)
            {
                return;
            }

            for (int i = 0; i < output.Count; i++)
            {
                output[i] = converter.TrueToWorld(output[i]);
            }
        }

        public void SetConverter(WorldConverter value)
        {
            converter = value;
        }
    }
}
