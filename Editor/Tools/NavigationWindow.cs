using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Gley.Common.Editor;

namespace Gley.NavigationSystem.Editor
{
    public class NavigationWindow : EditorWindow
    {
        private IRoadEditorMode[] modes;
        private string[] modeNames;
        private NavigationAssetLocator locator;
        private BakeStatus bakeStatus;
        private RoadDrawPlanner drawPlanner;
        private RoadSceneDrawer sceneDrawer;
        private NavigationEditorPrefs editorPrefs;
        private RoadEditorContext editorContext;
        private NavigationMap targetMap;
        private RoadNetworkAuthoring authoringAsset;
        private HashSet<int> typeFilter;
        private List<int> visibleRoadIds;
        private List<bool> detailedRoads;
        private List<ValidationIssue> validationIssues;
        private Plane[] frustumPlanes;
        private int currentModeIndex;
        private bool showViewFoldout;

        [MenuItem(NavigationWindowProperties.MenuItem, false, 0)]
        private static void OpenWindow()
        {
            WindowLoader.LoadWindow<NavigationWindow>(new NavigationWindowProperties(), new NavigationVersion(), out _);
        }

        private void OnEnable()
        {
            locator = new NavigationAssetLocator();
            bakeStatus = new BakeStatus();
            editorPrefs = new NavigationEditorPrefs();
            drawPlanner = new RoadDrawPlanner();
            sceneDrawer = new RoadSceneDrawer(editorPrefs);
            typeFilter = new HashSet<int>();
            visibleRoadIds = new List<int>();
            detailedRoads = new List<bool>();
            validationIssues = new List<ValidationIssue>();
            editorContext = new RoadEditorContext(editorPrefs, validationIssues);
            modeNames = new string[] { "Draw", "Edit", "Connect", "Validate", "Bake" };
            modes = new IRoadEditorMode[] { new DrawMode(editorContext), new EditMode(), new ConnectMode(), new ValidateMode(), new BakeMode() };
            frustumPlanes = new Plane[6];
            currentModeIndex = 0;

            ResolveTarget();
            modes[currentModeIndex].OnEnter();

            SceneView.duringSceneGui += HandleSceneGUI;
        }

        private void OnGUI()
        {
            if (targetMap == null)
            {
                EditorGUILayout.HelpBox("No map in scene — use the Setup window", MessageType.Info);
                return;
            }

            DrawHeader();
            DrawToolbar();
            DrawViewFoldout();

            modes[currentModeIndex].OnWindowGUI();
        }

        private void OnSelectionChange()
        {
            ResolveTarget();
            Repaint();
        }

        private void ResolveTarget()
        {
            NavigationMap resolved = null;
            if (Selection.activeGameObject != null)
            {
                resolved = Selection.activeGameObject.GetComponent<NavigationMap>();
            }

            if (resolved == null)
            {
                NavigationMap[] maps = UnityEngine.Object.FindObjectsOfType<NavigationMap>();
                if (maps.Length > 0)
                {
                    resolved = maps[0];
                }
            }

            targetMap = resolved;
            ResolveAuthoring();
            MigrateAssets();
            UpdateEditorContext();
        }

        private void ResolveAuthoring()
        {
            if (targetMap == null || targetMap.MapData == null || targetMap.MapData.RoadNetwork == null)
            {
                authoringAsset = null;
                return;
            }

            authoringAsset = locator.FindAuthoringFor(targetMap.MapData.RoadNetwork);
        }

        private void MigrateAssets()
        {
            FormatMigrator migrator = new FormatMigrator(new List<IFormatMigration>());

            NavigationSettings settings = locator.FindOrCreateSettings();
            migrator.MigrateIfNeeded(settings);
            AssignSettingsIfMissing(settings);

            if (targetMap != null && targetMap.MapData != null)
            {
                migrator.MigrateIfNeeded(targetMap.MapData);
            }

            if (authoringAsset != null)
            {
                migrator.MigrateIfNeeded(authoringAsset);
            }

            string[] routeStyleGuids = AssetDatabase.FindAssets("t:RouteStyle");
            for (int i = 0; i < routeStyleGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(routeStyleGuids[i]);
                UnityEngine.Object routeStyleAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                migrator.MigrateIfNeeded(routeStyleAsset);
            }
        }

        private void AssignSettingsIfMissing(NavigationSettings settings)
        {
            if (authoringAsset == null || authoringAsset.Settings != null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(authoringAsset);
            serializedObject.FindProperty("settings").objectReferenceValue = settings;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(authoringAsset);
        }

        private void UpdateEditorContext()
        {
            MapData mapData = null;
            if (targetMap != null)
            {
                mapData = targetMap.MapData;
            }
            editorContext.SetTarget(authoringAsset, mapData);
        }

        private void DrawHeader()
        {
            if (targetMap.MapData == null)
            {
                EditorGUILayout.LabelField("Map", "No map data assigned");
                return;
            }

            EditorGUILayout.LabelField("Map", targetMap.MapData.name);

            if (authoringAsset == null)
            {
                EditorGUILayout.LabelField("Roads", "-");
                EditorGUILayout.LabelField("Bake", "No road network asset");
                return;
            }

            EditorGUILayout.LabelField("Roads", authoringAsset.Roads.Count.ToString());

            if (bakeStatus.IsOutdated(authoringAsset))
            {
                EditorGUILayout.LabelField("Bake", "Outdated");
            }
            else
            {
                EditorGUILayout.LabelField("Bake", "Up to date");
            }
        }

        private void DrawToolbar()
        {
            int newIndex = GUILayout.Toolbar(currentModeIndex, modeNames);
            if (newIndex == currentModeIndex)
            {
                return;
            }

            modes[currentModeIndex].OnExit();
            currentModeIndex = newIndex;
            modes[currentModeIndex].OnEnter();
        }

        private void DrawViewFoldout()
        {
            showViewFoldout = EditorGUILayout.Foldout(showViewFoldout, "View");
            if (!showViewFoldout)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            editorPrefs.ShowRoadLines = EditorGUILayout.Toggle("Road Lines", editorPrefs.ShowRoadLines);
            editorPrefs.ShowDirectionArrows = EditorGUILayout.Toggle("Direction Arrows", editorPrefs.ShowDirectionArrows);
            editorPrefs.ShowRoadTypeColors = EditorGUILayout.Toggle("Road Type Colors", editorPrefs.ShowRoadTypeColors);
            editorPrefs.ShowKeyPoints = EditorGUILayout.Toggle("Key Points", editorPrefs.ShowKeyPoints);
            editorPrefs.ShowDenseShapePoints = EditorGUILayout.Toggle("Dense Shape Points", editorPrefs.ShowDenseShapePoints);
            editorPrefs.ShowIntersections = EditorGUILayout.Toggle("Intersections / Connections", editorPrefs.ShowIntersections);
            editorPrefs.ShowValidationHighlights = EditorGUILayout.Toggle("Validation Highlights", editorPrefs.ShowValidationHighlights);
            editorPrefs.ShowMapRectangle = EditorGUILayout.Toggle("Map Rectangle", editorPrefs.ShowMapRectangle);
            editorPrefs.ShowMapOverlay = EditorGUILayout.Toggle("Map Image Overlay", editorPrefs.ShowMapOverlay);

            DrawTypeFilter();

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawTypeFilter()
        {
            if (authoringAsset == null || authoringAsset.Settings == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Type Filter (none = all)");

            IReadOnlyList<RoadType> roadTypes = authoringAsset.Settings.RoadTypes;
            for (int i = 0; i < roadTypes.Count; i++)
            {
                RoadType roadType = roadTypes[i];
                bool isSelected = typeFilter.Contains(roadType.Id);
                bool newSelected = EditorGUILayout.ToggleLeft(roadType.Name, isSelected);
                if (newSelected == isSelected)
                {
                    continue;
                }

                if (newSelected)
                {
                    typeFilter.Add(roadType.Id);
                }
                else
                {
                    typeFilter.Remove(roadType.Id);
                }
            }
        }

        private void HandleSceneGUI(SceneView sceneView)
        {
            DrawRoadNetwork(sceneView);
            modes[currentModeIndex].OnSceneGUI(sceneView);
        }

        private void DrawRoadNetwork(SceneView sceneView)
        {
            if (authoringAsset == null || sceneView.camera == null)
            {
                return;
            }

            float unitsPerMeter = ResolveUnitsPerMeter();
            GeometryUtility.CalculateFrustumPlanes(sceneView.camera, frustumPlanes);
            Vector3 cameraPosition = sceneView.camera.transform.position;
            float detailDistance = RoadDrawPlanner.DefaultDetailDistance * unitsPerMeter;

            drawPlanner.Plan(authoringAsset, unitsPerMeter, frustumPlanes, cameraPosition, detailDistance, typeFilter, visibleRoadIds, detailedRoads);
            sceneDrawer.Draw(authoringAsset, targetMap.MapData, authoringAsset.Settings, unitsPerMeter, visibleRoadIds, detailedRoads, validationIssues);
        }

        private float ResolveUnitsPerMeter()
        {
            if (authoringAsset.Settings == null)
            {
                return 1f;
            }
            return authoringAsset.Settings.UnitsPerMeter;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= HandleSceneGUI;
            modes[currentModeIndex].OnExit();
        }
    }
}
