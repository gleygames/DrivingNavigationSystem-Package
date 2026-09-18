using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    internal class RouteLineMeshBuilder
    {
        private const float MinPointDistance = 0.0001f;
        private const float MiterLimit = 2f;
        private const float ZeroSumThreshold = 0.000001f;

        private readonly List<Vector2> _keptPoints;
        private readonly List<Vector2> _segmentNormals;
        private readonly List<Vector2> _groupPositions;
        private readonly List<Vector2> _groupOffsets;
        private readonly List<float> _keptDistances;
        private readonly List<float> _groupDistances;
        private readonly List<bool> _keptDashed;
        private readonly List<bool> _groupDashed;

        public RouteLineMeshBuilder()
        {
            _keptPoints = new List<Vector2>();
            _segmentNormals = new List<Vector2>();
            _groupPositions = new List<Vector2>();
            _groupOffsets = new List<Vector2>();
            _keptDistances = new List<float>();
            _groupDistances = new List<float>();
            _keptDashed = new List<bool>();
            _groupDashed = new List<bool>();
        }

        public void Build(List<Vector2> points, List<float> distances, List<bool> dashed, List<UIVertex> outVertices, List<int> outIndices)
        {
            outVertices.Clear();
            outIndices.Clear();

            BuildKeptPoints(points, distances, dashed);

            if (_keptPoints.Count < 2)
            {
                return;
            }

            BuildSegmentNormals();
            BuildGroups();
            BuildVerticesAndIndices(outVertices, outIndices);
        }

        private void BuildKeptPoints(List<Vector2> points, List<float> distances, List<bool> dashed)
        {
            _keptPoints.Clear();
            _keptDistances.Clear();
            _keptDashed.Clear();

            if (points.Count == 0)
            {
                return;
            }

            _keptPoints.Add(points[0]);
            _keptDistances.Add(distances[0]);

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 lastKept = _keptPoints[_keptPoints.Count - 1];
                float distanceToLast = Vector2.Distance(points[i], lastKept);
                if (distanceToLast < MinPointDistance)
                {
                    continue;
                }

                _keptPoints.Add(points[i]);
                _keptDistances.Add(distances[i]);
                _keptDashed.Add(dashed[i - 1]);
            }
        }

        private void BuildSegmentNormals()
        {
            _segmentNormals.Clear();

            for (int i = 0; i < _keptPoints.Count - 1; i++)
            {
                Vector2 direction = (_keptPoints[i + 1] - _keptPoints[i]).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                _segmentNormals.Add(normal);
            }
        }

        private void BuildGroups()
        {
            _groupPositions.Clear();
            _groupOffsets.Clear();
            _groupDashed.Clear();
            _groupDistances.Clear();

            int lastIndex = _keptPoints.Count - 1;

            AddGroup(_keptPoints[0], _segmentNormals[0], _keptDashed[0], _keptDistances[0]);

            for (int j = 1; j < lastIndex; j++)
            {
                BuildInnerGroups(j);
            }

            AddGroup(_keptPoints[lastIndex], _segmentNormals[lastIndex - 1], _keptDashed[lastIndex - 1], _keptDistances[lastIndex]);
        }

        private void AddGroup(Vector2 position, Vector2 offset, bool dashedFlag, float distance)
        {
            _groupPositions.Add(position);
            _groupOffsets.Add(offset);
            _groupDashed.Add(dashedFlag);
            _groupDistances.Add(distance);
        }

        private void BuildInnerGroups(int j)
        {
            Vector2 n0 = _segmentNormals[j - 1];
            Vector2 n1 = _segmentNormals[j];
            bool flagBefore = _keptDashed[j - 1];
            bool flagAfter = _keptDashed[j];
            Vector2 position = _keptPoints[j];
            float distance = _keptDistances[j];

            Vector2 sum = n0 + n1;

            if (sum.sqrMagnitude >= ZeroSumThreshold)
            {
                Vector2 miter = sum.normalized;
                float dot = Vector2.Dot(miter, n0);
                float scale = 1f / dot;
                if (scale <= MiterLimit)
                {
                    AddMiterGroup(position, miter * scale, flagBefore, flagAfter, distance);
                    return;
                }
            }

            AddGroup(position, n0, flagBefore, distance);
            AddGroup(position, n1, flagAfter, distance);
        }

        private void AddMiterGroup(Vector2 position, Vector2 offset, bool flagBefore, bool flagAfter, float distance)
        {
            AddGroup(position, offset, flagBefore, distance);
            if (flagAfter != flagBefore)
            {
                AddGroup(position, offset, flagAfter, distance);
            }
        }

        private void BuildVerticesAndIndices(List<UIVertex> outVertices, List<int> outIndices)
        {
            for (int i = 0; i < _groupPositions.Count; i++)
            {
                AddVertexPair(outVertices, _groupPositions[i], _groupOffsets[i], _groupDashed[i], _groupDistances[i]);
            }

            for (int i = 0; i < _groupPositions.Count - 1; i++)
            {
                int l0 = i * 2;
                int r0 = i * 2 + 1;
                int l1 = (i + 1) * 2;
                int r1 = (i + 1) * 2 + 1;

                outIndices.Add(l0);
                outIndices.Add(r0);
                outIndices.Add(l1);

                outIndices.Add(r0);
                outIndices.Add(r1);
                outIndices.Add(l1);
            }
        }

        private void AddVertexPair(List<UIVertex> outVertices, Vector2 position, Vector2 offset, bool dashedFlag, float distance)
        {
            float dashedValue = 0f;
            if (dashedFlag)
            {
                dashedValue = 1f;
            }

            UIVertex left = new UIVertex();
            left.position = new Vector3(position.x, position.y, 0f);
            left.color = Color.white;
            left.uv0 = new Vector2(distance, -1f);
            left.uv1 = offset;
            left.uv2 = new Vector2(dashedValue, 0f);
            outVertices.Add(left);

            UIVertex right = new UIVertex();
            right.position = new Vector3(position.x, position.y, 0f);
            right.color = Color.white;
            right.uv0 = new Vector2(distance, 1f);
            right.uv1 = -offset;
            right.uv2 = new Vector2(dashedValue, 0f);
            outVertices.Add(right);
        }
    }
}
