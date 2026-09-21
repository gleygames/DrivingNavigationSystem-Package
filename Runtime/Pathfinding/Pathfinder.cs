using System;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class Pathfinder
    {
        private const int NoState = -1;
        private const int MinHeapCapacity = 16;

        private readonly float[] costPerMeter;
        private readonly float[] stateCost;
        private readonly int[] parent;
        private readonly int[] visitStamp;
        private readonly int[] closedStamp;
        private readonly int[] pathBuffer;
        private readonly RoadNetworkData network;

        private float[] heapKeys;
        private float[] heapCosts;
        private int[] heapStates;
        private Vector3 destinationPoint;
        private UTurnRule uTurn;
        private float minCostPerMeter;
        private float destinationDistance;
        private float bestCost;
        private int destinationRoad;
        private int bestState;
        private int bestDirection;
        private int stamp;
        private int heapCount;
        private bool hasCandidate;

        public Pathfinder(RoadNetworkData network)
        {
            this.network = network;

            int roadCount = network.RoadCount;
            int stateCount = roadCount * 2;

            costPerMeter = new float[roadCount];
            stateCost = new float[stateCount];
            parent = new int[stateCount];
            visitStamp = new int[stateCount];
            closedStamp = new int[stateCount];
            pathBuffer = new int[stateCount];

            int heapCapacity = Math.Max(roadCount * 4, MinHeapCapacity);
            heapKeys = new float[heapCapacity];
            heapCosts = new float[heapCapacity];
            heapStates = new int[heapCapacity];
        }

        public void FindRouteBetweenIntersections(int from, int to, RoutePreferences preferences, Route result)
        {
            result.Clear();
            result.Network = network;
            result.Destination = network.GetIntersection(to).Position;

            if (!SetIntersectionDestination(to))
            {
                result.Failure = FailureReason.NoPath;
                return;
            }

            BeginSearch(preferences);
            AddIntersectionStartStates(from);
            EvaluateFinish(from, NoState, 0f);
            RunSearch();
            WriteResult(result);
        }

        public void FindRouteFromRoad(int startRoad, bool forward, int toIntersection, RoutePreferences preferences, Route result)
        {
            result.Clear();
            result.Network = network;
            result.Destination = network.GetIntersection(toIntersection).Position;

            if (!SetIntersectionDestination(toIntersection))
            {
                result.Failure = FailureReason.NoPath;
                return;
            }

            BeginSearch(preferences);

            int direction = 1;
            if (forward)
            {
                direction = 0;
            }
            RoadRecord road = network.GetRoad(startRoad);
            AddStartState(startRoad * 2 + direction, road.Length * costPerMeter[startRoad]);

            RunSearch();
            WriteResult(result);
        }

        private bool SetIntersectionDestination(int intersection)
        {
            IntersectionRecord record = network.GetIntersection(intersection);
            if (record.LinkCount == 0)
            {
                return false;
            }

            destinationRoad = network.GetLink(record.FirstLink);
            RoadRecord road = network.GetRoad(destinationRoad);
            if (road.StartIntersection == intersection)
            {
                destinationDistance = 0f;
            }
            else
            {
                destinationDistance = road.Length;
            }
            destinationPoint = record.Position;
            return true;
        }

        private void BeginSearch(RoutePreferences preferences)
        {
            stamp++;
            heapCount = 0;
            hasCandidate = false;
            bestCost = float.PositiveInfinity;
            bestState = NoState;
            bestDirection = 0;
            uTurn = preferences.UTurn;

            bool fastest = preferences.Mode == RouteMode.Fastest;
            for (int i = 0; i < costPerMeter.Length; i++)
            {
                RoadRecord road = network.GetRoad(i);
                float multiplier = preferences.GetMultiplier(road.TypeId);
                if (fastest)
                {
                    costPerMeter[i] = multiplier / road.Speed;
                }
                else
                {
                    costPerMeter[i] = multiplier;
                }
            }

            float smallestMultiplier = preferences.GetSmallestMultiplier();
            if (fastest)
            {
                if (network.MaxSpeed > 0f)
                {
                    minCostPerMeter = smallestMultiplier / network.MaxSpeed;
                }
                else
                {
                    minCostPerMeter = 0f;
                }
            }
            else
            {
                minCostPerMeter = smallestMultiplier;
            }
        }

        private void AddIntersectionStartStates(int intersection)
        {
            IntersectionRecord record = network.GetIntersection(intersection);
            int lastLink = record.FirstLink + record.LinkCount;
            for (int l = record.FirstLink; l < lastLink; l++)
            {
                int roadIndex = network.GetLink(l);
                RoadRecord road = network.GetRoad(roadIndex);
                float cost = road.Length * costPerMeter[roadIndex];
                if (road.StartIntersection == intersection)
                {
                    AddStartState(roadIndex * 2, cost);
                }
                if (road.EndIntersection == intersection && !road.OneWay)
                {
                    AddStartState(roadIndex * 2 + 1, cost);
                }
            }
        }

        private void AddStartState(int state, float cost)
        {
            Relax(state, cost, NoState);
        }

        private void Relax(int state, float cost, int fromState)
        {
            if (closedStamp[state] == stamp)
            {
                return;
            }
            if (visitStamp[state] == stamp && cost >= stateCost[state])
            {
                return;
            }

            visitStamp[state] = stamp;
            stateCost[state] = cost;
            parent[state] = fromState;
            Push(state, cost + Heuristic(state), cost);
        }

        private float Heuristic(int state)
        {
            Vector3 position = network.GetIntersection(GetEndIntersection(state)).Position;
            float deltaX = destinationPoint.x - position.x;
            float deltaZ = destinationPoint.z - position.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ) * minCostPerMeter;
        }

        private int GetEndIntersection(int state)
        {
            RoadRecord road = network.GetRoad(state / 2);
            if (state % 2 == 0)
            {
                return road.EndIntersection;
            }
            return road.StartIntersection;
        }

        private void Push(int state, float key, float cost)
        {
            if (heapCount == heapStates.Length)
            {
                GrowHeap();
            }

            int index = heapCount;
            heapCount++;
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (heapKeys[parentIndex] <= key)
                {
                    break;
                }
                heapKeys[index] = heapKeys[parentIndex];
                heapCosts[index] = heapCosts[parentIndex];
                heapStates[index] = heapStates[parentIndex];
                index = parentIndex;
            }
            heapKeys[index] = key;
            heapCosts[index] = cost;
            heapStates[index] = state;
        }

        private void GrowHeap()
        {
            int capacity = heapStates.Length * 2;

            float[] newKeys = new float[capacity];
            float[] newCosts = new float[capacity];
            int[] newStates = new int[capacity];
            Array.Copy(heapKeys, newKeys, heapCount);
            Array.Copy(heapCosts, newCosts, heapCount);
            Array.Copy(heapStates, newStates, heapCount);

            heapKeys = newKeys;
            heapCosts = newCosts;
            heapStates = newStates;
        }

        private void EvaluateFinish(int intersection, int state, float cost)
        {
            RoadRecord road = network.GetRoad(destinationRoad);
            bool deadEnd = network.IsDeadEnd(intersection);
            if (road.StartIntersection == intersection)
            {
                TryFinish(state, 0, destinationDistance, cost, road.OneWay, deadEnd);
            }
            if (road.EndIntersection == intersection)
            {
                TryFinish(state, 1, road.Length - destinationDistance, cost, road.OneWay, deadEnd);
            }
        }

        private void TryFinish(int state, int direction, float partialLength, float cost, bool oneWay, bool deadEnd)
        {
            if (partialLength > 0f)
            {
                if (direction == 1 && oneWay)
                {
                    return;
                }
                if (state != NoState && IsUTurn(state, destinationRoad, direction) && !IsUTurnAllowed(deadEnd))
                {
                    return;
                }
            }

            float total = cost + partialLength * costPerMeter[destinationRoad];
            if (total < bestCost)
            {
                bestCost = total;
                bestState = state;
                bestDirection = direction;
                hasCandidate = true;
            }
        }

        private bool IsUTurn(int state, int nextRoad, int nextDirection)
        {
            return state / 2 == nextRoad && state % 2 != nextDirection;
        }

        private bool IsUTurnAllowed(bool deadEnd)
        {
            return uTurn != UTurnRule.Never || deadEnd;
        }

        private void RunSearch()
        {
            while (heapCount > 0)
            {
                if (heapKeys[0] >= bestCost)
                {
                    break;
                }

                int state;
                float cost;
                Pop(out state, out cost);

                if (closedStamp[state] == stamp)
                {
                    continue;
                }
                if (cost > stateCost[state])
                {
                    continue;
                }

                closedStamp[state] = stamp;
                int intersection = GetEndIntersection(state);
                EvaluateFinish(intersection, state, cost);
                ExpandState(state, intersection, cost);
            }
        }

        private void Pop(out int state, out float cost)
        {
            state = heapStates[0];
            cost = heapCosts[0];

            heapCount--;
            if (heapCount == 0)
            {
                return;
            }

            float key = heapKeys[heapCount];
            float lastCost = heapCosts[heapCount];
            int lastState = heapStates[heapCount];

            int index = 0;
            while (true)
            {
                int child = index * 2 + 1;
                if (child >= heapCount)
                {
                    break;
                }
                if (child + 1 < heapCount && heapKeys[child + 1] < heapKeys[child])
                {
                    child++;
                }
                if (heapKeys[child] >= key)
                {
                    break;
                }
                heapKeys[index] = heapKeys[child];
                heapCosts[index] = heapCosts[child];
                heapStates[index] = heapStates[child];
                index = child;
            }
            heapKeys[index] = key;
            heapCosts[index] = lastCost;
            heapStates[index] = lastState;
        }

        private void ExpandState(int state, int intersection, float cost)
        {
            IntersectionRecord record = network.GetIntersection(intersection);
            bool deadEnd = record.LinkCount == 1;
            int lastLink = record.FirstLink + record.LinkCount;
            for (int l = record.FirstLink; l < lastLink; l++)
            {
                int nextRoad = network.GetLink(l);
                RoadRecord road = network.GetRoad(nextRoad);
                float nextCost = cost + road.Length * costPerMeter[nextRoad];
                if (road.StartIntersection == intersection)
                {
                    TryEnter(state, nextRoad, 0, nextCost, deadEnd);
                }
                if (road.EndIntersection == intersection && !road.OneWay)
                {
                    TryEnter(state, nextRoad, 1, nextCost, deadEnd);
                }
            }
        }

        private void TryEnter(int state, int nextRoad, int nextDirection, float nextCost, bool deadEnd)
        {
            if (IsUTurn(state, nextRoad, nextDirection) && !IsUTurnAllowed(deadEnd))
            {
                return;
            }
            Relax(nextRoad * 2 + nextDirection, nextCost, state);
        }

        private void WriteResult(Route result)
        {
            if (!hasCandidate)
            {
                result.Success = false;
                result.Failure = FailureReason.NoPath;
                return;
            }

            int count = 0;
            int state = bestState;
            while (state != NoState)
            {
                pathBuffer[count] = state;
                count++;
                state = parent[state];
            }

            for (int i = count - 1; i >= 0; i--)
            {
                int pathState = pathBuffer[i];
                int roadIndex = pathState / 2;
                RoadRecord road = network.GetRoad(roadIndex);
                if (pathState % 2 == 0)
                {
                    AppendSegment(result, roadIndex, true, 0f, road.Length);
                }
                else
                {
                    AppendSegment(result, roadIndex, false, road.Length, 0f);
                }
            }

            RoadRecord destination = network.GetRoad(destinationRoad);
            if (bestDirection == 0)
            {
                if (destinationDistance > 0f)
                {
                    AppendSegment(result, destinationRoad, true, 0f, destinationDistance);
                }
            }
            else
            {
                if (destination.Length - destinationDistance > 0f)
                {
                    AppendSegment(result, destinationRoad, false, destination.Length, destinationDistance);
                }
            }

            result.Success = true;
            result.Failure = FailureReason.None;
        }

        private void AppendSegment(Route result, int roadIndex, bool forward, float fromDistance, float toDistance)
        {
            RoadRecord road = network.GetRoad(roadIndex);
            result.AddSegment(new RouteSegment(roadIndex, road.Id, forward, fromDistance, toDistance));

            float pieceLength = Mathf.Abs(toDistance - fromDistance);
            result.Length += pieceLength;
            if (road.Speed > 0f)
            {
                result.Eta += pieceLength / road.Speed;
            }
        }
    }
}
