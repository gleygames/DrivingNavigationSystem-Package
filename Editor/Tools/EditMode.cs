using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class EditMode : IRoadEditorMode
    {
        private const float PickDistance = 10f;
        private const float BoxDragThreshold = 4f;
        private const float MinCurveT = 0.01f;
        private const float KeyPointHandleSize = 0.08f;
        private const float CurveHandleSize = 0.06f;
        private const float IntersectionHandleSize = 0.12f;
        private const float SelectedRoadLineWidth = 6f;
        private const float SelectedIntersectionRadius = 1.5f;
        private const float MarkerSize = 0.15f;
        private const int CurveSamplesPerSegment = 32;

        private readonly List<int> handleIntersectionIds;
        private readonly List<int> deleteBuffer;
        private readonly List<int> boxRoadIds;
        private readonly List<Vector2> boxGuiPoints;
        private readonly List<int> boxGuiPointStarts;
        private readonly RoadEditorContext context;
        private readonly RoadSelection selection;
        private readonly RoadValidator validator;
        private readonly RoadCurve curve;
        private readonly WorldConverter converter;

        private Vector3[] lineBuffer = new Vector3[0];
        private Camera sceneCamera;
        private Vector2 boxStart;
        private Vector2 boxEnd;
        private float lastClickedT;
        private int lastClickedRoadId;
        private int lastClickedSegment;
        private int pressedControlId;
        private bool hasLastClicked;
        private bool isBoxCandidate;
        private bool isBoxSelecting;
        private bool boxAdditive;
        private bool dragUndoRecorded;
        private bool swallowDeleteCommand;

        public EditMode(RoadEditorContext context)
        {
            this.context = context;
            handleIntersectionIds = new List<int>();
            deleteBuffer = new List<int>();
            boxRoadIds = new List<int>();
            boxGuiPoints = new List<Vector2>();
            boxGuiPointStarts = new List<int>();
            selection = new RoadSelection();
            validator = new RoadValidator();
            curve = new RoadCurve();
            converter = new WorldConverter();
        }

        public void OnEnter()
        {
            ResetInteraction();
        }

        public void OnExit()
        {
            ResetInteraction();
        }

        public void OnWindowGUI()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            if (authoring == null)
            {
                EditorGUILayout.HelpBox("This map has no road network asset.", MessageType.Info);
                return;
            }

            PruneSelection();

            EditorGUILayout.LabelField("Selection", BuildSelectionSummary());
            DrawOperationButtons();
            DrawPropertiesPanel(authoring);
            EditorGUILayout.HelpBox("Click: select a road, key point or intersection. Ctrl+click: add to selection.\nDrag on empty space: box select roads.\nDrag handles: move key points, intersections and curve handles.\nDouble-click a road: insert a key point.\nDelete: delete the selected key point, or the selected roads.\nSplit uses the last clicked road position.", MessageType.None);
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
            sceneCamera = sceneView.camera;

            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            UpdateHandleDragEnd();
            PruneSelection();

            if (current.type == EventType.Repaint)
            {
                DrawSelectionVisuals();
            }

            DrawHandles();

            switch (current.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
                case EventType.MouseDown:
                    HandleMouseDown(current, controlId);
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        HandleMouseDrag(current);
                        current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        HandleMouseUp();
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;
                case EventType.KeyDown:
                    HandleKeyDown(current);
                    break;
                case EventType.ValidateCommand:
                    HandleValidateCommand(current);
                    break;
                case EventType.ExecuteCommand:
                    HandleExecuteCommand(current);
                    break;
                case EventType.Repaint:
                    DrawBox();
                    break;
            }
        }

        private void ResetInteraction()
        {
            isBoxCandidate = false;
            isBoxSelecting = false;
            pressedControlId = 0;
            dragUndoRecorded = false;
            swallowDeleteCommand = false;
        }

        private void PruneSelection()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            List<int> roadIds = selection.RoadIds;
            for (int i = roadIds.Count - 1; i >= 0; i--)
            {
                if (authoring.FindRoad(roadIds[i]) == null)
                {
                    selection.RemoveRoad(roadIds[i]);
                }
            }

            if (selection.HasKeyPoint)
            {
                AuthoringRoad road = authoring.FindRoad(selection.KeyPointRoadId);
                if (road == null || selection.KeyPointIndex <= 0 || selection.KeyPointIndex >= road.KeyPoints.Count - 1)
                {
                    selection.ClearKeyPoint();
                }
            }

            if (selection.HasIntersection && authoring.FindIntersection(selection.IntersectionId) == null)
            {
                selection.ClearIntersection();
            }

            if (hasLastClicked)
            {
                AuthoringRoad road = authoring.FindRoad(lastClickedRoadId);
                if (road == null || lastClickedSegment >= road.KeyPoints.Count - 1)
                {
                    hasLastClicked = false;
                }
            }
        }

        private string BuildSelectionSummary()
        {
            string summary = selection.RoadIds.Count + " roads";
            if (selection.HasKeyPoint)
            {
                summary += ", key point " + selection.KeyPointIndex + " of road " + selection.KeyPointRoadId;
            }
            if (selection.HasIntersection)
            {
                summary += ", intersection " + selection.IntersectionId;
            }
            return summary;
        }

        private void DrawOperationButtons()
        {
            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = hasLastClicked;
            if (GUILayout.Button("Split"))
            {
                SplitAtLastClicked();
            }

            GUI.enabled = selection.HasIntersection;
            if (GUILayout.Button("Merge"))
            {
                MergeSelectedIntersection();
            }
            if (GUILayout.Button("Disconnect"))
            {
                DisconnectSelectedIntersection();
            }

            GUI.enabled = selection.RoadIds.Count > 0;
            if (GUILayout.Button("Flip"))
            {
                FlipSelectedRoads();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void SplitAtLastClicked()
        {
            Undo.RecordObject(context.Authoring, "Split Road");
            int newIntersectionId = CreateOperations().SplitRoad(lastClickedRoadId, lastClickedSegment, lastClickedT);
            hasLastClicked = false;
            if (newIntersectionId == 0)
            {
                Notify("Split failed.");
                return;
            }

            selection.ClearKeyPoint();
            selection.Click(new SelectionHit(SelectionHitKind.Intersection, 0, -1, newIntersectionId), true);
            FinishOperation();
        }

        private RoadEditOperations CreateOperations()
        {
            NavigationEditorPrefs prefs = context.Prefs;
            IGroundProbe probe = new PhysicsGroundProbe(prefs.RoadLayers, context.UnitsPerMeter);
            return new RoadEditOperations(context.Authoring, probe, prefs.MaxDeviation, prefs.MaxSpacing);
        }

        private void Notify(string message)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.ShowNotification(new GUIContent(message));
            }
        }

        private void FinishOperation()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            context.ValidationIssues.Clear();
            validator.RunCheapChecks(authoring, context.ValidationIssues);
            EditorUtility.SetDirty(authoring);
            RepaintViews();
        }

        private void RepaintViews()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        private void MergeSelectedIntersection()
        {
            Undo.RecordObject(context.Authoring, "Merge Roads");
            if (!CreateOperations().MergeAtIntersection(selection.IntersectionId))
            {
                Notify("Merge needs exactly two roads with matching properties at this intersection.");
                return;
            }

            selection.ClearIntersection();
            selection.ClearKeyPoint();
            PruneSelection();
            FinishOperation();
        }

        private void DisconnectSelectedIntersection()
        {
            Undo.RecordObject(context.Authoring, "Disconnect Intersection");
            if (!CreateOperations().Disconnect(selection.IntersectionId))
            {
                return;
            }
            FinishOperation();
        }

        private void FlipSelectedRoads()
        {
            Undo.RecordObject(context.Authoring, "Flip Roads");
            RoadEditOperations operations = CreateOperations();
            List<int> roadIds = selection.RoadIds;
            for (int i = 0; i < roadIds.Count; i++)
            {
                operations.Flip(roadIds[i]);
            }

            selection.ClearKeyPoint();
            hasLastClicked = false;
            FinishOperation();
        }

        private void DrawPropertiesPanel(RoadNetworkAuthoring authoring)
        {
            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

            List<int> roadIds = selection.RoadIds;
            if (roadIds.Count == 0)
            {
                EditorGUILayout.LabelField("No roads selected");
                return;
            }

            AuthoringRoad first = authoring.FindRoad(roadIds[0]);
            if (first == null)
            {
                return;
            }

            bool typeMixed = false;
            bool oneWayMixed = false;
            bool speedMixed = false;
            bool widthMixed = false;
            for (int i = 1; i < roadIds.Count; i++)
            {
                AuthoringRoad road = authoring.FindRoad(roadIds[i]);
                if (road == null)
                {
                    continue;
                }
                if (road.TypeId != first.TypeId)
                {
                    typeMixed = true;
                }
                if (road.OneWay != first.OneWay)
                {
                    oneWayMixed = true;
                }
                if (road.SpeedOverride != first.SpeedOverride)
                {
                    speedMixed = true;
                }
                if (road.WidthOverride != first.WidthOverride)
                {
                    widthMixed = true;
                }
            }

            EditorGUI.showMixedValue = typeMixed;
            EditorGUI.BeginChangeCheck();
            int typeId = DrawTypePopup(authoring.Settings, first.TypeId);
            if (EditorGUI.EndChangeCheck())
            {
                RoadBrush values = new RoadBrush();
                values.SetTypeId(typeId);
                ApplyProperties(values, true, false, false, false);
            }

            EditorGUI.showMixedValue = oneWayMixed;
            EditorGUI.BeginChangeCheck();
            bool oneWay = EditorGUILayout.Toggle("One-way", first.OneWay);
            if (EditorGUI.EndChangeCheck())
            {
                RoadBrush values = new RoadBrush();
                values.SetOneWay(oneWay);
                ApplyProperties(values, false, true, false, false);
            }

            EditorGUI.showMixedValue = speedMixed;
            EditorGUI.BeginChangeCheck();
            float speedOverride = EditorGUILayout.DelayedFloatField("Speed Override (m/s, 0 = type)", first.SpeedOverride);
            if (EditorGUI.EndChangeCheck())
            {
                RoadBrush values = new RoadBrush();
                values.SetSpeedOverride(Mathf.Max(0f, speedOverride));
                ApplyProperties(values, false, false, true, false);
            }

            EditorGUI.showMixedValue = widthMixed;
            EditorGUI.BeginChangeCheck();
            float widthOverride = EditorGUILayout.DelayedFloatField("Width Override (m, 0 = type)", first.WidthOverride);
            if (EditorGUI.EndChangeCheck())
            {
                RoadBrush values = new RoadBrush();
                values.SetWidthOverride(Mathf.Max(0f, widthOverride));
                ApplyProperties(values, false, false, false, true);
            }

            EditorGUI.showMixedValue = false;
        }

        private int DrawTypePopup(NavigationSettings settings, int typeId)
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

        private void ApplyProperties(RoadBrush values, bool setType, bool setOneWay, bool setSpeed, bool setWidth)
        {
            Undo.RecordObject(context.Authoring, "Edit Road Properties");
            if (!CreateOperations().SetProperties(selection.RoadIds, values, setType, setOneWay, setSpeed, setWidth))
            {
                return;
            }
            FinishOperation();
        }

        private void UpdateHandleDragEnd()
        {
            if (pressedControlId == 0 || GUIUtility.hotControl == pressedControlId)
            {
                return;
            }

            bool changed = dragUndoRecorded;
            pressedControlId = 0;
            dragUndoRecorded = false;
            if (changed)
            {
                FinishOperation();
            }
        }

        private void DrawSelectionVisuals()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            List<int> roadIds = selection.RoadIds;

            Handles.color = new Color(1f, 0.92f, 0.016f, 0.8f);
            for (int i = 0; i < roadIds.Count; i++)
            {
                AuthoringRoad road = authoring.FindRoad(roadIds[i]);
                if (road != null)
                {
                    DrawRoadHighlight(road);
                }
            }

            if (selection.HasIntersection)
            {
                AuthoringIntersection intersection = authoring.FindIntersection(selection.IntersectionId);
                if (intersection != null)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawWireDisc(ToWorld(intersection.Position), Vector3.up, SelectedIntersectionRadius * context.UnitsPerMeter);
                }
            }

            if (hasLastClicked)
            {
                AuthoringRoad road = authoring.FindRoad(lastClickedRoadId);
                if (road != null)
                {
                    Vector3 markerWorld = ToWorld(curve.Evaluate(road, lastClickedSegment, lastClickedT));
                    Handles.color = Color.white;
                    Handles.DrawWireDisc(markerWorld, Vector3.up, HandleUtility.GetHandleSize(markerWorld) * MarkerSize);
                }
            }
        }

        private void DrawRoadHighlight(AuthoringRoad road)
        {
            List<Vector3> points = road.Points;
            if (points.Count < 2)
            {
                return;
            }

            if (lineBuffer.Length < points.Count)
            {
                lineBuffer = new Vector3[points.Count];
            }
            for (int i = 0; i < points.Count; i++)
            {
                lineBuffer[i] = ToWorld(points[i]);
            }
            Handles.DrawAAPolyLine(SelectedRoadLineWidth, points.Count, lineBuffer);
        }

        private Vector3 ToWorld(Vector3 truePosition)
        {
            return converter.TrueToWorld(truePosition);
        }

        private void DrawHandles()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            List<int> roadIds = selection.RoadIds;
            for (int i = 0; i < roadIds.Count; i++)
            {
                AuthoringRoad road = authoring.FindRoad(roadIds[i]);
                if (road == null)
                {
                    continue;
                }
                for (int index = 1; index < road.KeyPoints.Count - 1; index++)
                {
                    DrawKeyPointHandle(road, index);
                }
            }

            CollectHandleIntersections(authoring);
            for (int i = 0; i < handleIntersectionIds.Count; i++)
            {
                AuthoringIntersection intersection = authoring.FindIntersection(handleIntersectionIds[i]);
                if (intersection != null)
                {
                    DrawIntersectionHandle(intersection);
                }
            }

            if (selection.HasKeyPoint)
            {
                DrawCurveHandles(authoring);
            }
        }

        private void DrawKeyPointHandle(AuthoringRoad road, int index)
        {
            AuthoringKeyPoint keyPoint = road.KeyPoints[index];
            Vector3 world = ToWorld(keyPoint.Position);
            float size = HandleUtility.GetHandleSize(world) * KeyPointHandleSize;

            if (selection.KeyPointRoadId == road.Id && selection.KeyPointIndex == index)
            {
                Handles.color = Color.yellow;
            }
            else
            {
                Handles.color = Color.cyan;
            }

            Vector3 newWorld;
            bool pressed;
            bool changed = DoMoveHandle(world, size, Handles.DotHandleCap, out newWorld, out pressed);

            if (pressed)
            {
                selection.Click(new SelectionHit(SelectionHitKind.KeyPoint, road.Id, index, 0), true);
                RepaintViews();
            }

            if (changed)
            {
                RecordDragUndo("Move Key Point");
                CreateOperations().MoveKeyPoint(road.Id, index, ProjectToGround(newWorld));
                EditorUtility.SetDirty(context.Authoring);
            }
        }

        private bool DoMoveHandle(Vector3 world, float size, Handles.CapFunction cap, out Vector3 newWorld, out bool pressed)
        {
            int hotBefore = GUIUtility.hotControl;
            EditorGUI.BeginChangeCheck();
            newWorld = Handles.FreeMoveHandle(world, size, Vector3.zero, cap);
            bool changed = EditorGUI.EndChangeCheck();
            int hotAfter = GUIUtility.hotControl;

            pressed = hotAfter != 0 && hotAfter != hotBefore;
            if (pressed)
            {
                pressedControlId = hotAfter;
                dragUndoRecorded = false;
                swallowDeleteCommand = false;
            }
            return changed;
        }

        private void RecordDragUndo(string actionName)
        {
            if (dragUndoRecorded)
            {
                return;
            }
            Undo.RegisterCompleteObjectUndo(context.Authoring, actionName);
            dragUndoRecorded = true;
        }

        private Vector3 ProjectToGround(Vector3 world)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(HandleUtility.WorldToGUIPoint(world));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, context.Prefs.RoadLayers, QueryTriggerInteraction.Ignore))
            {
                return converter.WorldToTrue(hit.point);
            }
            return converter.WorldToTrue(world);
        }

        private void CollectHandleIntersections(RoadNetworkAuthoring authoring)
        {
            handleIntersectionIds.Clear();
            List<int> roadIds = selection.RoadIds;
            for (int i = 0; i < roadIds.Count; i++)
            {
                AuthoringRoad road = authoring.FindRoad(roadIds[i]);
                if (road == null)
                {
                    continue;
                }
                AddHandleIntersection(road.StartIntersectionId);
                AddHandleIntersection(road.EndIntersectionId);
            }

            if (selection.HasIntersection)
            {
                AddHandleIntersection(selection.IntersectionId);
            }
        }

        private void AddHandleIntersection(int intersectionId)
        {
            if (intersectionId != 0 && !handleIntersectionIds.Contains(intersectionId))
            {
                handleIntersectionIds.Add(intersectionId);
            }
        }

        private void DrawIntersectionHandle(AuthoringIntersection intersection)
        {
            Vector3 world = ToWorld(intersection.Position);
            float size = HandleUtility.GetHandleSize(world) * IntersectionHandleSize;

            if (selection.IntersectionId == intersection.Id)
            {
                Handles.color = Color.yellow;
            }
            else
            {
                Handles.color = Color.green;
            }

            Vector3 newWorld;
            bool pressed;
            bool changed = DoMoveHandle(world, size, Handles.SphereHandleCap, out newWorld, out pressed);

            if (pressed)
            {
                selection.Click(new SelectionHit(SelectionHitKind.Intersection, 0, -1, intersection.Id), true);
                RepaintViews();
            }

            if (changed)
            {
                RecordDragUndo("Move Intersection");
                CreateOperations().MoveIntersection(intersection.Id, ProjectToGround(newWorld));
                EditorUtility.SetDirty(context.Authoring);
            }
        }

        private void DrawCurveHandles(RoadNetworkAuthoring authoring)
        {
            AuthoringRoad road = authoring.FindRoad(selection.KeyPointRoadId);
            if (road == null)
            {
                return;
            }

            int index = selection.KeyPointIndex;
            if (index < 0 || index >= road.KeyPoints.Count)
            {
                return;
            }

            DrawCurveHandle(road, index, false);
            DrawCurveHandle(road, index, true);
        }

        private void DrawCurveHandle(AuthoringRoad road, int index, bool isOut)
        {
            AuthoringKeyPoint keyPoint = road.KeyPoints[index];
            Vector3 offset;
            if (isOut)
            {
                offset = keyPoint.OutHandle;
            }
            else
            {
                offset = keyPoint.InHandle;
            }

            Vector3 keyWorld = ToWorld(keyPoint.Position);
            Vector3 tipWorld = ToWorld(keyPoint.Position + offset);
            float size = HandleUtility.GetHandleSize(tipWorld) * CurveHandleSize;

            Handles.color = new Color(1f, 0.5f, 0f);
            if (Event.current.type == EventType.Repaint)
            {
                Handles.DrawLine(keyWorld, tipWorld);
            }

            Vector3 newWorld;
            bool pressed;
            bool changed = DoMoveHandle(tipWorld, size, Handles.DotHandleCap, out newWorld, out pressed);
            if (!changed)
            {
                return;
            }

            Vector3 flatWorld = ProjectToHorizontalPlane(newWorld, tipWorld.y);
            RecordDragUndo("Move Curve Handle");
            Vector3 newOffset = converter.WorldToTrue(flatWorld) - keyPoint.Position;
            RoadEditOperations operations = CreateOperations();
            operations.MoveHandle(road.Id, index, !isOut, -newOffset);
            operations.MoveHandle(road.Id, index, isOut, newOffset);
            EditorUtility.SetDirty(context.Authoring);
        }

        private Vector3 ProjectToHorizontalPlane(Vector3 world, float height)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(HandleUtility.WorldToGUIPoint(world));
            Plane plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            float enter;
            if (plane.Raycast(ray, out enter))
            {
                return ray.GetPoint(enter);
            }
            return world;
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
            bool additive = EditorGUI.actionKey;
            int clickCount = current.clickCount;

            swallowDeleteCommand = false;
            GUIUtility.hotControl = controlId;
            current.Use();

            int segment;
            float t;
            SelectionHit hit = Pick(mousePosition, out segment, out t);

            if (clickCount >= 2 && hit.Kind == SelectionHitKind.Road)
            {
                InsertKeyPointAt(hit.RoadId, segment, t);
                return;
            }

            if (hit.Kind == SelectionHitKind.None)
            {
                isBoxCandidate = true;
                isBoxSelecting = false;
                boxAdditive = additive;
                boxStart = mousePosition;
                boxEnd = mousePosition;
                return;
            }

            selection.Click(hit, additive);
            if (hit.Kind == SelectionHitKind.Road)
            {
                hasLastClicked = true;
                lastClickedRoadId = hit.RoadId;
                lastClickedSegment = segment;
                lastClickedT = t;
            }
            RepaintViews();
        }

        private SelectionHit Pick(Vector2 mousePosition, out int segment, out float t)
        {
            segment = 0;
            t = 0f;

            int intersectionId = PickIntersection(mousePosition);
            if (intersectionId != 0)
            {
                return new SelectionHit(SelectionHitKind.Intersection, 0, -1, intersectionId);
            }

            int keyPointRoadId;
            int keyPointIndex;
            if (PickKeyPoint(mousePosition, out keyPointRoadId, out keyPointIndex))
            {
                return new SelectionHit(SelectionHitKind.KeyPoint, keyPointRoadId, keyPointIndex, 0);
            }

            AuthoringRoad road = PickRoad(mousePosition);
            if (road != null)
            {
                FindNearestCurvePosition(road, mousePosition, out segment, out t);
                return new SelectionHit(SelectionHitKind.Road, road.Id, -1, 0);
            }

            return new SelectionHit(SelectionHitKind.None, 0, -1, 0);
        }

        private int PickIntersection(Vector2 mousePosition)
        {
            List<AuthoringIntersection> intersections = context.Authoring.Intersections;
            float bestDistance = PickDistance;
            int bestId = 0;
            for (int i = 0; i < intersections.Count; i++)
            {
                Vector3 world = ToWorld(intersections[i].Position);
                if (!IsInFront(world))
                {
                    continue;
                }

                float distance = Vector2.Distance(HandleUtility.WorldToGUIPoint(world), mousePosition);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    bestId = intersections[i].Id;
                }
            }
            return bestId;
        }

        private bool IsInFront(Vector3 world)
        {
            if (sceneCamera == null)
            {
                return true;
            }
            return sceneCamera.WorldToViewportPoint(world).z > 0f;
        }

        private bool PickKeyPoint(Vector2 mousePosition, out int roadId, out int index)
        {
            List<AuthoringRoad> roads = context.Authoring.Roads;
            float bestDistance = PickDistance;
            roadId = 0;
            index = -1;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                List<AuthoringKeyPoint> keyPoints = road.KeyPoints;
                for (int k = 1; k < keyPoints.Count - 1; k++)
                {
                    Vector3 world = ToWorld(keyPoints[k].Position);
                    if (!IsInFront(world))
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(HandleUtility.WorldToGUIPoint(world), mousePosition);
                    if (distance <= bestDistance)
                    {
                        bestDistance = distance;
                        roadId = road.Id;
                        index = k;
                    }
                }
            }
            return index >= 0;
        }

        private AuthoringRoad PickRoad(Vector2 mousePosition)
        {
            List<AuthoringRoad> roads = context.Authoring.Roads;
            float bestDistance = PickDistance;
            AuthoringRoad best = null;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                List<Vector3> points = road.Points;
                Vector2 previousGui = Vector2.zero;
                bool previousValid = false;
                for (int p = 0; p < points.Count; p++)
                {
                    Vector3 world = ToWorld(points[p]);
                    bool valid = IsInFront(world);
                    Vector2 gui = Vector2.zero;
                    if (valid)
                    {
                        gui = HandleUtility.WorldToGUIPoint(world);
                    }

                    if (valid && previousValid)
                    {
                        float distance = HandleUtility.DistancePointToLineSegment(mousePosition, previousGui, gui);
                        if (distance <= bestDistance)
                        {
                            bestDistance = distance;
                            best = road;
                        }
                    }

                    previousGui = gui;
                    previousValid = valid;
                }
            }
            return best;
        }

        private void FindNearestCurvePosition(AuthoringRoad road, Vector2 mousePosition, out int segment, out float t)
        {
            segment = 0;
            t = 0.5f;
            float bestDistance = float.MaxValue;
            int segmentCount = road.KeyPoints.Count - 1;

            for (int s = 0; s < segmentCount; s++)
            {
                Vector3 previousWorld = ToWorld(curve.Evaluate(road, s, 0f));
                bool previousValid = IsInFront(previousWorld);
                Vector2 previousGui = Vector2.zero;
                if (previousValid)
                {
                    previousGui = HandleUtility.WorldToGUIPoint(previousWorld);
                }
                float previousT = 0f;

                for (int step = 1; step <= CurveSamplesPerSegment; step++)
                {
                    float stepT = (float)step / CurveSamplesPerSegment;
                    Vector3 world = ToWorld(curve.Evaluate(road, s, stepT));
                    bool valid = IsInFront(world);
                    Vector2 gui = Vector2.zero;
                    if (valid)
                    {
                        gui = HandleUtility.WorldToGUIPoint(world);
                    }

                    if (valid && previousValid)
                    {
                        Vector2 lineDelta = gui - previousGui;
                        float fraction = 0f;
                        float sqrLength = lineDelta.sqrMagnitude;
                        if (sqrLength > 0f)
                        {
                            fraction = Mathf.Clamp01(Vector2.Dot(mousePosition - previousGui, lineDelta) / sqrLength);
                        }

                        float distance = Vector2.Distance(mousePosition, previousGui + lineDelta * fraction);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            segment = s;
                            t = Mathf.Lerp(previousT, stepT, fraction);
                        }
                    }

                    previousGui = gui;
                    previousValid = valid;
                    previousT = stepT;
                }
            }

            t = Mathf.Clamp(t, MinCurveT, 1f - MinCurveT);
        }

        private void InsertKeyPointAt(int roadId, int segment, float t)
        {
            Undo.RecordObject(context.Authoring, "Insert Key Point");
            if (!CreateOperations().InsertKeyPoint(roadId, segment, t))
            {
                return;
            }

            selection.Click(new SelectionHit(SelectionHitKind.KeyPoint, roadId, segment + 1, 0), false);
            hasLastClicked = false;
            FinishOperation();
        }

        private void HandleMouseDrag(Event current)
        {
            if (!isBoxCandidate)
            {
                return;
            }

            boxEnd = current.mousePosition;
            if ((boxEnd - boxStart).sqrMagnitude > BoxDragThreshold * BoxDragThreshold)
            {
                isBoxSelecting = true;
            }
            HandleUtility.Repaint();
        }

        private void HandleMouseUp()
        {
            if (isBoxSelecting)
            {
                FillBoxPoints();
                selection.BoxSelect(GetBoxRect(), boxRoadIds, boxGuiPoints, boxGuiPointStarts, boxAdditive);
                RepaintViews();
            }
            else if (isBoxCandidate)
            {
                selection.Click(new SelectionHit(SelectionHitKind.None, 0, -1, 0), boxAdditive);
                RepaintViews();
            }

            isBoxCandidate = false;
            isBoxSelecting = false;
        }

        private void FillBoxPoints()
        {
            boxRoadIds.Clear();
            boxGuiPoints.Clear();
            boxGuiPointStarts.Clear();

            List<AuthoringRoad> roads = context.Authoring.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                AuthoringRoad road = roads[i];
                boxRoadIds.Add(road.Id);
                boxGuiPointStarts.Add(boxGuiPoints.Count);

                List<Vector3> points = road.Points;
                for (int p = 0; p < points.Count; p++)
                {
                    Vector3 world = ToWorld(points[p]);
                    if (IsInFront(world))
                    {
                        boxGuiPoints.Add(HandleUtility.WorldToGUIPoint(world));
                    }
                }
            }
        }

        private Rect GetBoxRect()
        {
            return Rect.MinMaxRect(Mathf.Min(boxStart.x, boxEnd.x), Mathf.Min(boxStart.y, boxEnd.y), Mathf.Max(boxStart.x, boxEnd.x), Mathf.Max(boxStart.y, boxEnd.y));
        }

        private void HandleKeyDown(Event current)
        {
            if (current.keyCode != KeyCode.Delete)
            {
                return;
            }

            if (DeleteSelection())
            {
                swallowDeleteCommand = true;
                current.Use();
            }
        }

        private bool DeleteSelection()
        {
            RoadNetworkAuthoring authoring = context.Authoring;

            if (selection.HasKeyPoint)
            {
                Undo.RecordObject(authoring, "Delete Key Point");
                bool deleted = CreateOperations().DeleteKeyPoint(selection.KeyPointRoadId, selection.KeyPointIndex);
                selection.ClearKeyPoint();
                if (deleted)
                {
                    hasLastClicked = false;
                    FinishOperation();
                    return true;
                }
            }

            if (selection.RoadIds.Count == 0)
            {
                return false;
            }

            Undo.RecordObject(authoring, "Delete Roads");
            deleteBuffer.Clear();
            deleteBuffer.AddRange(selection.RoadIds);
            RoadEditOperations operations = CreateOperations();
            for (int i = 0; i < deleteBuffer.Count; i++)
            {
                operations.DeleteRoad(deleteBuffer[i]);
            }

            selection.Clear();
            hasLastClicked = false;
            FinishOperation();
            return true;
        }

        private void HandleValidateCommand(Event current)
        {
            if (!IsDeleteCommand(current.commandName))
            {
                return;
            }
            if (swallowDeleteCommand || !selection.IsEmpty)
            {
                current.Use();
            }
        }

        private bool IsDeleteCommand(string commandName)
        {
            return commandName == "SoftDelete" || commandName == "Delete";
        }

        private void HandleExecuteCommand(Event current)
        {
            if (!IsDeleteCommand(current.commandName))
            {
                return;
            }

            if (swallowDeleteCommand)
            {
                swallowDeleteCommand = false;
                current.Use();
                return;
            }

            if (!selection.IsEmpty)
            {
                DeleteSelection();
                current.Use();
            }
        }

        private void DrawBox()
        {
            if (!isBoxSelecting)
            {
                return;
            }

            Rect rect = GetBoxRect();
            Color edgeColor = new Color(0.4f, 0.7f, 1f, 0.9f);
            Handles.BeginGUI();
            EditorGUI.DrawRect(rect, new Color(0.4f, 0.7f, 1f, 0.15f));
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, 1f), edgeColor);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - 1f, rect.width, 1f), edgeColor);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, 1f, rect.height), edgeColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.yMin, 1f, rect.height), edgeColor);
            Handles.EndGUI();
        }
    }
}
