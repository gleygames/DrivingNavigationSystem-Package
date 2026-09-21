using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadEditOperations
    {
        private readonly RoadNetworkAuthoring asset;
        private readonly RoadCurve curve;
        private readonly RoadSampler sampler;
        private readonly IGroundProbe probe;
        private readonly float maxDeviation;
        private readonly float maxSpacing;

        public RoadEditOperations(RoadNetworkAuthoring asset, IGroundProbe probe, float maxDeviation, float maxSpacing)
        {
            this.asset = asset;
            this.probe = probe;
            this.maxDeviation = maxDeviation;
            this.maxSpacing = maxSpacing;
            curve = new RoadCurve();
            sampler = new RoadSampler();
        }

        public int CreateRoad(List<Vector3> keyPoints, RoadBrush brush, int startIntersectionId, int endIntersectionId)
        {
            if (keyPoints.Count < 2)
            {
                return 0;
            }

            AuthoringRoad road = new AuthoringRoad(asset.NewRoadId());
            road.SetTypeId(brush.TypeId);
            road.SetOneWay(brush.OneWay);
            road.SetSpeedOverride(brush.SpeedOverride);
            road.SetWidthOverride(brush.WidthOverride);

            for (int i = 0; i < keyPoints.Count; i++)
            {
                road.KeyPoints.Add(new AuthoringKeyPoint(keyPoints[i]));
            }

            int resolvedStartId = ResolveIntersection(startIntersectionId, road.KeyPoints[0]);
            int resolvedEndId = ResolveIntersection(endIntersectionId, road.KeyPoints[road.KeyPoints.Count - 1]);
            road.SetStartIntersectionId(resolvedStartId);
            road.SetEndIntersectionId(resolvedEndId);

            asset.Roads.Add(road);

            FinishRoads(road);

            return road.Id;
        }

        private int ResolveIntersection(int intersectionId, AuthoringKeyPoint endpoint)
        {
            if (intersectionId == 0)
            {
                int newId = asset.NewIntersectionId();
                asset.Intersections.Add(new AuthoringIntersection(newId, endpoint.Position));
                return newId;
            }

            AuthoringIntersection existing = asset.FindIntersection(intersectionId);
            if (existing != null)
            {
                endpoint.SetPosition(existing.Position);
            }
            return intersectionId;
        }

        private void FinishRoads(AuthoringRoad road)
        {
            FinishRoad(road);
            asset.MarkChanged();
        }

        private void FinishRoad(AuthoringRoad road)
        {
            curve.UpdateAutoHandles(road);
            sampler.Sample(road, probe, maxDeviation, maxSpacing);
            if (!string.IsNullOrEmpty(road.SourceTag))
            {
                road.SetModifiedAfterImport(true);
            }
        }

        public bool ExtendRoad(int roadId, bool atEnd, Vector3 newKeyPoint)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return false;
            }

            int intersectionId;
            if (atEnd)
            {
                intersectionId = road.EndIntersectionId;
            }
            else
            {
                intersectionId = road.StartIntersectionId;
            }

            List<AuthoringRoad> connected = new List<AuthoringRoad>();
            asset.GetRoadsAtIntersection(intersectionId, connected);
            if (connected.Count != 1)
            {
                return false;
            }

            AuthoringIntersection intersection = asset.FindIntersection(intersectionId);

            if (atEnd)
            {
                int lastIndex = road.KeyPoints.Count - 1;
                AuthoringKeyPoint innerPoint = new AuthoringKeyPoint(road.KeyPoints[lastIndex].Position);
                road.KeyPoints.Insert(lastIndex, innerPoint);
                road.KeyPoints[road.KeyPoints.Count - 1].SetPosition(newKeyPoint);
            }
            else
            {
                AuthoringKeyPoint innerPoint = new AuthoringKeyPoint(road.KeyPoints[0].Position);
                road.KeyPoints.Insert(1, innerPoint);
                road.KeyPoints[0].SetPosition(newKeyPoint);
            }

            intersection.SetPosition(newKeyPoint);

            FinishRoads(road);

            return true;
        }

        public bool InsertKeyPoint(int roadId, int segment, float t)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return false;
            }
            if (segment < 0 || segment >= road.KeyPoints.Count - 1)
            {
                return false;
            }

            AuthoringKeyPoint segmentStart = road.KeyPoints[segment];
            AuthoringKeyPoint segmentEnd = road.KeyPoints[segment + 1];

            Vector3 newStartOutHandle;
            Vector3 newEndInHandle;
            AuthoringKeyPoint splitPoint = curve.Split(road, segment, t, out newStartOutHandle, out newEndInHandle);

            segmentStart.SetOutHandle(newStartOutHandle);
            segmentStart.SetManualHandles(true);
            segmentEnd.SetInHandle(newEndInHandle);
            segmentEnd.SetManualHandles(true);
            road.KeyPoints.Insert(segment + 1, splitPoint);

            FinishRoads(road);

            return true;
        }

        public bool DeleteKeyPoint(int roadId, int index)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return false;
            }
            if (index <= 0 || index >= road.KeyPoints.Count - 1)
            {
                return false;
            }

            road.KeyPoints.RemoveAt(index);

            FinishRoads(road);

            return true;
        }

        public bool MoveKeyPoint(int roadId, int index, Vector3 position)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return false;
            }
            if (index <= 0 || index >= road.KeyPoints.Count - 1)
            {
                return false;
            }

            road.KeyPoints[index].SetPosition(position);

            FinishRoads(road);

            return true;
        }

        public bool MoveHandle(int roadId, int index, bool isOut, Vector3 offset)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return false;
            }
            if (index < 0 || index >= road.KeyPoints.Count)
            {
                return false;
            }

            AuthoringKeyPoint keyPoint = road.KeyPoints[index];
            keyPoint.SetManualHandles(true);
            if (isOut)
            {
                keyPoint.SetOutHandle(offset);
            }
            else
            {
                keyPoint.SetInHandle(offset);
            }

            FinishRoads(road);

            return true;
        }

        public bool MoveIntersection(int id, Vector3 position)
        {
            AuthoringIntersection intersection = asset.FindIntersection(id);
            if (intersection == null)
            {
                return false;
            }

            intersection.SetPosition(position);

            List<AuthoringRoad> connected = new List<AuthoringRoad>();
            asset.GetRoadsAtIntersection(id, connected);
            for (int i = 0; i < connected.Count; i++)
            {
                AuthoringRoad road = connected[i];
                if (road.StartIntersectionId == id)
                {
                    road.KeyPoints[0].SetPosition(position);
                }
                if (road.EndIntersectionId == id)
                {
                    road.KeyPoints[road.KeyPoints.Count - 1].SetPosition(position);
                }
            }

            FinishRoads(connected);

            return true;
        }

        private void FinishRoads(List<AuthoringRoad> roads)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                FinishRoad(roads[i]);
            }
            asset.MarkChanged();
        }

        public int SplitRoad(int roadId, int segment, float t)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return 0;
            }
            if (segment < 0 || segment >= road.KeyPoints.Count - 1)
            {
                return 0;
            }

            Vector3 newStartOutHandle;
            Vector3 newEndInHandle;
            AuthoringKeyPoint splitPoint = curve.Split(road, segment, t, out newStartOutHandle, out newEndInHandle);

            AuthoringKeyPoint segmentStart = road.KeyPoints[segment];
            AuthoringKeyPoint segmentEnd = road.KeyPoints[segment + 1];
            segmentStart.SetOutHandle(newStartOutHandle);
            segmentStart.SetManualHandles(true);
            segmentEnd.SetInHandle(newEndInHandle);
            segmentEnd.SetManualHandles(true);

            int newIntersectionId = asset.NewIntersectionId();
            asset.Intersections.Add(new AuthoringIntersection(newIntersectionId, splitPoint.Position));

            AuthoringRoad secondHalf = new AuthoringRoad(asset.NewRoadId());
            secondHalf.SetTypeId(road.TypeId);
            secondHalf.SetOneWay(road.OneWay);
            secondHalf.SetSpeedOverride(road.SpeedOverride);
            secondHalf.SetWidthOverride(road.WidthOverride);
            secondHalf.SetSourceTag(road.SourceTag);
            secondHalf.SetStartIntersectionId(newIntersectionId);
            secondHalf.SetEndIntersectionId(road.EndIntersectionId);

            AuthoringKeyPoint secondHalfStart = new AuthoringKeyPoint(splitPoint.Position);
            secondHalfStart.SetOutHandle(splitPoint.OutHandle);
            secondHalfStart.SetManualHandles(true);
            secondHalf.KeyPoints.Add(secondHalfStart);
            for (int i = segment + 1; i < road.KeyPoints.Count; i++)
            {
                secondHalf.KeyPoints.Add(road.KeyPoints[i]);
            }

            int removeCount = road.KeyPoints.Count - (segment + 1);
            road.KeyPoints.RemoveRange(segment + 1, removeCount);

            AuthoringKeyPoint firstHalfEnd = new AuthoringKeyPoint(splitPoint.Position);
            firstHalfEnd.SetInHandle(splitPoint.InHandle);
            firstHalfEnd.SetManualHandles(true);
            road.KeyPoints.Add(firstHalfEnd);
            road.SetEndIntersectionId(newIntersectionId);

            asset.Roads.Add(secondHalf);

            List<AuthoringRoad> affected = new List<AuthoringRoad>();
            affected.Add(road);
            affected.Add(secondHalf);
            FinishRoads(affected);

            return newIntersectionId;
        }

        public bool MergeAtIntersection(int intersectionId)
        {
            List<AuthoringRoad> connected = new List<AuthoringRoad>();
            asset.GetRoadsAtIntersection(intersectionId, connected);
            if (connected.Count != 2)
            {
                return false;
            }

            AuthoringRoad roadA = connected[0];
            AuthoringRoad roadB = connected[1];

            if (roadA.TypeId != roadB.TypeId || roadA.OneWay != roadB.OneWay ||
                roadA.SpeedOverride != roadB.SpeedOverride || roadA.WidthOverride != roadB.WidthOverride ||
                roadA.SourceTag != roadB.SourceTag)
            {
                return false;
            }

            bool aEndsAtIntersection = roadA.EndIntersectionId == intersectionId;
            bool aStartsAtIntersection = roadA.StartIntersectionId == intersectionId;
            bool bEndsAtIntersection = roadB.EndIntersectionId == intersectionId;
            bool bStartsAtIntersection = roadB.StartIntersectionId == intersectionId;

            AuthoringRoad firstRoad;
            AuthoringRoad secondRoad;
            bool reverseFirst;
            bool reverseSecond;

            if (aEndsAtIntersection && bStartsAtIntersection)
            {
                firstRoad = roadA;
                secondRoad = roadB;
                reverseFirst = false;
                reverseSecond = false;
            }
            else if (bEndsAtIntersection && aStartsAtIntersection)
            {
                firstRoad = roadB;
                secondRoad = roadA;
                reverseFirst = false;
                reverseSecond = false;
            }
            else if (roadA.OneWay)
            {
                return false;
            }
            else if (aStartsAtIntersection && bStartsAtIntersection)
            {
                firstRoad = roadA;
                secondRoad = roadB;
                reverseFirst = true;
                reverseSecond = false;
            }
            else
            {
                firstRoad = roadA;
                secondRoad = roadB;
                reverseFirst = false;
                reverseSecond = true;
            }

            List<AuthoringKeyPoint> firstPoints;
            int newStartIntersectionId;
            if (reverseFirst)
            {
                firstPoints = ReverseKeyPoints(firstRoad.KeyPoints);
                newStartIntersectionId = firstRoad.EndIntersectionId;
            }
            else
            {
                firstPoints = firstRoad.KeyPoints;
                newStartIntersectionId = firstRoad.StartIntersectionId;
            }

            List<AuthoringKeyPoint> secondPoints;
            int newEndIntersectionId;
            if (reverseSecond)
            {
                secondPoints = ReverseKeyPoints(secondRoad.KeyPoints);
                newEndIntersectionId = secondRoad.StartIntersectionId;
            }
            else
            {
                secondPoints = secondRoad.KeyPoints;
                newEndIntersectionId = secondRoad.EndIntersectionId;
            }

            List<AuthoringKeyPoint> mergedPoints = new List<AuthoringKeyPoint>();
            for (int i = 0; i < firstPoints.Count; i++)
            {
                mergedPoints.Add(firstPoints[i]);
            }
            for (int i = 1; i < secondPoints.Count; i++)
            {
                mergedPoints.Add(secondPoints[i]);
            }

            AuthoringRoad surviving;
            AuthoringRoad removed;
            if (roadA.Id < roadB.Id)
            {
                surviving = roadA;
                removed = roadB;
            }
            else
            {
                surviving = roadB;
                removed = roadA;
            }

            surviving.KeyPoints.Clear();
            for (int i = 0; i < mergedPoints.Count; i++)
            {
                surviving.KeyPoints.Add(mergedPoints[i]);
            }
            surviving.SetStartIntersectionId(newStartIntersectionId);
            surviving.SetEndIntersectionId(newEndIntersectionId);

            asset.Roads.Remove(removed);
            AuthoringIntersection removedIntersection = asset.FindIntersection(intersectionId);
            if (removedIntersection != null)
            {
                asset.Intersections.Remove(removedIntersection);
            }

            FinishRoads(surviving);

            return true;
        }

        private List<AuthoringKeyPoint> ReverseKeyPoints(List<AuthoringKeyPoint> keyPoints)
        {
            List<AuthoringKeyPoint> reversed = new List<AuthoringKeyPoint>(keyPoints.Count);
            for (int i = keyPoints.Count - 1; i >= 0; i--)
            {
                AuthoringKeyPoint source = keyPoints[i];
                AuthoringKeyPoint copy = new AuthoringKeyPoint(source.Position);
                copy.SetInHandle(source.OutHandle);
                copy.SetOutHandle(source.InHandle);
                copy.SetManualHandles(source.ManualHandles);
                reversed.Add(copy);
            }
            return reversed;
        }

        public bool DeleteRoad(int roadId)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return false;
            }

            int startId = road.StartIntersectionId;
            int endId = road.EndIntersectionId;

            asset.Roads.Remove(road);

            RemoveIntersectionIfOrphan(startId);
            if (endId != startId)
            {
                RemoveIntersectionIfOrphan(endId);
            }

            asset.MarkChanged();

            return true;
        }

        private void RemoveIntersectionIfOrphan(int intersectionId)
        {
            List<AuthoringRoad> connected = new List<AuthoringRoad>();
            asset.GetRoadsAtIntersection(intersectionId, connected);
            if (connected.Count == 0)
            {
                AuthoringIntersection intersection = asset.FindIntersection(intersectionId);
                if (intersection != null)
                {
                    asset.Intersections.Remove(intersection);
                }
            }
        }

        public bool ConnectIntersections(int fromId, int intoId)
        {
            if (fromId == intoId)
            {
                return false;
            }

            AuthoringIntersection from = asset.FindIntersection(fromId);
            AuthoringIntersection into = asset.FindIntersection(intoId);
            if (from == null || into == null)
            {
                return false;
            }

            List<AuthoringRoad> connected = new List<AuthoringRoad>();
            asset.GetRoadsAtIntersection(fromId, connected);
            for (int i = 0; i < connected.Count; i++)
            {
                AuthoringRoad road = connected[i];
                if (road.StartIntersectionId == fromId)
                {
                    road.SetStartIntersectionId(intoId);
                    road.KeyPoints[0].SetPosition(into.Position);
                }
                if (road.EndIntersectionId == fromId)
                {
                    road.SetEndIntersectionId(intoId);
                    road.KeyPoints[road.KeyPoints.Count - 1].SetPosition(into.Position);
                }
            }

            asset.Intersections.Remove(from);

            FinishRoads(connected);

            return true;
        }

        public bool ConnectToRoadMiddle(int intersectionId, int roadId, int segment, float t)
        {
            AuthoringIntersection intersection = asset.FindIntersection(intersectionId);
            if (intersection == null)
            {
                return false;
            }

            int newIntersectionId = SplitRoad(roadId, segment, t);
            if (newIntersectionId == 0)
            {
                return false;
            }

            return ConnectIntersections(intersectionId, newIntersectionId);
        }

        public bool Disconnect(int intersectionId)
        {
            AuthoringIntersection intersection = asset.FindIntersection(intersectionId);
            if (intersection == null)
            {
                return false;
            }

            List<AuthoringRoad> connected = new List<AuthoringRoad>();
            asset.GetRoadsAtIntersection(intersectionId, connected);

            List<AuthoringRoad> affected = new List<AuthoringRoad>();
            for (int i = 1; i < connected.Count; i++)
            {
                AuthoringRoad road = connected[i];
                int newIntersectionId = asset.NewIntersectionId();
                asset.Intersections.Add(new AuthoringIntersection(newIntersectionId, intersection.Position));

                if (road.StartIntersectionId == intersectionId)
                {
                    road.SetStartIntersectionId(newIntersectionId);
                }
                if (road.EndIntersectionId == intersectionId)
                {
                    road.SetEndIntersectionId(newIntersectionId);
                }
                affected.Add(road);
            }

            FinishRoads(affected);

            return true;
        }

        public bool Flip(int roadId)
        {
            AuthoringRoad road = asset.FindRoad(roadId);
            if (road == null)
            {
                return false;
            }

            List<AuthoringKeyPoint> reversed = ReverseKeyPoints(road.KeyPoints);
            road.KeyPoints.Clear();
            for (int i = 0; i < reversed.Count; i++)
            {
                road.KeyPoints.Add(reversed[i]);
            }

            int oldStart = road.StartIntersectionId;
            road.SetStartIntersectionId(road.EndIntersectionId);
            road.SetEndIntersectionId(oldStart);

            FinishRoads(road);

            return true;
        }

        public bool SetProperties(List<int> roadIds, RoadBrush values, bool setType, bool setOneWay, bool setSpeed, bool setWidth)
        {
            List<AuthoringRoad> affected = new List<AuthoringRoad>();
            for (int i = 0; i < roadIds.Count; i++)
            {
                AuthoringRoad road = asset.FindRoad(roadIds[i]);
                if (road == null)
                {
                    continue;
                }

                if (setType)
                {
                    road.SetTypeId(values.TypeId);
                }
                if (setOneWay)
                {
                    road.SetOneWay(values.OneWay);
                }
                if (setSpeed)
                {
                    road.SetSpeedOverride(values.SpeedOverride);
                }
                if (setWidth)
                {
                    road.SetWidthOverride(values.WidthOverride);
                }

                affected.Add(road);
            }

            if (affected.Count == 0)
            {
                return false;
            }

            FinishRoads(affected);

            return true;
        }
    }
}
