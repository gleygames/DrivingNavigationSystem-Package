using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [CustomEditor(typeof(NavigationMap))]
    public class NavigationMapEditor : UnityEditor.Editor
    {
        private MapRectangleSync sync;
        private MapOverlayDrawer overlayDrawer;

        private void OnEnable()
        {
            sync = new MapRectangleSync();
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
            DrawCornerHandles(map, data, unitsPerMeter);
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

        private void DrawCornerHandles(NavigationMap map, MapData data, float unitsPerMeter)
        {
            if (data.Locked)
            {
                return;
            }

            bool keepRatio = data.ImageState == MapImageState.Custom;
            MapFrame frame = data.CreateFrame();
            Vector2 size = data.RectangleSize;
            float handleSize = HandleUtility.GetHandleSize(map.transform.position) * 0.15f;

            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 cornerMap = GetCornerMap(corner, size);
                Vector3 cornerTrue = frame.MapToTrue(cornerMap, 0f);
                Vector3 cornerWorld = new Vector3(cornerTrue.x * unitsPerMeter, map.transform.position.y, cornerTrue.z * unitsPerMeter);

                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.FreeMoveHandle(cornerWorld, handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (!EditorGUI.EndChangeCheck())
                {
                    continue;
                }

                Vector3 newTrue = new Vector3(newWorld.x / unitsPerMeter, 0f, newWorld.z / unitsPerMeter);
                Vector2 newCornerMap = frame.TrueToMap(newTrue);

                Undo.RecordObject(data, "Resize Map Rectangle");
                sync.ResizeFromCorner(data, corner, newCornerMap, keepRatio);
                EditorUtility.SetDirty(data);
            }
        }

        private Vector2 GetCornerMap(int corner, Vector2 size)
        {
            if (corner == 0)
            {
                return new Vector2(0f, 0f);
            }
            if (corner == 1)
            {
                return new Vector2(size.x, 0f);
            }
            if (corner == 2)
            {
                return new Vector2(size.x, size.y);
            }
            return new Vector2(0f, size.y);
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
