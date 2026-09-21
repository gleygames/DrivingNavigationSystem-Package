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
        private NavigationMap targetMap;
        private RoadNetworkAuthoring authoringAsset;
        private int currentModeIndex;

        [MenuItem(NavigationWindowProperties.MenuItem, false, 0)]
        private static void OpenWindow()
        {
            WindowLoader.LoadWindow<NavigationWindow>(new NavigationWindowProperties(), new NavigationVersion(), out _);
        }

        private void OnEnable()
        {
            modeNames = new string[] { "Draw", "Edit", "Connect", "Validate", "Bake" };
            modes = new IRoadEditorMode[] { new DrawMode(), new EditMode(), new ConnectMode(), new ValidateMode(), new BakeMode() };
            locator = new NavigationAssetLocator();
            bakeStatus = new BakeStatus();
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

        private void HandleSceneGUI(SceneView sceneView)
        {
            modes[currentModeIndex].OnSceneGUI(sceneView);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= HandleSceneGUI;
            modes[currentModeIndex].OnExit();
        }
    }
}
