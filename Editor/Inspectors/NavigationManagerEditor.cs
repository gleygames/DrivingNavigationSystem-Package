using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [CustomEditor(typeof(NavigationManager))]
    public class NavigationManagerEditor : UnityEditor.Editor
    {
        private readonly string[] visiblePaths =
        {
            "car",
            "carYawOffset",
            "carSpawnedAtRuntime",
            "explicitMap",
            "routeMode",
            "uTurnRule"
        };
        private readonly string[] advancedPaths =
        {
            "shiftSource",
            "startManually",
            "settings"
        };
        private readonly string[] projectPaths =
        {
            "runtime.formatter",
            "runtime.playerMarkerPrefab",
            "runtime.destinationMarkerPrefab",
            "runtime.previewPinPrefab",
            "runtime.avoidMultiplier",
            "runtime.preferMultiplier",
            "runtime.startSnapDistance",
            "runtime.destinationSnapDistance",
            "runtime.arrivalDistance",
            "runtime.turnedAroundDistance",
            "runtime.rerouteCooldown",
            "runtime.minHeadingSpeed",
            "runtime.stoppedSpeed",
            "runtime.teleportDistance",
            "runtime.leaveMargin"
        };
        private readonly RootInspectorDrawer drawer = new RootInspectorDrawer();

        private SerializedObject serializedSettings;
        private NavigationSettings settingsAsset;
        private bool advancedOpen;
        private bool projectOpen;

        internal string[] VisiblePaths { get { return visiblePaths; } }
        internal string[] AdvancedPaths { get { return advancedPaths; } }
        internal string[] ProjectPaths { get { return projectPaths; } }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            drawer.DrawProperty(serializedObject, "car");
            drawer.DrawProperty(serializedObject, "carYawOffset");
            drawer.DrawProperty(serializedObject, "carSpawnedAtRuntime");
            SerializedProperty carSpawnedAtRuntime = serializedObject.FindProperty("carSpawnedAtRuntime");
            if (carSpawnedAtRuntime != null && carSpawnedAtRuntime.boolValue)
            {
                EditorGUILayout.HelpBox("Call SetCar(car) after spawning the car.", MessageType.Info);
            }

            drawer.DrawProperty(serializedObject, "explicitMap", "Map");
            EditorGUILayout.LabelField("Empty = found automatically", EditorStyles.miniLabel);

            drawer.DrawHeader("Routing");
            drawer.DrawProperty(serializedObject, "routeMode");
            drawer.DrawProperty(serializedObject, "uTurnRule");

            advancedOpen = drawer.DrawAdvancedFoldout(advancedOpen);
            if (advancedOpen)
            {
                EditorGUI.indentLevel++;
                drawer.DrawProperties(serializedObject, advancedPaths);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            projectOpen = EditorGUILayout.Foldout(projectOpen, "Project-wide (Navigation Settings)", true);
            if (projectOpen)
            {
                EditorGUI.indentLevel++;
                DrawProjectSettings();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProjectSettings()
        {
            NavigationSettings referencedSettings = serializedObject.FindProperty("settings").objectReferenceValue as NavigationSettings;
            if (referencedSettings == null)
            {
                EditorGUILayout.HelpBox("No Navigation Settings assigned. Built-in defaults are used and markers are not shown.", MessageType.Warning);
                return;
            }

            if (settingsAsset != referencedSettings || serializedSettings == null)
            {
                settingsAsset = referencedSettings;
                serializedSettings = new SerializedObject(settingsAsset);
            }

            EditorGUILayout.HelpBox("Shared by every scene. Stored in " + AssetDatabase.GetAssetPath(settingsAsset) + ".", MessageType.Info);
            serializedSettings.Update();
            drawer.DrawProperties(serializedSettings, projectPaths);
            serializedSettings.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            serializedObject.Update();
            SerializedProperty settingsProperty = serializedObject.FindProperty("settings");
            if (settingsProperty.objectReferenceValue == null)
            {
                settingsProperty.objectReferenceValue = new NavigationAssetLocator().FindOrCreateSettings();
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
