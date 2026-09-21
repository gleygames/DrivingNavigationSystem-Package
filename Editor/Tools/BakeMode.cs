using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class BakeMode : IRoadEditorMode
    {
        private const float MinGridCellSize = 1f;

        private readonly List<IRoadImporter> availableImporters;
        private readonly RoadEditorContext context;
        private readonly BakeStatus bakeStatus;
        private readonly RoadBaker baker;
        private readonly RoadValidator validator;
        private readonly RoadImporterDiscovery importerDiscovery;

        public BakeMode(RoadEditorContext context)
        {
            this.context = context;
            bakeStatus = new BakeStatus();
            baker = new RoadBaker();
            validator = new RoadValidator();
            importerDiscovery = new RoadImporterDiscovery();
            availableImporters = new List<IRoadImporter>();
        }

        public void OnEnter()
        {
            importerDiscovery.Discover(availableImporters);
        }

        public void OnExit()
        {
        }

        public void OnWindowGUI()
        {
            RoadNetworkAuthoring authoring = context.Authoring;
            if (authoring == null)
            {
                EditorGUILayout.HelpBox("This map has no road network asset.", MessageType.Info);
                return;
            }

            DrawBakeSection(authoring);
            EditorGUILayout.Space();
            DrawImportSection(authoring);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
        }

        private void DrawBakeSection(RoadNetworkAuthoring authoring)
        {
            if (bakeStatus.IsOutdated(authoring))
            {
                EditorGUILayout.LabelField("Bake status", "Outdated");
            }
            else
            {
                EditorGUILayout.LabelField("Bake status", "Up to date");
            }

            EditorGUI.BeginChangeCheck();
            float gridCellSize = EditorGUILayout.DelayedFloatField("Grid Cell Size", authoring.GridCellSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(authoring, "Change Grid Cell Size");
                authoring.SetGridCellSize(Mathf.Max(MinGridCellSize, gridCellSize));
                EditorUtility.SetDirty(authoring);
            }

            if (GUILayout.Button("Bake"))
            {
                baker.Bake(authoring);
                RepaintViews();
            }
        }

        private void RepaintViews()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        private void DrawImportSection(RoadNetworkAuthoring authoring)
        {
            EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);

            if (availableImporters.Count == 0)
            {
                EditorGUILayout.HelpBox("No importers available.", MessageType.Info);
                return;
            }

            for (int i = 0; i < availableImporters.Count; i++)
            {
                IRoadImporter importer = availableImporters[i];
                if (GUILayout.Button(importer.DisplayName))
                {
                    RunImporter(authoring, importer);
                }
            }
        }

        private void RunImporter(RoadNetworkAuthoring authoring, IRoadImporter importer)
        {
            RoadImportRunner runner = CreateRunner();
            int existingCount = CountRoadsWithSourceTag(authoring, importer.SourceTag);
            if (existingCount > 0)
            {
                int modifiedCount = runner.CountModifiedImportedRoads(authoring, importer.SourceTag);
                string message = "Re-import replaces " + existingCount + " roads from " + importer.DisplayName + ". " + modifiedCount + " of them were edited by hand and those edits will be lost.";
                if (!EditorUtility.DisplayDialog("Re-import roads", message, "Re-import", "Cancel"))
                {
                    return;
                }
            }

            Undo.RecordObject(authoring, "Import Roads");
            runner.Run(importer, authoring);

            context.ValidationIssues.Clear();
            validator.RunCheapChecks(authoring, context.ValidationIssues);
            EditorUtility.SetDirty(authoring);
            RepaintViews();
        }

        private RoadImportRunner CreateRunner()
        {
            NavigationEditorPrefs prefs = context.Prefs;
            IGroundProbe probe = new PhysicsGroundProbe(prefs.RoadLayers, context.UnitsPerMeter);
            return new RoadImportRunner(probe, prefs.MaxDeviation, prefs.MaxSpacing, prefs.SnapDistance);
        }

        private int CountRoadsWithSourceTag(RoadNetworkAuthoring authoring, string sourceTag)
        {
            int count = 0;
            List<AuthoringRoad> roads = authoring.Roads;
            for (int i = 0; i < roads.Count; i++)
            {
                if (roads[i].SourceTag == sourceTag)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
