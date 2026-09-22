using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class NavigationSession
    {
        private const float DefaultArrivalDistance = 10f;
        private const float LookAheadDistance = 30f;

        private readonly List<float> segmentStartDistances;

        private Route route;

        public float ProgressDistance { get; private set; }
        public float RemainingDistance { get; private set; }
        public float Eta { get; private set; }
        public float TrimDistance { get { return ProgressDistance; } }
        public float WrongWayDistance { get; private set; }
        public float ArrivalDistance { get; set; }
        public int CurrentSegment { get; private set; }
        public bool OffRoad { get; private set; }
        public bool WrongTurn { get; private set; }
        public bool Arrived { get; private set; }

        public NavigationSession()
        {
            segmentStartDistances = new List<float>();
            ArrivalDistance = DefaultArrivalDistance;
        }

        public void UpdateNavigationSessionLogic(RoadMatcher matcher, float drivenDistance)
        {
            if (route == null || route.Segments.Count == 0)
            {
                return;
            }

            if (!matcher.IsOnRoad)
            {
                OffRoad = true;
                return;
            }

            OffRoad = false;

            if (TryMatchCurrentSegment(matcher))
            {
                WrongWayDistance = 0f;
                WrongTurn = false;
            }
            else if (TryMatchLookAheadSegment(matcher))
            {
                WrongWayDistance = 0f;
                WrongTurn = false;
            }
            else if (IsOppositeOnCurrentSegment(matcher))
            {
                WrongWayDistance += drivenDistance;
                WrongTurn = false;
            }
            else
            {
                WrongWayDistance = 0f;
                WrongTurn = true;
            }

            UpdateRemainingAndEta();
            UpdateArrival(matcher);
        }

        public void Start(Route route)
        {
            this.route = route;
            segmentStartDistances.Clear();

            float cumulative = 0f;
            for (int i = 0; i < route.Segments.Count; i++)
            {
                segmentStartDistances.Add(cumulative);
                cumulative += GetSegmentLength(route.Segments[i]);
            }

            CurrentSegment = 0;
            ProgressDistance = 0f;
            RemainingDistance = route.Length;
            Eta = route.Eta;
            WrongWayDistance = 0f;
            WrongTurn = false;
            OffRoad = false;
            Arrived = false;
        }

        public void JumpTo(RoadMatcher matcher)
        {
            int bestSegment = -1;
            float bestProgress = 0f;
            float bestDistanceFromCurrent = 0f;

            for (int i = 0; i < route.Segments.Count; i++)
            {
                RouteSegment segment = route.Segments[i];
                if (segment.RoadIndex != matcher.RoadIndex)
                {
                    continue;
                }
                if (segment.Forward != matcher.MovingForward)
                {
                    continue;
                }

                float candidateProgress = segmentStartDistances[i] + ComputeProgressWithinSegment(segment, matcher.DistanceAlong);
                float distanceFromCurrent = Mathf.Abs(candidateProgress - ProgressDistance);

                if (bestSegment < 0 || distanceFromCurrent < bestDistanceFromCurrent)
                {
                    bestSegment = i;
                    bestProgress = candidateProgress;
                    bestDistanceFromCurrent = distanceFromCurrent;
                }
            }

            if (bestSegment < 0)
            {
                return;
            }

            CurrentSegment = bestSegment;
            ProgressDistance = bestProgress;
            WrongWayDistance = 0f;
            WrongTurn = false;
            OffRoad = false;
            UpdateRemainingAndEta();
        }

        public void Stop()
        {
            route = null;
            segmentStartDistances.Clear();
            CurrentSegment = 0;
            ProgressDistance = 0f;
            RemainingDistance = 0f;
            Eta = 0f;
            WrongWayDistance = 0f;
            OffRoad = false;
            WrongTurn = false;
            Arrived = false;
        }

        private bool TryMatchCurrentSegment(RoadMatcher matcher)
        {
            RouteSegment segment = route.Segments[CurrentSegment];
            if (segment.RoadIndex != matcher.RoadIndex)
            {
                return false;
            }
            if (segment.Forward != matcher.MovingForward)
            {
                return false;
            }

            ApplySegmentProgress(CurrentSegment, segment, matcher.DistanceAlong);
            return true;
        }

        private bool TryMatchLookAheadSegment(RoadMatcher matcher)
        {
            float skipped = 0f;
            int segmentIndex = CurrentSegment + 1;
            while (segmentIndex < route.Segments.Count && skipped < LookAheadDistance)
            {
                RouteSegment segment = route.Segments[segmentIndex];
                if (segment.RoadIndex == matcher.RoadIndex && segment.Forward == matcher.MovingForward)
                {
                    ApplySegmentProgress(segmentIndex, segment, matcher.DistanceAlong);
                    return true;
                }

                skipped += GetSegmentLength(segment);
                segmentIndex++;
            }

            return false;
        }

        private bool IsOppositeOnCurrentSegment(RoadMatcher matcher)
        {
            RouteSegment segment = route.Segments[CurrentSegment];
            if (segment.RoadIndex != matcher.RoadIndex)
            {
                return false;
            }
            return segment.Forward != matcher.MovingForward;
        }

        private void UpdateRemainingAndEta()
        {
            RemainingDistance = route.Length - ProgressDistance;

            RouteSegment currentSegment = route.Segments[CurrentSegment];
            float remainingInSegment = segmentStartDistances[CurrentSegment] + GetSegmentLength(currentSegment) - ProgressDistance;
            float roadSpeed = route.Network.GetRoad(currentSegment.RoadIndex).Speed;

            float eta = 0f;
            if (roadSpeed > 0f)
            {
                eta = remainingInSegment / roadSpeed;
            }

            for (int i = CurrentSegment + 1; i < route.Segments.Count; i++)
            {
                RouteSegment segment = route.Segments[i];
                float speed = route.Network.GetRoad(segment.RoadIndex).Speed;
                if (speed > 0f)
                {
                    eta += GetSegmentLength(segment) / speed;
                }
            }

            Eta = eta;
        }

        private void UpdateArrival(RoadMatcher matcher)
        {
            int lastSegmentIndex = route.Segments.Count - 1;
            if (CurrentSegment != lastSegmentIndex)
            {
                return;
            }

            RouteSegment lastSegment = route.Segments[lastSegmentIndex];
            if (lastSegment.RoadIndex != matcher.RoadIndex)
            {
                return;
            }

            bool passed;
            if (lastSegment.Forward)
            {
                passed = matcher.DistanceAlong >= lastSegment.ToDistance;
            }
            else
            {
                passed = matcher.DistanceAlong <= lastSegment.ToDistance;
            }

            if (passed || RemainingDistance <= ArrivalDistance)
            {
                Arrived = true;
            }
        }

        private float GetSegmentLength(RouteSegment segment)
        {
            return Mathf.Abs(segment.ToDistance - segment.FromDistance);
        }

        private float ComputeProgressWithinSegment(RouteSegment segment, float distanceAlong)
        {
            float raw = Mathf.Abs(distanceAlong - segment.FromDistance);
            float length = GetSegmentLength(segment);
            return Mathf.Clamp(raw, 0f, length);
        }

        private void ApplySegmentProgress(int segmentIndex, RouteSegment segment, float distanceAlong)
        {
            CurrentSegment = segmentIndex;
            ProgressDistance = segmentStartDistances[segmentIndex] + ComputeProgressWithinSegment(segment, distanceAlong);
        }
    }
}
