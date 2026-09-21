using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class MapMode : IRoadEditorMode
    {
        private readonly RoadEditorContext context;
        private readonly MapRectangleHandles rectangleHandles;
        private readonly MapImagePanel mapImagePanel;

        public MapMode(RoadEditorContext context)
        {
            this.context = context;
            rectangleHandles = new MapRectangleHandles();
            mapImagePanel = new MapImagePanel(context.Prefs);
        }

        public void OnEnter()
        {
            SceneView.RepaintAll();
        }

        public void OnExit()
        {
            SceneView.RepaintAll();
        }

        public void OnWindowGUI()
        {
            MapData data = context.MapData;
            if (data == null)
            {
                EditorGUILayout.HelpBox("No map data assigned.", MessageType.Info);
                return;
            }

            if (data.Locked)
            {
                EditorGUILayout.HelpBox("The map area is locked by the captured image. Use Change area below to edit it.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Drag the corner handles in the Scene view to resize the map area. Select the map object to move or rotate it. Road tools are off in this mode.", MessageType.Info);
            }

            EditorGUILayout.LabelField("Map Image", EditorStyles.boldLabel);
            mapImagePanel.Draw(data, context.UnitsPerMeter);
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            MapData data = context.MapData;
            if (data == null || IsMapObjectSelected(data))
            {
                return;
            }

            rectangleHandles.Draw(data, data.RectangleCenter.y, context.UnitsPerMeter);
        }

        private bool IsMapObjectSelected(MapData data)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                return false;
            }

            NavigationMap map = selected.GetComponent<NavigationMap>();
            if (map == null)
            {
                return false;
            }
            return map.MapData == data;
        }

        public void Dispose()
        {
            mapImagePanel.Dispose();
        }
    }
}
