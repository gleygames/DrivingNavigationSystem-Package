using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class ConnectMode : IRoadEditorMode
    {
        private const float SnapHighlightSize = 0.2f;
        private const float DottedLineSpacing = 4f;

        private readonly RoadEditorContext context;
        private readonly SnapFinder snapFinder;
        private readonly RoadValidator validator;
        private readonly WorldConverter converter;

        private SnapTarget hoverSnap;
        private Vector3 hoverPosition;
        private int fromIntersectionId;
        private bool hasHover;

        public ConnectMode(RoadEditorContext context)
        {
            this.context = context;
            snapFinder = new SnapFinder();
            validator = new RoadValidator();
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
            if (context.Authoring == null)
            {
                EditorGUILayout.HelpBox("This map has no road network asset.", MessageType.Info);
                return;
            }

            PruneState();

            if (fromIntersectionId != 0)
            {
                EditorGUILayout.LabelField("Status", "Connecting intersection " + fromIntersectionId);
            }
            else
            {
                EditorGUILayout.LabelField("Status", "Pick an intersection");
            }

            EditorGUILayout.HelpBox("Click an intersection, then another intersection or a road.\nIntersection: the first one is merged into the second.\nRoad: the road is split there and connected.\nEsc: cancel.", MessageType.None);
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

            PruneState();

            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            switch (current.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
                case EventType.MouseMove:
                    UpdateHover(current.mousePosition);
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
                case EventType.KeyDown:
                    if (current.keyCode == KeyCode.Escape && fromIntersectionId != 0)
                    {
                        fromIntersectionId = 0;
                        current.Use();
                        RepaintViews();
                    }
                    break;
                case EventType.Repaint:
                    DrawModeVisuals();
                    break;
            }
        }

        private void ResetState()
        {
            fromIntersectionId = 0;
            hasHover = false;
            hoverSnap = new SnapTarget();
        }

        private void PruneState()
        {
            if (fromIntersectionId != 0 && context.Authoring.FindIntersection(fromIntersectionId) == null)
            {
                fromIntersectionId = 0;
            }
        }

        private void UpdateHover(Vector2 mousePosition)
        {
            hoverSnap = new SnapTarget();

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            RaycastHit hit;
            hasHover = Physics.Raycast(ray, out hit, Mathf.Infinity, context.Prefs.RoadLayers, QueryTriggerInteraction.Ignore);
            if (!hasHover)
            {
                return;
            }

            hoverPosition = converter.WorldToTrue(hit.point);

            SnapTarget target;
            if (snapFinder.FindSnap(hoverPosition, context.Prefs.SnapDistance, context.Authoring, 0, out target))
            {
                hoverSnap = target;
            }
        }

        private void RepaintViews()
        {
            InternalEditorUtility.RepaintAllViews();
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
            GUIUtility.hotControl = controlId;
            current.Use();

            UpdateHover(mousePosition);
            HandleClick();
            RepaintViews();
        }

        private void HandleClick()
        {
            if (hoverSnap.Kind == SnapTargetKind.None)
            {
                return;
            }

            if (fromIntersectionId == 0)
            {
                if (hoverSnap.Kind == SnapTargetKind.Intersection)
                {
                    fromIntersectionId = hoverSnap.IntersectionId;
                }
                return;
            }

            RoadNetworkAuthoring authoring = context.Authoring;
            if (hoverSnap.Kind == SnapTargetKind.Intersection)
            {
                if (hoverSnap.IntersectionId == fromIntersectionId)
                {
                    return;
                }

                Undo.RecordObject(authoring, "Connect Intersections");
                CreateOperations().ConnectIntersections(fromIntersectionId, hoverSnap.IntersectionId);
            }
            else
            {
                Undo.RecordObject(authoring, "Connect To Road");
                CreateOperations().ConnectToRoadMiddle(fromIntersectionId, hoverSnap.RoadId, hoverSnap.Segment, hoverSnap.T);
            }

            fromIntersectionId = 0;
            hoverSnap = new SnapTarget();
            FinishOperation();
        }

        private RoadEditOperations CreateOperations()
        {
            NavigationEditorPrefs prefs = context.Prefs;
            IGroundProbe probe = new PhysicsGroundProbe(prefs.RoadLayers, context.UnitsPerMeter);
            return new RoadEditOperations(context.Authoring, probe, prefs.MaxDeviation, prefs.MaxSpacing);
        }

        private void FinishOperation()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            context.ValidationIssues.Clear();
            validator.RunCheapChecks(authoring, context.ValidationIssues);
            EditorUtility.SetDirty(authoring);
        }

        private void DrawModeVisuals()
        {
            Vector3 fromWorld = Vector3.zero;
            bool hasFrom = false;
            if (fromIntersectionId != 0)
            {
                AuthoringIntersection fromIntersection = context.Authoring.FindIntersection(fromIntersectionId);
                if (fromIntersection != null)
                {
                    hasFrom = true;
                    fromWorld = converter.TrueToWorld(fromIntersection.Position);
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(fromWorld, Vector3.up, HandleUtility.GetHandleSize(fromWorld) * SnapHighlightSize);
                }
            }

            if (!hasHover)
            {
                return;
            }

            Vector3 targetWorld = converter.TrueToWorld(hoverPosition);
            if (hoverSnap.Kind != SnapTargetKind.None)
            {
                targetWorld = converter.TrueToWorld(hoverSnap.Position);
                Handles.color = new Color(1f, 0.92f, 0.016f, 0.6f);
                Handles.DrawSolidDisc(targetWorld, Vector3.up, HandleUtility.GetHandleSize(targetWorld) * SnapHighlightSize);
            }

            if (hasFrom)
            {
                Handles.color = Color.white;
                Handles.DrawDottedLine(fromWorld, targetWorld, DottedLineSpacing);
            }
        }
    }
}
