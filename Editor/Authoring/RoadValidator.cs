using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadValidator
    {
        public const float DefaultNearMissDistance = 2f;
        public const float DefaultDuplicateTolerance = 1f;

        public void RunCheapChecks(RoadNetworkAuthoring asset, List<ValidationIssue> output)
        {
            CheckNearMisses(asset, output);
            CheckDuplicates(asset, output);
        }

        public void RunFullChecks(RoadNetworkAuthoring asset, MapData map, List<ValidationIssue> output)
        {
            RunCheapChecks(asset, output);
            CheckIslands(asset, output);
            CheckOneWayTraps(asset, output);
            CheckOutsideMap(asset, map, output);
            CheckGroundMisses(asset, output);
        }

        private void CheckNearMisses(RoadNetworkAuthoring asset, List<ValidationIssue> output)
        {
            List<AuthoringIntersection> intersections = asset.Intersections;
            List<AuthoringRoad> roads = asset.Roads;
            List<AuthoringRoad> connectedRoads = new List<AuthoringRoad>();

            for (int i = 0; i < intersections.Count; i++)
            {
                AuthoringIntersection intersection = intersections[i];
                asset.GetRoadsAtIntersection(intersection.Id, connectedRoads);
                if (connectedRoads.Count != 1)
                {
                    continue;
                }

                AuthoringRoad ownRoad = connectedRoads[0];
                int otherEndId;
                if (ownRoad.StartIntersectionId == intersection.Id)
                {
                    otherEndId = ownRoad.EndIntersectionId;
                }
                else
                {
                    otherEndId = ownRoad.StartIntersectionId;
                }

                bool reported = false;
                for (int j = 0; j < intersections.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    AuthoringIntersection other = intersections[j];
                    if (other.Id == otherEndId)
                    {
                        continue;
                    }

                    if (DistanceXZ(intersection.Position, other.Position) <= DefaultNearMissDistance)
                    {
                        string message = "Intersection " + intersection.Id + " is close to intersection " + other.Id + " but not connected.";
                        output.Add(new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.NearMiss, 0, intersection.Id, intersection.Position, message));
                        reported = true;
                        break;
                    }
                }

                if (reported)
                {
                    continue;
                }

                for (int r = 0; r < roads.Count; r++)
                {
                    AuthoringRoad road = roads[r];
                    if (road.Id == ownRoad.Id)
                    {
                        continue;
                    }

                    List<Vector3> points = road.Points;
                    bool foundOnRoad = false;
                    for (int p = 0; p < points.Count; p++)
                    {
                        if (DistanceXZ(intersection.Position, points[p]) <= DefaultNearMissDistance)
                        {
                            string message = "Intersection " + intersection.Id + " is close to road " + road.Id + " but not connected.";
                            output.Add(new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.NearMiss, road.Id, intersection.Id, intersection.Position, message));
                            foundOnRoad = true;
                            break;
                        }
                    }

                    if (foundOnRoad)
                    {
                        break;
                    }
                }
            }
        }

        private float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void CheckDuplicates(RoadNetworkAuthoring asset, List<ValidationIssue> output)
        {
            List<AuthoringRoad> roads = asset.Roads;
            Dictionary<long, List<AuthoringRoad>> groups = new Dictionary<long, List<AuthoringRoad>>();

            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                long key = MakePairKey(road.StartIntersectionId, road.EndIntersectionId);
                List<AuthoringRoad> group;
                if (!groups.TryGetValue(key, out group))
                {
                    group = new List<AuthoringRoad>();
                    groups[key] = group;
                }
                group.Add(road);
            }

            foreach (KeyValuePair<long, List<AuthoringRoad>> pair in groups)
            {
                List<AuthoringRoad> group = pair.Value;
                if (group.Count < 2)
                {
                    continue;
                }

                for (int a = 0; a < group.Count; a++)
                {
                    for (int b = a + 1; b < group.Count; b++)
                    {
                        AuthoringRoad roadA = group[a];
                        AuthoringRoad roadB = group[b];
                        if (AllPointsWithinTolerance(roadA.Points, roadB.Points) && AllPointsWithinTolerance(roadB.Points, roadA.Points))
                        {
                            string message = "Road " + roadB.Id + " duplicates road " + roadA.Id + ".";
                            output.Add(new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.Duplicate, roadB.Id, 0, roadB.Points[0], message));
                        }
                    }
                }
            }
        }

        private long MakePairKey(int idA, int idB)
        {
            int lower;
            int higher;
            if (idA < idB)
            {
                lower = idA;
                higher = idB;
            }
            else
            {
                lower = idB;
                higher = idA;
            }
            return ((long)lower << 32) | (uint)higher;
        }

        private bool AllPointsWithinTolerance(List<Vector3> from, List<Vector3> to)
        {
            for (int i = 0; i < from.Count; i++)
            {
                float minDistance = float.MaxValue;
                for (int j = 0; j < to.Count; j++)
                {
                    float distance = DistanceXZ(from[i], to[j]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                if (minDistance > DefaultDuplicateTolerance)
                {
                    return false;
                }
            }
            return true;
        }

        private void CheckIslands(RoadNetworkAuthoring asset, List<ValidationIssue> output)
        {
            Dictionary<int, int> idToIndex;
            int[] ids;
            BuildIntersectionIndex(asset, out idToIndex, out ids);
            if (ids.Length == 0)
            {
                return;
            }

            List<int>[] adjacency = BuildUndirectedAdjacency(asset, idToIndex, ids.Length);

            int[] componentOf;
            List<List<int>> components = FindConnectedComponents(ids.Length, adjacency, out componentOf);
            int largestIndex = FindLargestComponentIndex(components);

            for (int i = 0; i < components.Count; i++)
            {
                if (i == largestIndex)
                {
                    continue;
                }

                int firstNode = components[i][0];
                int intersectionId = ids[firstNode];
                Vector3 position = Vector3.zero;
                AuthoringIntersection intersection = asset.FindIntersection(intersectionId);
                if (intersection != null)
                {
                    position = intersection.Position;
                }

                string message = "Intersection " + intersectionId + " is part of a disconnected island.";
                output.Add(new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.Island, 0, intersectionId, position, message));
            }
        }

        private void BuildIntersectionIndex(RoadNetworkAuthoring asset, out Dictionary<int, int> idToIndex, out int[] ids)
        {
            List<AuthoringIntersection> intersections = asset.Intersections;
            idToIndex = new Dictionary<int, int>(intersections.Count);
            ids = new int[intersections.Count];
            for (int i = 0; i < intersections.Count; i++)
            {
                idToIndex[intersections[i].Id] = i;
                ids[i] = intersections[i].Id;
            }
        }

        private List<int>[] BuildUndirectedAdjacency(RoadNetworkAuthoring asset, Dictionary<int, int> idToIndex, int nodeCount)
        {
            List<int>[] adjacency = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                adjacency[i] = new List<int>();
            }

            List<AuthoringRoad> roads = asset.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                int startIndex;
                int endIndex;
                if (!idToIndex.TryGetValue(road.StartIntersectionId, out startIndex))
                {
                    continue;
                }
                if (!idToIndex.TryGetValue(road.EndIntersectionId, out endIndex))
                {
                    continue;
                }

                adjacency[startIndex].Add(endIndex);
                adjacency[endIndex].Add(startIndex);
            }

            return adjacency;
        }

        private List<List<int>> FindConnectedComponents(int nodeCount, List<int>[] adjacency, out int[] componentOf)
        {
            componentOf = new int[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                componentOf[i] = -1;
            }

            List<List<int>> components = new List<List<int>>();
            Queue<int> queue = new Queue<int>();

            for (int start = 0; start < nodeCount; start++)
            {
                if (componentOf[start] != -1)
                {
                    continue;
                }

                int componentIndex = components.Count;
                List<int> component = new List<int>();
                components.Add(component);

                componentOf[start] = componentIndex;
                component.Add(start);
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    int node = queue.Dequeue();
                    List<int> neighbors = adjacency[node];
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        int neighbor = neighbors[i];
                        if (componentOf[neighbor] == -1)
                        {
                            componentOf[neighbor] = componentIndex;
                            component.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return components;
        }

        private int FindLargestComponentIndex(List<List<int>> components)
        {
            int largestIndex = 0;
            int largestSize = 0;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Count > largestSize)
                {
                    largestSize = components[i].Count;
                    largestIndex = i;
                }
            }
            return largestIndex;
        }

        private void CheckOneWayTraps(RoadNetworkAuthoring asset, List<ValidationIssue> output)
        {
            Dictionary<int, int> idToIndex;
            int[] ids;
            BuildIntersectionIndex(asset, out idToIndex, out ids);
            if (ids.Length == 0)
            {
                return;
            }

            List<int>[] undirectedAdjacency = BuildUndirectedAdjacency(asset, idToIndex, ids.Length);
            int[] componentOf;
            List<List<int>> components = FindConnectedComponents(ids.Length, undirectedAdjacency, out componentOf);
            int largestComponentIndex = FindLargestComponentIndex(components);
            List<int> largestComponent = components[largestComponentIndex];

            List<int>[] directedAdjacency = BuildDirectedAdjacency(asset, idToIndex, ids.Length);
            List<List<int>> sccs = FindStronglyConnectedComponents(largestComponent, directedAdjacency);
            int largestSccIndex = FindLargestComponentIndex(sccs);

            int[] sccOfNode = new int[ids.Length];
            for (int i = 0; i < sccOfNode.Length; i++)
            {
                sccOfNode[i] = -1;
            }
            for (int i = 0; i < sccs.Count; i++)
            {
                List<int> scc = sccs[i];
                for (int n = 0; n < scc.Count; n++)
                {
                    sccOfNode[scc[n]] = i;
                }
            }

            bool[] hasOutgoing = new bool[sccs.Count];
            bool[] hasIncoming = new bool[sccs.Count];
            for (int n = 0; n < largestComponent.Count; n++)
            {
                int node = largestComponent[n];
                int fromScc = sccOfNode[node];
                List<int> neighbors = directedAdjacency[node];
                for (int e = 0; e < neighbors.Count; e++)
                {
                    int toScc = sccOfNode[neighbors[e]];
                    if (toScc != fromScc)
                    {
                        hasOutgoing[fromScc] = true;
                        hasIncoming[toScc] = true;
                    }
                }
            }

            for (int i = 0; i < sccs.Count; i++)
            {
                if (i == largestSccIndex)
                {
                    continue;
                }

                if (!hasOutgoing[i] || !hasIncoming[i])
                {
                    int firstNode = sccs[i][0];
                    int intersectionId = ids[firstNode];
                    Vector3 position = Vector3.zero;
                    AuthoringIntersection intersection = asset.FindIntersection(intersectionId);
                    if (intersection != null)
                    {
                        position = intersection.Position;
                    }

                    string message = "Intersection " + intersectionId + " is a one-way trap.";
                    output.Add(new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.OneWayTrap, 0, intersectionId, position, message));
                }
            }
        }

        private List<int>[] BuildDirectedAdjacency(RoadNetworkAuthoring asset, Dictionary<int, int> idToIndex, int nodeCount)
        {
            List<int>[] adjacency = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                adjacency[i] = new List<int>();
            }

            List<AuthoringRoad> roads = asset.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                int startIndex;
                int endIndex;
                if (!idToIndex.TryGetValue(road.StartIntersectionId, out startIndex))
                {
                    continue;
                }
                if (!idToIndex.TryGetValue(road.EndIntersectionId, out endIndex))
                {
                    continue;
                }

                adjacency[startIndex].Add(endIndex);
                if (!road.OneWay)
                {
                    adjacency[endIndex].Add(startIndex);
                }
            }

            return adjacency;
        }

        private List<List<int>> FindStronglyConnectedComponents(List<int> nodes, List<int>[] adjacency)
        {
            int nodeCount = adjacency.Length;
            int[] index = new int[nodeCount];
            int[] lowlink = new int[nodeCount];
            bool[] onStack = new bool[nodeCount];
            bool[] visited = new bool[nodeCount];
            Stack<int> tarjanStack = new Stack<int>();
            Stack<int> callStack = new Stack<int>();
            Stack<int> childIndexStack = new Stack<int>();
            List<List<int>> result = new List<List<int>>();
            int counter = 0;

            for (int n = 0; n < nodes.Count; n++)
            {
                int start = nodes[n];
                if (visited[start])
                {
                    continue;
                }

                visited[start] = true;
                index[start] = counter;
                lowlink[start] = counter;
                counter++;
                tarjanStack.Push(start);
                onStack[start] = true;
                callStack.Push(start);
                childIndexStack.Push(0);

                while (callStack.Count > 0)
                {
                    int node = callStack.Peek();
                    int childIndex = childIndexStack.Pop();
                    List<int> neighbors = adjacency[node];

                    if (childIndex < neighbors.Count)
                    {
                        childIndexStack.Push(childIndex + 1);
                        int neighbor = neighbors[childIndex];
                        if (!visited[neighbor])
                        {
                            visited[neighbor] = true;
                            index[neighbor] = counter;
                            lowlink[neighbor] = counter;
                            counter++;
                            tarjanStack.Push(neighbor);
                            onStack[neighbor] = true;
                            callStack.Push(neighbor);
                            childIndexStack.Push(0);
                        }
                        else if (onStack[neighbor])
                        {
                            if (index[neighbor] < lowlink[node])
                            {
                                lowlink[node] = index[neighbor];
                            }
                        }
                    }
                    else
                    {
                        callStack.Pop();

                        if (callStack.Count > 0)
                        {
                            int parent = callStack.Peek();
                            if (lowlink[node] < lowlink[parent])
                            {
                                lowlink[parent] = lowlink[node];
                            }
                        }

                        if (lowlink[node] == index[node])
                        {
                            List<int> component = new List<int>();
                            int member;
                            do
                            {
                                member = tarjanStack.Pop();
                                onStack[member] = false;
                                component.Add(member);
                            }
                            while (member != node);
                            result.Add(component);
                        }
                    }
                }
            }

            return result;
        }

        private void CheckOutsideMap(RoadNetworkAuthoring asset, MapData map, List<ValidationIssue> output)
        {
            if (map == null)
            {
                return;
            }

            MapFrame frame = map.CreateFrame();
            List<AuthoringRoad> roads = asset.Roads;

            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                List<Vector3> points = road.Points;
                for (int p = 0; p < points.Count; p++)
                {
                    Vector2 mapPosition = frame.TrueToMap(points[p]);
                    if (!frame.ContainsMap(mapPosition))
                    {
                        string message = "Road " + road.Id + " has a point outside the map area.";
                        output.Add(new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.OutsideMap, road.Id, 0, points[p], message));
                        break;
                    }
                }
            }
        }

        private void CheckGroundMisses(RoadNetworkAuthoring asset, List<ValidationIssue> output)
        {
            List<AuthoringRoad> roads = asset.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                if (road.GroundMissIndices.Count == 0)
                {
                    continue;
                }

                Vector3 position = road.Points[road.GroundMissIndices[0]];
                string message = "Road " + road.Id + " has " + road.GroundMissIndices.Count + " ground probe misses.";
                output.Add(new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.GroundMiss, road.Id, 0, position, message));
            }
        }
    }
}
