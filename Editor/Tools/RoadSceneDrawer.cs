using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadSceneDrawer
    {
        private const float RoadLineWidth = 3f;
        private const float KeyPointHandleSize = 0.3f;
        private const float DensePointHandleSize = 0.1f;
        private const float IntersectionRadius = 1f;
        private const float ValidationRadius = 1.5f;
        private const float ArrowSpacing = 20f;
        private const float ArrowSize = 1.5f;

        private readonly NavigationEditorPrefs prefs;
        private Vector3[] worldPointsBuffer = new Vector3[0];

        public RoadSceneDrawer(NavigationEditorPrefs prefs)
        {
            this.prefs = prefs;
        }

        public void Draw(RoadNetworkAuthoring authoring, MapData mapData, NavigationSettings settings, float unitsPerMeter, List<int> visibleRoadIds, List<bool> detailed, List<ValidationIssue> validationIssues)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            for (int i = 0; i < visibleRoadIds.Count; i++)
            {
                AuthoringRoad road = authoring.FindRoad(visibleRoadIds[i]);
                if (road == null)
                {
                    continue;
                }
                DrawRoad(road, settings, unitsPerMeter, detailed[i]);
            }

            if (prefs.ShowIntersections)
            {
                DrawIntersections(authoring, unitsPerMeter);
            }

            if (prefs.ShowValidationHighlights)
            {
                DrawValidationHighlights(validationIssues, unitsPerMeter);
            }

            if (prefs.ShowMapRectangle && mapData != null)
            {
                DrawMapRectangle(mapData, unitsPerMeter);
            }
        }

        private void DrawRoad(AuthoringRoad road, NavigationSettings settings, float unitsPerMeter, bool isDetailed)
        {
            if (prefs.ShowRoadLines)
            {
                DrawRoadLine(road, settings, unitsPerMeter, isDetailed);
            }

            if (!isDetailed)
            {
                return;
            }

            if (prefs.ShowDirectionArrows && road.OneWay)
            {
                DrawDirectionArrows(road, unitsPerMeter);
            }

            if (prefs.ShowKeyPoints)
            {
                DrawKeyPoints(road, unitsPerMeter);
            }

            if (prefs.ShowDenseShapePoints)
            {
                DrawDensePoints(road, unitsPerMeter);
            }
        }

        private void DrawRoadLine(AuthoringRoad road, NavigationSettings settings, float unitsPerMeter, bool isDetailed)
        {
            List<Vector3> points = road.Points;
            if (points.Count < 2)
            {
                return;
            }

            EnsureBufferSize(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                worldPointsBuffer[i] = ToWorld(points[i], unitsPerMeter);
            }

            Handles.color = ResolveRoadColor(road, settings, isDetailed);
            Handles.DrawAAPolyLine(RoadLineWidth, points.Count, worldPointsBuffer);
        }

        private void EnsureBufferSize(int count)
        {
            if (worldPointsBuffer.Length >= count)
            {
                return;
            }
            worldPointsBuffer = new Vector3[count];
        }

        private Vector3 ToWorld(Vector3 truePosition, float unitsPerMeter)
        {
            return truePosition * unitsPerMeter;
        }

        private Color ResolveRoadColor(AuthoringRoad road, NavigationSettings settings, bool isDetailed)
        {
            if (!isDetailed || !prefs.ShowRoadTypeColors || settings == null)
            {
                return Color.gray;
            }

            RoadType roadType = settings.FindRoadType(road.TypeId);
            if (roadType == null)
            {
                return Color.gray;
            }
            return roadType.EditorColor;
        }

        private void DrawDirectionArrows(AuthoringRoad road, float unitsPerMeter)
        {
            List<Vector3> points = road.Points;
            if (points.Count < 2)
            {
                return;
            }

            float distanceSinceArrow = ArrowSpacing;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 segmentStart = points[i - 1];
                Vector3 segmentEnd = points[i];
                distanceSinceArrow += Vector3.Distance(segmentStart, segmentEnd);
                if (distanceSinceArrow < ArrowSpacing)
                {
                    continue;
                }

                distanceSinceArrow = 0f;
                Vector3 midpoint = (segmentStart + segmentEnd) * 0.5f;
                Vector3 direction = (segmentEnd - segmentStart).normalized;
                DrawArrow(ToWorld(midpoint, unitsPerMeter), direction, unitsPerMeter);
            }
        }

        private void DrawArrow(Vector3 worldPosition, Vector3 trueDirection, float unitsPerMeter)
        {
            if (trueDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(trueDirection, Vector3.up);
            Handles.color = Color.white;
            Handles.ConeHandleCap(0, worldPosition, rotation, ArrowSize * unitsPerMeter, EventType.Repaint);
        }

        private void DrawKeyPoints(AuthoringRoad road, float unitsPerMeter)
        {
            List<AuthoringKeyPoint> keyPoints = road.KeyPoints;
            Handles.color = Color.cyan;
            for (int i = 0; i < keyPoints.Count; i++)
            {
                Vector3 worldPosition = ToWorld(keyPoints[i].Position, unitsPerMeter);
                float handleSize = HandleUtility.GetHandleSize(worldPosition) * KeyPointHandleSize;
                Handles.DotHandleCap(0, worldPosition, Quaternion.identity, handleSize, EventType.Repaint);
            }
        }

        private void DrawDensePoints(AuthoringRoad road, float unitsPerMeter)
        {
            List<Vector3> points = road.Points;
            Handles.color = Color.yellow;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 worldPosition = ToWorld(points[i], unitsPerMeter);
                float handleSize = HandleUtility.GetHandleSize(worldPosition) * DensePointHandleSize;
                Handles.DotHandleCap(0, worldPosition, Quaternion.identity, handleSize, EventType.Repaint);
            }
        }

        private void DrawIntersections(RoadNetworkAuthoring authoring, float unitsPerMeter)
        {
            List<AuthoringIntersection> intersections = authoring.Intersections;
            Handles.color = Color.green;
            for (int i = 0; i < intersections.Count; i++)
            {
                Vector3 worldPosition = ToWorld(intersections[i].Position, unitsPerMeter);
                Handles.DrawSolidDisc(worldPosition, Vector3.up, IntersectionRadius * unitsPerMeter);
            }
        }

        private void DrawValidationHighlights(List<ValidationIssue> validationIssues, float unitsPerMeter)
        {
            if (validationIssues == null)
            {
                return;
            }

            Handles.color = Color.red;
            for (int i = 0; i < validationIssues.Count; i++)
            {
                Vector3 worldPosition = ToWorld(validationIssues[i].Position, unitsPerMeter);
                Handles.DrawWireDisc(worldPosition, Vector3.up, ValidationRadius * unitsPerMeter);
            }
        }

        private void DrawMapRectangle(MapData mapData, float unitsPerMeter)
        {
            MapFrame frame = mapData.CreateFrame();
            Vector2 size = mapData.RectangleSize;
            float height = mapData.RectangleCenter.y;

            Vector3 bottomLeft = ToWorld(frame.MapToTrue(new Vector2(0f, 0f), height), unitsPerMeter);
            Vector3 bottomRight = ToWorld(frame.MapToTrue(new Vector2(size.x, 0f), height), unitsPerMeter);
            Vector3 topRight = ToWorld(frame.MapToTrue(new Vector2(size.x, size.y), height), unitsPerMeter);
            Vector3 topLeft = ToWorld(frame.MapToTrue(new Vector2(0f, size.y), height), unitsPerMeter);

            Handles.color = Color.white;
            Handles.DrawLine(bottomLeft, bottomRight);
            Handles.DrawLine(bottomRight, topRight);
            Handles.DrawLine(topRight, topLeft);
            Handles.DrawLine(topLeft, bottomLeft);
        }
    }
}
