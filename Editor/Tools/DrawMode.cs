using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class DrawMode : IRoadEditorMode
    {
        private const float ResolveTolerance = 0.05f;
        private const float MinKeyPointSpacing = 0.01f;
        private const float SnapHighlightSize = 0.2f;
        private const float HoverDotSize = 0.06f;
        private const float KeyPointDotSize = 0.08f;
        private const float PreviewLineWidth = 4f;
        private const float DottedLineSpacing = 4f;
        private const float RightClickMaxDrag = 4f;
        private const int PreviewStepsPerSegment = 16;

        private readonly List<Vector3> keyPoints;
        private readonly List<AuthoringRoad> connectedRoads;
        private readonly RoadEditorContext context;
        private readonly SnapFinder snapFinder;
        private readonly RoadValidator validator;
        private readonly RoadCurve curve;
        private readonly AuthoringRoad previewRoad;
        private readonly WorldConverter converter;

        private Vector3[] previewBuffer = new Vector3[0];
        private SnapTarget hoverSnap;
        private Vector2 lastMousePosition;
        private Vector2 rightMouseDownPosition;
        private Vector3 hoverPosition;
        private int extendRoadId;
        private bool hasHover;
        private bool startSnapped;
        private bool endSnapped;
        private bool isExtending;
        private bool extendAtEnd;
        private bool isRightMouseDown;

        private bool IsDrawing
        {
            get { return keyPoints.Count > 0 || isExtending; }
        }

        public DrawMode(RoadEditorContext context)
        {
            this.context = context;
            keyPoints = new List<Vector3>();
            connectedRoads = new List<AuthoringRoad>();
            snapFinder = new SnapFinder();
            validator = new RoadValidator();
            curve = new RoadCurve();
            previewRoad = new AuthoringRoad(0);
            converter = new WorldConverter();
        }

        public void OnEnter()
        {
            ResetState();
        }

        public void OnExit()
        {
            ResetState();
        }

        public void OnWindowGUI()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            if (authoring == null)
            {
                EditorGUILayout.HelpBox("This map has no road network asset.", MessageType.Info);
                return;
            }

            NavigationEditorPrefs prefs = context.Prefs;

            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int typeId = DrawBrushTypePopup(authoring.Settings, prefs.BrushTypeId);
            bool oneWay = EditorGUILayout.Toggle("One-way", prefs.BrushOneWay);
            float speedOverride = EditorGUILayout.FloatField("Speed Override (m/s, 0 = type)", prefs.BrushSpeedOverride);
            float widthOverride = EditorGUILayout.FloatField("Width Override (m, 0 = type)", prefs.BrushWidthOverride);

            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            LayerMask roadLayers = DrawLayerMaskField("Road Layers", prefs.RoadLayers);
            float snapDistance = EditorGUILayout.FloatField("Snap Distance (m)", prefs.SnapDistance);

            if (EditorGUI.EndChangeCheck())
            {
                prefs.BrushTypeId = typeId;
                prefs.BrushOneWay = oneWay;
                prefs.BrushSpeedOverride = Mathf.Max(0f, speedOverride);
                prefs.BrushWidthOverride = Mathf.Max(0f, widthOverride);
                prefs.RoadLayers = roadLayers;
                prefs.SnapDistance = Mathf.Max(0f, snapDistance);
                SceneView.RepaintAll();
            }

            DrawStatus();
            EditorGUILayout.HelpBox("Click: add key point (ends snap to intersections and roads).\nShift: no snapping.\nRight-click: remove last key point.\nEnter or double-click: finish. Esc: cancel.\nClick a dead end first to extend its road.", MessageType.None);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (context.Authoring == null)
            {
                return;
            }

            float unitsPerMeter = context.UnitsPerMeter;
            if (unitsPerMeter > 0f)
            {
                converter.SetUnitsPerMeter(unitsPerMeter);
            }

            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            HandleRightClick(current, sceneView);

            switch (current.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
                case EventType.MouseMove:
                    UpdateHover(current.mousePosition, current.shift);
                    sceneView.Repaint();
                    break;
                case EventType.MouseDown:
                    HandleMouseDown(current, controlId);
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;
                case EventType.ContextClick:
                    if (IsDrawing)
                    {
                        current.Use();
                    }
                    break;
                case EventType.KeyDown:
                    HandleKeyDown(current, sceneView);
                    break;
                case EventType.KeyUp:
                    if (current.keyCode == KeyCode.LeftShift || current.keyCode == KeyCode.RightShift)
                    {
                        UpdateHover(lastMousePosition, false);
                        sceneView.Repaint();
                    }
                    break;
                case EventType.Repaint:
                    DrawModeVisuals();
                    break;
            }
        }

        private void HandleRightClick(Event current, SceneView sceneView)
        {
            if (current.button != 1)
            {
                return;
            }

            if (current.rawType == EventType.MouseDown)
            {
                rightMouseDownPosition = current.mousePosition;
                isRightMouseDown = true;
                return;
            }

            if (current.rawType != EventType.MouseUp || !isRightMouseDown)
            {
                return;
            }

            isRightMouseDown = false;
            if ((current.mousePosition - rightMouseDownPosition).sqrMagnitude > RightClickMaxDrag * RightClickMaxDrag)
            {
                return;
            }
            if (!IsDrawing)
            {
                return;
            }

            RemoveLastKeyPoint();
            sceneView.Repaint();
            HandleUtility.Repaint();
        }

        private int DrawBrushTypePopup(NavigationSettings settings, int typeId)
        {
            if (settings == null || settings.RoadTypes.Count == 0)
            {
                EditorGUILayout.LabelField("Type", "No road types");
                return typeId;
            }

            IReadOnlyList<RoadType> roadTypes = settings.RoadTypes;
            string[] names = new string[roadTypes.Count];
            int selectedIndex = 0;
            for (int i = 0; i < roadTypes.Count; i++)
            {
                names[i] = roadTypes[i].Name;
                if (roadTypes[i].Id == typeId)
                {
                    selectedIndex = i;
                }
            }

            int newIndex = EditorGUILayout.Popup("Type", selectedIndex, names);
            return roadTypes[newIndex].Id;
        }

        private LayerMask DrawLayerMaskField(string label, LayerMask mask)
        {
            int concatenated = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask);
            concatenated = EditorGUILayout.MaskField(label, concatenated, InternalEditorUtility.layers);
            return InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(concatenated);
        }

        private void DrawStatus()
        {
            if (isExtending)
            {
                EditorGUILayout.LabelField("Status", "Extending road " + extendRoadId);
                return;
            }

            if (keyPoints.Count > 0)
            {
                EditorGUILayout.LabelField("Status", "Drawing, " + keyPoints.Count + " key points");
                return;
            }

            EditorGUILayout.LabelField("Status", "Idle");
        }

        private void UpdateHover(Vector2 mousePosition, bool disableSnap)
        {
            lastMousePosition = mousePosition;
            hoverSnap = new SnapTarget();

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            RaycastHit hit;
            hasHover = Physics.Raycast(ray, out hit, Mathf.Infinity, context.Prefs.RoadLayers, QueryTriggerInteraction.Ignore);
            if (!hasHover)
            {
                return;
            }

            hoverPosition = converter.WorldToTrue(hit.point);
            if (disableSnap)
            {
                return;
            }

            SnapTarget target;
            if (snapFinder.FindSnap(hoverPosition, context.Prefs.SnapDistance, context.Authoring, GetIgnoreRoadId(), out target))
            {
                hoverSnap = target;
            }
        }

        private int GetIgnoreRoadId()
        {
            if (isExtending)
            {
                return extendRoadId;
            }
            return 0;
        }

        private void HandleMouseDown(Event current, int controlId)
        {
            if (current.button != 0 || current.alt)
            {
                return;
            }
            if (HandleUtility.nearestControl != controlId)
            {
                return;
            }

            Vector2 mousePosition = current.mousePosition;
            bool disableSnap = current.shift;
            int clickCount = current.clickCount;

            GUIUtility.hotControl = controlId;
            current.Use();

            if (clickCount >= 2)
            {
                Finish();
                return;
            }

            UpdateHover(mousePosition, disableSnap);
            if (!hasHover)
            {
                return;
            }

            HandleClick();
        }

        private void Finish()
        {
            if (keyPoints.Count >= 2)
            {
                CommitRoad();
                return;
            }

            ResetState();
            SceneView.RepaintAll();
        }

        private void CommitRoad()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            Vector3 startPosition = keyPoints[0];
            Vector3 endPosition = keyPoints[keyPoints.Count - 1];

            Undo.RecordObject(authoring, "Draw Road");
            RoadEditOperations operations = CreateOperations();
            int roadId = operations.CreateRoad(keyPoints, CreateBrush(), 0, 0);
            if (roadId != 0)
            {
                AuthoringRoad road = authoring.FindRoad(roadId);
                if (startSnapped)
                {
                    ConnectToTarget(operations, road.StartIntersectionId, startPosition, roadId);
                }
                if (endSnapped)
                {
                    ConnectToTarget(operations, road.EndIntersectionId, endPosition, roadId);
                }
            }

            ResetState();
            FinishOperation();
        }

        private void ResetState()
        {
            keyPoints.Clear();
            startSnapped = false;
            endSnapped = false;
            isExtending = false;
            extendRoadId = 0;
            extendAtEnd = false;
        }

        private RoadEditOperations CreateOperations()
        {
            NavigationEditorPrefs prefs = context.Prefs;
            IGroundProbe probe = new PhysicsGroundProbe(prefs.RoadLayers, context.UnitsPerMeter);
            return new RoadEditOperations(context.Authoring, probe, prefs.MaxDeviation, prefs.MaxSpacing);
        }

        private RoadBrush CreateBrush()
        {
            NavigationEditorPrefs prefs = context.Prefs;
            RoadBrush brush = new RoadBrush();
            brush.SetTypeId(ResolveBrushTypeId());
            brush.SetOneWay(prefs.BrushOneWay);
            brush.SetSpeedOverride(prefs.BrushSpeedOverride);
            brush.SetWidthOverride(prefs.BrushWidthOverride);
            return brush;
        }

        private int ResolveBrushTypeId()
        {
            int typeId = context.Prefs.BrushTypeId;
            NavigationSettings settings = context.Authoring.Settings;
            if (settings == null)
            {
                return typeId;
            }
            if (settings.FindRoadType(typeId) != null)
            {
                return typeId;
            }
            if (settings.RoadTypes.Count > 0)
            {
                return settings.RoadTypes[0].Id;
            }
            return typeId;
        }

        private void ConnectToTarget(RoadEditOperations operations, int intersectionId, Vector3 position, int ignoreRoadId)
        {
            SnapTarget target;
            if (!snapFinder.FindSnap(position, ResolveTolerance, context.Authoring, ignoreRoadId, out target))
            {
                return;
            }

            if (target.Kind == SnapTargetKind.Intersection)
            {
                operations.ConnectIntersections(intersectionId, target.IntersectionId);
            }
            else if (target.Kind == SnapTargetKind.Road)
            {
                operations.ConnectToRoadMiddle(intersectionId, target.RoadId, target.Segment, target.T);
            }
        }

        private void FinishOperation()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            context.ValidationIssues.Clear();
            validator.RunCheapChecks(authoring, context.ValidationIssues);
            EditorUtility.SetDirty(authoring);
            SceneView.RepaintAll();
        }

        private void HandleClick()
        {
            bool snapped = hoverSnap.Kind != SnapTargetKind.None;
            Vector3 position = GetClickPosition();

            if (isExtending)
            {
                ExtendTo(position, snapped);
                return;
            }

            if (keyPoints.Count == 0)
            {
                if (hoverSnap.Kind == SnapTargetKind.Intersection && TryStartExtend(hoverSnap.IntersectionId))
                {
                    return;
                }

                keyPoints.Add(position);
                startSnapped = snapped;
                endSnapped = false;
                return;
            }

            Vector3 lastPosition = keyPoints[keyPoints.Count - 1];
            if ((position - lastPosition).sqrMagnitude < MinKeyPointSpacing * MinKeyPointSpacing)
            {
                return;
            }

            keyPoints.Add(position);
            endSnapped = snapped;
            if (snapped)
            {
                CommitRoad();
            }
        }

        private Vector3 GetClickPosition()
        {
            if (hoverSnap.Kind != SnapTargetKind.None)
            {
                return hoverSnap.Position;
            }
            return hoverPosition;
        }

        private void ExtendTo(Vector3 position, bool snapped)
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            AuthoringRoad road = authoring.FindRoad(extendRoadId);
            if (road == null)
            {
                ResetState();
                return;
            }

            Vector3 endPosition = GetExtendEndPosition(road);
            if ((position - endPosition).sqrMagnitude < MinKeyPointSpacing * MinKeyPointSpacing)
            {
                return;
            }

            Undo.RecordObject(authoring, "Extend Road");
            RoadEditOperations operations = CreateOperations();
            if (!operations.ExtendRoad(extendRoadId, extendAtEnd, position))
            {
                ResetState();
                return;
            }

            if (snapped)
            {
                int endIntersectionId = GetExtendEndIntersectionId(road);
                ConnectToTarget(operations, endIntersectionId, position, extendRoadId);
                ResetState();
            }

            FinishOperation();
        }

        private Vector3 GetExtendEndPosition(AuthoringRoad road)
        {
            if (extendAtEnd)
            {
                return road.KeyPoints[road.KeyPoints.Count - 1].Position;
            }
            return road.KeyPoints[0].Position;
        }

        private int GetExtendEndIntersectionId(AuthoringRoad road)
        {
            if (extendAtEnd)
            {
                return road.EndIntersectionId;
            }
            return road.StartIntersectionId;
        }

        private bool TryStartExtend(int intersectionId)
        {
            context.Authoring.GetRoadsAtIntersection(intersectionId, connectedRoads);
            if (connectedRoads.Count != 1)
            {
                return false;
            }

            AuthoringRoad road = connectedRoads[0];
            if (road.StartIntersectionId == road.EndIntersectionId || road.KeyPoints.Count < 2)
            {
                return false;
            }

            isExtending = true;
            extendRoadId = road.Id;
            extendAtEnd = road.EndIntersectionId == intersectionId;
            return true;
        }

        private void RemoveLastKeyPoint()
        {
            if (isExtending)
            {
                ResetState();
                return;
            }

            keyPoints.RemoveAt(keyPoints.Count - 1);
            endSnapped = false;
            if (keyPoints.Count == 0)
            {
                startSnapped = false;
            }
        }

        private void HandleKeyDown(Event current, SceneView sceneView)
        {
            KeyCode keyCode = current.keyCode;

            if (keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter)
            {
                if (IsDrawing)
                {
                    Finish();
                    current.Use();
                }
                return;
            }

            if (keyCode == KeyCode.Escape)
            {
                if (IsDrawing)
                {
                    ResetState();
                    current.Use();
                    sceneView.Repaint();
                }
                return;
            }

            if (keyCode == KeyCode.LeftShift || keyCode == KeyCode.RightShift)
            {
                UpdateHover(lastMousePosition, true);
                sceneView.Repaint();
            }
        }

        private void DrawModeVisuals()
        {
            if (isExtending)
            {
                DrawExtendPreview();
            }
            else if (keyPoints.Count > 0)
            {
                DrawRoadPreview();
            }

            DrawHover();
        }

        private void DrawExtendPreview()
        {
            AuthoringRoad road = context.Authoring.FindRoad(extendRoadId);
            if (road == null)
            {
                return;
            }

            Vector3 endWorld = converter.TrueToWorld(GetExtendEndPosition(road));
            Handles.color = Color.green;
            Handles.DrawWireDisc(endWorld, Vector3.up, HandleUtility.GetHandleSize(endWorld) * SnapHighlightSize);

            if (hasHover)
            {
                Handles.color = Color.white;
                Handles.DrawDottedLine(endWorld, converter.TrueToWorld(GetClickPosition()), DottedLineSpacing);
            }
        }

        private void DrawRoadPreview()
        {
            previewRoad.KeyPoints.Clear();
            for (int i = 0; i < keyPoints.Count; i++)
            {
                previewRoad.KeyPoints.Add(new AuthoringKeyPoint(keyPoints[i]));
            }
            if (hasHover)
            {
                previewRoad.KeyPoints.Add(new AuthoringKeyPoint(GetClickPosition()));
            }

            if (previewRoad.KeyPoints.Count >= 2)
            {
                DrawPreviewCurve();
            }

            Handles.color = new Color(1f, 0.5f, 0f);
            for (int i = 0; i < keyPoints.Count; i++)
            {
                Vector3 world = converter.TrueToWorld(keyPoints[i]);
                Handles.DotHandleCap(0, world, Quaternion.identity, HandleUtility.GetHandleSize(world) * KeyPointDotSize, EventType.Repaint);
            }
        }

        private void DrawPreviewCurve()
        {
            curve.UpdateAutoHandles(previewRoad);

            int segmentCount = previewRoad.KeyPoints.Count - 1;
            int pointCount = segmentCount * PreviewStepsPerSegment + 1;
            if (previewBuffer.Length < pointCount)
            {
                previewBuffer = new Vector3[pointCount];
            }

            int index = 0;
            for (int segment = 0; segment < segmentCount; segment++)
            {
                int startStep = 1;
                if (segment == 0)
                {
                    startStep = 0;
                }
                for (int step = startStep; step <= PreviewStepsPerSegment; step++)
                {
                    float t = (float)step / PreviewStepsPerSegment;
                    previewBuffer[index] = converter.TrueToWorld(curve.Evaluate(previewRoad, segment, t));
                    index++;
                }
            }

            Handles.color = new Color(1f, 0.5f, 0f);
            Handles.DrawAAPolyLine(PreviewLineWidth, index, previewBuffer);
        }

        private void DrawHover()
        {
            if (!hasHover)
            {
                return;
            }

            if (hoverSnap.Kind != SnapTargetKind.None)
            {
                Vector3 snapWorld = converter.TrueToWorld(hoverSnap.Position);
                Handles.color = new Color(1f, 0.92f, 0.016f, 0.6f);
                Handles.DrawSolidDisc(snapWorld, Vector3.up, HandleUtility.GetHandleSize(snapWorld) * SnapHighlightSize);
                return;
            }

            Vector3 hoverWorld = converter.TrueToWorld(hoverPosition);
            Handles.color = Color.white;
            Handles.DotHandleCap(0, hoverWorld, Quaternion.identity, HandleUtility.GetHandleSize(hoverWorld) * HoverDotSize, EventType.Repaint);
        }
    }
}
