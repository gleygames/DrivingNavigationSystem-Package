using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal readonly struct RouteLineChunk
    {
        public float StartDistance { get; }
        public float EndDistance { get; }
        public int StartIndex { get; }
        public int Count { get; }

        public RouteLineChunk(int startIndex, int count, float startDistance, float endDistance)
        {
            StartDistance = startDistance;
            EndDistance = endDistance;
            StartIndex = startIndex;
            Count = count;
        }
    }

    internal class RouteLineChunker
    {
        public const int MaxVerticesPerChunk = 16000;
        public const int MaxPointsPerChunk = MaxVerticesPerChunk / 4;

        public void Split(List<Vector2> points, List<float> distances, List<bool> dashed, List<RouteLineChunk> outChunks)
        {
            outChunks.Clear();

            int pointCount = points.Count;
            if (pointCount < 2)
            {
                return;
            }

            int start = 0;
            while (start < pointCount - 1)
            {
                int remaining = pointCount - start;
                int count = remaining;
                if (count > MaxPointsPerChunk)
                {
                    count = MaxPointsPerChunk;
                }

                int endIndex = start + count - 1;
                outChunks.Add(new RouteLineChunk(start, count, distances[start], distances[endIndex]));

                if (endIndex >= pointCount - 1)
                {
                    break;
                }

                start = endIndex;
            }
        }
    }
}
