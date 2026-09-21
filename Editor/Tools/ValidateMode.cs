using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class ValidateMode : IRoadEditorMode
    {
        private const float SelectedMarkerRadius = 2.5f;
        private const float FrameMargin = 10f;

        private readonly RoadEditorContext context;
        private readonly RoadValidator validator;
        private readonly WorldConverter converter;

        private Vector2 scrollPosition;
        private int selectedIndex;
        private bool hasSelection;

        public ValidateMode(RoadEditorContext context)
        {
            this.context = context;
            validator = new RoadValidator();
            converter = new WorldConverter();
        }

        public void OnEnter()
        {
            hasSelection = false;
        }

        public void OnExit()
        {
            hasSelection = false;
        }

        public void OnWindowGUI()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            if (authoring == null)
            {
                EditorGUILayout.HelpBox("This map has no road network asset.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Validate"))
            {
                RunFullChecks(authoring);
            }

            List<ValidationIssue> issues = context.ValidationIssues;
            EditorGUILayout.LabelField(issues.Count + " issue(s)");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < issues.Count; i++)
            {
                if (GUILayout.Button(issues[i].Message, EditorStyles.linkLabel))
                {
                    SelectIssue(i);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (!hasSelection || context.Authoring == null || selectedIndex >= context.ValidationIssues.Count)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            UpdateConverter();
            Vector3 world = converter.TrueToWorld(context.ValidationIssues[selectedIndex].Position);

            Handles.color = Color.yellow;
            Handles.DrawWireDisc(world, Vector3.up, SelectedMarkerRadius * context.UnitsPerMeter);
        }

        private void RunFullChecks(RoadNetworkAuthoring authoring)
        {
            context.ValidationIssues.Clear();
            validator.RunFullChecks(authoring, context.MapData, context.ValidationIssues);
            hasSelection = false;
            SceneView.RepaintAll();
        }

        private void SelectIssue(int index)
        {
            selectedIndex = index;
            hasSelection = true;

            UpdateConverter();
            Vector3 world = converter.TrueToWorld(context.ValidationIssues[index].Position);
            float margin = FrameMargin * context.UnitsPerMeter;
            Bounds bounds = new Bounds(world, new Vector3(margin, margin, margin));

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.Frame(bounds, false);
            }

            SceneView.RepaintAll();
        }

        private void UpdateConverter()
        {
            float unitsPerMeter = context.UnitsPerMeter;
            if (unitsPerMeter > 0f)
            {
                converter.SetUnitsPerMeter(unitsPerMeter);
            }
        }
    }
}
