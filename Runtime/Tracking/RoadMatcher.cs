using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class RoadMatcher
    {
        private const float DefaultLeaveMargin = 3f;
        private const float DefaultUnconnectedSwitchDistance = 10f;
        private const float HeadingWeight = 10f;
        private const float ContinuityCheckDistance = 10f;
        private const float ContinuitySearchRadius = 20f;
        private const float UnconnectedBetterMargin = 2f;
        private const float LostSearchRadius = 30f;
        private const float EndTolerance = 0.001f;
        private const int MinForkCandidates = 3;
        private const int SearchCapacity = 64;

        private readonly List<RoadPoint> searchResults;
        private readonly RoadPoint[] forkPoints;
        private readonly float[] forkScores;
        private readonly RoadNetworkData data;
        private readonly RoadQuery query;

        private Vector3 lastTruePosition;
        private float continuityDistance;
        private float unconnectedDistance;
        private int forkCount;
        private int unconnectedRoad;
        private bool hasLastPosition;
        private bool searchedWhileStopped;

        public Vector3 SnappedPosition { get; private set; }
        public Vector3 RoadTangent { get; private set; }
        public float LeaveMargin { get; set; }
        public float UnconnectedSwitchDistance { get; set; }
        public float DistanceAlong { get; private set; }
        public int RoadIndex { get; private set; }
        public int ForkCandidateCount { get { return forkCount; } }
        public bool IsOnRoad { get; private set; }
        public bool MovingForward { get; private set; }
        public bool IsInFork { get { return forkCount > 0; } }
        public bool EnteredRoad { get; private set; }
        public bool LeftRoad { get; private set; }
        public bool ChangedRoad { get; private set; }

        public RoadMatcher(RoadNetworkData data)
        {
            this.data = data;
            query = new RoadQuery(data);
            searchResults = new List<RoadPoint>(SearchCapacity);
            int forkCapacity = GetForkCapacity(data);
            forkPoints = new RoadPoint[forkCapacity];
            forkScores = new float[forkCapacity];
            LeaveMargin = DefaultLeaveMargin;
            UnconnectedSwitchDistance = DefaultUnconnectedSwitchDistance;
            Reset();
        }

        public void UpdateRoadMatchingLogic(Vector3 truePos, Vector3 movementHeading, bool stopped, bool teleported)
        {
            EnteredRoad = false;
            LeftRoad = false;
            ChangedRoad = false;

            if (stopped && !teleported)
            {
                if (!IsOnRoad && !searchedWhileStopped)
                {
                    searchedWhileStopped = true;
                    RunLostSearch(truePos, movementHeading);
                }
                return;
            }

            searchedWhileStopped = false;

            float driven = 0f;
            if (teleported)
            {
                Reset();
            }
            else if (hasLastPosition)
            {
                driven = GetFlatDistance(truePos, lastTruePosition);
            }
            lastTruePosition = truePos;
            hasLastPosition = true;

            if (forkCount > 0)
            {
                UpdateFork(truePos, movementHeading);
            }
            else if (IsOnRoad)
            {
                UpdateOnRoad(truePos, movementHeading, driven);
            }

            if (!IsOnRoad)
            {
                RunLostSearch(truePos, movementHeading);
            }
        }

        public void Reset()
        {
            BecomeLost();
            EnteredRoad = false;
            LeftRoad = false;
            ChangedRoad = false;
            DistanceAlong = 0f;
            SnappedPosition = Vector3.zero;
            RoadTangent = Vector3.zero;
            MovingForward = true;
            hasLastPosition = false;
            searchedWhileStopped = false;
        }

        private int GetForkCapacity(RoadNetworkData network)
        {
            int capacity = MinForkCandidates;
            for (int i = 0; i < network.IntersectionCount; i++)
            {
                int linkCount = network.GetIntersection(i).LinkCount;
                if (linkCount > capacity)
                {
                    capacity = linkCount;
                }
            }
            return capacity;
        }

        private void RunLostSearch(Vector3 truePos, Vector3 heading)
        {
            query.FindAllWithin(truePos, LostSearchRadius, searchResults);

            int bestIndex = -1;
            float bestScore = 0f;
            for (int i = 0; i < searchResults.Count; i++)
            {
                RoadPoint point = searchResults[i];
                RoadRecord road = data.GetRoad(point.RoadIndex);
                if (point.Distance > GetBackThreshold(road))
                {
                    continue;
                }

                float score = Score(point, heading, road);
                if (bestIndex < 0 || score < bestScore)
                {
                    bestIndex = i;
                    bestScore = score;
                }
            }

            if (bestIndex < 0)
            {
                return;
            }

            RoadPoint best = searchResults[bestIndex];
            IsOnRoad = true;
            EnteredRoad = true;
            RoadIndex = best.RoadIndex;
            ApplyPoint(best, heading);
        }

        private float GetFlatDistance(Vector3 a, Vector3 b)
        {
            float deltaX = a.x - b.x;
            float deltaZ = a.z - b.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private void UpdateFork(Vector3 truePos, Vector3 heading)
        {
            int kept = 0;
            int withinBackCount = 0;
            int withinBackIndex = -1;
            for (int i = 0; i < forkCount; i++)
            {
                int roadIndex = forkPoints[i].RoadIndex;
                RoadRecord road = data.GetRoad(roadIndex);

                RoadPoint point;
                query.ProjectOnRoad(roadIndex, truePos, out point);
                if (point.Distance > GetLeaveThreshold(road))
                {
                    continue;
                }

                forkPoints[kept] = point;
                forkScores[kept] = Score(point, heading, road);
                if (point.Distance <= GetBackThreshold(road))
                {
                    withinBackCount++;
                    withinBackIndex = kept;
                }
                kept++;
            }

            forkCount = kept;

            if (kept == 0)
            {
                LeftRoad = true;
                BecomeLost();
                return;
            }

            if (kept == 1)
            {
                forkCount = 0;
                SetCurrentRoad(forkPoints[0], heading);
                return;
            }

            if (withinBackCount == 1)
            {
                forkCount = 0;
                SetCurrentRoad(forkPoints[withinBackIndex], heading);
                return;
            }

            SetCurrentRoad(forkPoints[FindBestForkCandidate()], heading);
        }

        private void UpdateOnRoad(Vector3 truePos, Vector3 heading, float driven)
        {
            RoadRecord road = data.GetRoad(RoadIndex);

            RoadPoint point;
            query.ProjectOnRoad(RoadIndex, truePos, out point);

            int passedIntersection = GetPassedIntersection(truePos, point, road);
            if (passedIntersection >= 0)
            {
                ChooseAtIntersection(truePos, heading, passedIntersection);
                return;
            }

            if (point.Distance > GetLeaveThreshold(road))
            {
                LeftRoad = true;
                BecomeLost();
                return;
            }

            ApplyPoint(point, heading);
            CheckContinuity(truePos, heading, point, road, driven);
        }

        private void BecomeLost()
        {
            IsOnRoad = false;
            RoadIndex = -1;
            forkCount = 0;
            continuityDistance = 0f;
            ClearUnconnected();
        }

        private float GetBackThreshold(RoadRecord road)
        {
            return road.Width * 0.5f;
        }

        private float Score(RoadPoint point, Vector3 heading, RoadRecord road)
        {
            float dot = heading.x * point.Tangent.x + heading.z * point.Tangent.z;
            float alignment;
            if (road.OneWay)
            {
                alignment = dot;
            }
            else
            {
                alignment = Mathf.Abs(dot);
            }
            return point.Distance + HeadingWeight * (1f - alignment);
        }

        private void ApplyPoint(RoadPoint point, Vector3 heading)
        {
            DistanceAlong = point.DistanceAlong;
            SnappedPosition = point.Position;
            RoadTangent = point.Tangent;
            MovingForward = heading.x * point.Tangent.x + heading.z * point.Tangent.z >= 0f;
        }

        private float GetLeaveThreshold(RoadRecord road)
        {
            return road.Width * 0.5f + LeaveMargin;
        }

        private void SetCurrentRoad(RoadPoint point, Vector3 heading)
        {
            if (point.RoadIndex != RoadIndex)
            {
                ChangedRoad = true;
                ClearUnconnected();
            }
            RoadIndex = point.RoadIndex;
            ApplyPoint(point, heading);
        }

        private int FindBestForkCandidate()
        {
            int best = 0;
            for (int i = 1; i < forkCount; i++)
            {
                if (forkScores[i] < forkScores[best])
                {
                    best = i;
                }
            }
            return best;
        }

        private int GetPassedIntersection(Vector3 truePos, RoadPoint point, RoadRecord road)
        {
            float along = (truePos.x - point.Position.x) * point.Tangent.x + (truePos.z - point.Position.z) * point.Tangent.z;
            if (along > EndTolerance && point.DistanceAlong >= road.Length - EndTolerance)
            {
                return road.EndIntersection;
            }
            if (along < -EndTolerance && point.DistanceAlong <= EndTolerance)
            {
                return road.StartIntersection;
            }
            return -1;
        }

        private void ChooseAtIntersection(Vector3 truePos, Vector3 heading, int intersectionIndex)
        {
            int previousRoad = RoadIndex;
            IntersectionRecord intersection = data.GetIntersection(intersectionIndex);
            bool deadEnd = data.IsDeadEnd(intersectionIndex);

            forkCount = 0;
            for (int l = 0; l < intersection.LinkCount; l++)
            {
                int roadIndex = data.GetLink(intersection.FirstLink + l);
                if (roadIndex == previousRoad && !deadEnd)
                {
                    continue;
                }

                RoadRecord candidate = data.GetRoad(roadIndex);
                RoadPoint point;
                query.ProjectOnRoad(roadIndex, truePos, out point);
                if (point.Distance > GetLeaveThreshold(candidate))
                {
                    continue;
                }

                AddForkCandidate(point, Score(point, heading, candidate));
            }

            if (forkCount == 0)
            {
                LeftRoad = true;
                BecomeLost();
                return;
            }

            if (forkCount == 1)
            {
                forkCount = 0;
                SetCurrentRoad(forkPoints[0], heading);
                return;
            }

            SetCurrentRoad(forkPoints[FindBestForkCandidate()], heading);
        }

        private void CheckContinuity(Vector3 truePos, Vector3 heading, RoadPoint currentPoint, RoadRecord currentRoad, float driven)
        {
            float currentScore = Score(currentPoint, heading, currentRoad);

            if (unconnectedRoad >= 0)
            {
                RoadRecord other = data.GetRoad(unconnectedRoad);
                RoadPoint otherPoint;
                query.ProjectOnRoad(unconnectedRoad, truePos, out otherPoint);
                if (IsClearlyBetter(otherPoint, other, heading, currentScore))
                {
                    unconnectedDistance += driven;
                    if (unconnectedDistance >= UnconnectedSwitchDistance)
                    {
                        SetCurrentRoad(otherPoint, heading);
                    }
                    return;
                }
                ClearUnconnected();
            }

            continuityDistance += driven;
            if (continuityDistance < ContinuityCheckDistance)
            {
                return;
            }
            continuityDistance = 0f;

            query.FindAllWithin(truePos, ContinuitySearchRadius, searchResults);

            int bestIndex = -1;
            float bestScore = 0f;
            for (int i = 0; i < searchResults.Count; i++)
            {
                RoadPoint point = searchResults[i];
                if (point.RoadIndex == RoadIndex)
                {
                    continue;
                }

                RoadRecord road = data.GetRoad(point.RoadIndex);
                if (AreConnected(currentRoad, road))
                {
                    continue;
                }
                if (!IsClearlyBetter(point, road, heading, currentScore))
                {
                    continue;
                }

                float score = Score(point, heading, road);
                if (bestIndex < 0 || score < bestScore)
                {
                    bestIndex = i;
                    bestScore = score;
                }
            }

            if (bestIndex >= 0)
            {
                unconnectedRoad = searchResults[bestIndex].RoadIndex;
                unconnectedDistance = 0f;
            }
        }

        private void ClearUnconnected()
        {
            unconnectedRoad = -1;
            unconnectedDistance = 0f;
        }

        private void AddForkCandidate(RoadPoint point, float score)
        {
            for (int i = 0; i < forkCount; i++)
            {
                if (forkPoints[i].RoadIndex == point.RoadIndex)
                {
                    if (score < forkScores[i])
                    {
                        forkPoints[i] = point;
                        forkScores[i] = score;
                    }
                    return;
                }
            }

            if (forkCount < forkPoints.Length)
            {
                forkPoints[forkCount] = point;
                forkScores[forkCount] = score;
                forkCount++;
                return;
            }

            int worst = 0;
            for (int i = 1; i < forkCount; i++)
            {
                if (forkScores[i] > forkScores[worst])
                {
                    worst = i;
                }
            }
            if (score < forkScores[worst])
            {
                forkPoints[worst] = point;
                forkScores[worst] = score;
            }
        }

        private bool IsClearlyBetter(RoadPoint point, RoadRecord road, Vector3 heading, float currentScore)
        {
            if (point.Distance > GetBackThreshold(road))
            {
                return false;
            }
            return Score(point, heading, road) < currentScore - UnconnectedBetterMargin;
        }

        private bool AreConnected(RoadRecord a, RoadRecord b)
        {
            if (a.StartIntersection == b.StartIntersection || a.StartIntersection == b.EndIntersection)
            {
                return true;
            }
            return a.EndIntersection == b.StartIntersection || a.EndIntersection == b.EndIntersection;
        }
    }
}
