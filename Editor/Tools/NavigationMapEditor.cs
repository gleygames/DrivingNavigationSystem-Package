using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [CustomEditor(typeof(NavigationMap))]
    public class NavigationMapEditor : UnityEditor.Editor
    {
        private MapRectangleSync sync;
        private MapRectangleHandles rectangleHandles;
        private MapOverlayDrawer overlayDrawer;

        private void OnEnable()
        {
            sync = new MapRectangleSync();
            rectangleHandles = new MapRectangleHandles();
            overlayDrawer = new MapOverlayDrawer(new NavigationEditorPrefs());
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty mapDataProperty = serializedObject.FindProperty("mapData");
            EditorGUILayout.PropertyField(mapDataProperty);

            serializedObject.ApplyModifiedProperties();

            NavigationMap map = (NavigationMap)target;
            MapData data = map.MapData;
            if (data == null)
            {
                return;
            }

            DrawRectangleFields(data);
            DrawChangeAreaButton(data);
        }

        private void DrawRectangleFields(MapData data)
        {
            bool locked = data.Locked;
            EditorGUILayout.LabelField("Locked", locked.ToString());

            EditorGUI.BeginDisabledGroup(locked);
            Vector2 newSize = EditorGUILayout.Vector2Field("Rectangle Size", data.RectangleSize);
            float newRotation = EditorGUILayout.FloatField("Rotation Y", data.RectangleRotationY);
            EditorGUI.EndDisabledGroup();

            if (locked)
            {
                return;
            }

            bool sizeChanged = newSize != data.RectangleSize;
            bool rotationChanged = !Mathf.Approximately(newRotation, data.RectangleRotationY);
            if (!sizeChanged && !rotationChanged)
            {
                return;
            }

            Undo.RecordObject(data, "Edit Map Rectangle");
            data.SetRectangleSize(newSize);
            data.SetRectangleRotationY(newRotation);
            EditorUtility.SetDirty(data);
        }

        private void DrawChangeAreaButton(MapData data)
        {
            if (!data.Locked || data.ImageState != MapImageState.Captured)
            {
                return;
            }

            if (!GUILayout.Button("Change area"))
            {
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Change Map Area",
                "This unlocks the rectangle and marks the captured image outdated. You will need to recapture.",
                "Change area",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            Undo.RecordObject(data, "Change Map Area");
            data.SetLocked(false);
            data.SetImageState(MapImageState.Outdated);
            EditorUtility.SetDirty(data);
        }

        private void OnSceneGUI()
        {
            NavigationMap map = (NavigationMap)target;
            MapData data = map.MapData;
            if (data == null)
            {
                return;
            }

            float unitsPerMeter = ResolveUnitsPerMeter();
            rectangleHandles.Draw(data, map.transform.position.y, unitsPerMeter);
            ApplySceneTransformChange(map, data, unitsPerMeter);

            overlayDrawer.DrawSceneOverlay(map, unitsPerMeter);
        }

        private float ResolveUnitsPerMeter()
        {
            string[] guids = AssetDatabase.FindAssets("t:NavigationSettings");
            if (guids.Length == 0)
            {
                return 1f;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            NavigationSettings settings = AssetDatabase.LoadAssetAtPath<NavigationSettings>(path);
            if (settings == null)
            {
                return 1f;
            }
            return settings.UnitsPerMeter;
        }

        private void ApplySceneTransformChange(NavigationMap map, MapData data, float unitsPerMeter)
        {
            Undo.RecordObject(data, "Move Map Rectangle");

            MapRectangleSyncResult result = sync.ApplyTransformChange(map.transform, data, unitsPerMeter);
            if (result == MapRectangleSyncResult.NoChange)
            {
                return;
            }

            EditorUtility.SetDirty(data);
            if (result == MapRectangleSyncResult.Reverted)
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Map area is locked, use Change area"));
            }
        }

        private void OnDisable()
        {
            overlayDrawer.Dispose();
        }
    }
}
