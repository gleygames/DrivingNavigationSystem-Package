using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadSampler
    {
        private const int MaxDepth = 16;

        private readonly List<SubdivisionRange> stack;
        private readonly List<float> tBuffer;
        private readonly RoadCurve curve;

        public RoadSampler()
        {
            stack = new List<SubdivisionRange>();
            tBuffer = new List<float>();
            curve = new RoadCurve();
        }

        public void Sample(AuthoringRoad road, IGroundProbe probe, float maxDeviation, float maxSpacing)
        {
            road.Points.Clear();
            road.GroundMissIndices.Clear();

            int segmentCount = road.KeyPoints.Count - 1;
            for (int segment = 0; segment < segmentCount; segment++)
            {
                SampleSegment(road, segment, probe, maxDeviation, maxSpacing);
            }
        }

        private void SampleSegment(AuthoringRoad road, int segment, IGroundProbe probe, float maxDeviation, float maxSpacing)
        {
            tBuffer.Clear();
            tBuffer.Add(0f);

            stack.Clear();
            stack.Add(new SubdivisionRange(0f, 1f, 0));

            while (stack.Count > 0)
            {
                SubdivisionRange range = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);

                bool needsSplit = false;
                if (range.Depth < MaxDepth)
                {
                    needsSplit = NeedsSubdivision(road, segment, range.T0, range.T1, maxDeviation, maxSpacing);
                }

                if (!needsSplit)
                {
                    tBuffer.Add(range.T1);
                    continue;
                }

                float mid = (range.T0 + range.T1) * 0.5f;
                stack.Add(new SubdivisionRange(mid, range.T1, range.Depth + 1));
                stack.Add(new SubdivisionRange(range.T0, mid, range.Depth + 1));
            }

            int startIndex = 0;
            if (segment > 0)
            {
                startIndex = 1;
            }

            for (int i = startIndex; i < tBuffer.Count; i++)
            {
                AddPoint(road, segment, tBuffer[i], probe);
            }
        }

        private bool NeedsSubdivision(AuthoringRoad road, int segment, float t0, float t1, float maxDeviation, float maxSpacing)
        {
            Vector3 start = curve.Evaluate(road, segment, t0);
            Vector3 end = curve.Evaluate(road, segment, t1);
            float mid = (t0 + t1) * 0.5f;
            Vector3 curveMid = curve.Evaluate(road, segment, mid);
            Vector3 chordMid = (start + end) * 0.5f;

            Vector2 curveMidXZ = new Vector2(curveMid.x, curveMid.z);
            Vector2 chordMidXZ = new Vector2(chordMid.x, chordMid.z);
            float deviation = Vector2.Distance(curveMidXZ, chordMidXZ);
            if (deviation > maxDeviation)
            {
                return true;
            }

            Vector2 startXZ = new Vector2(start.x, start.z);
            Vector2 endXZ = new Vector2(end.x, end.z);
            float chordLength = Vector2.Distance(startXZ, endXZ);
            if (chordLength > maxSpacing)
            {
                return true;
            }

            return false;
        }

        private void AddPoint(AuthoringRoad road, int segment, float t, IGroundProbe probe)
        {
            Vector3 point = curve.Evaluate(road, segment, t);
            float trueY;
            bool didHit = probe.Probe(point, out trueY);
            if (didHit)
            {
                point.y = trueY;
            }
            else
            {
                road.GroundMissIndices.Add(road.Points.Count);
            }
            road.Points.Add(point);
        }

        private readonly struct SubdivisionRange
        {
            public SubdivisionRange(float t0, float t1, int depth)
            {
                T0 = t0;
                T1 = t1;
                Depth = depth;
            }

            public float T0 { get; }
            public float T1 { get; }
            public int Depth { get; }
        }
    }
}
