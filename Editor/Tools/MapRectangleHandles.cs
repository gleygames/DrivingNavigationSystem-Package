using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class MapRectangleHandles
    {
        private const float HandleSizeFactor = 0.15f;

        private readonly MapRectangleSync sync;

        public MapRectangleHandles()
        {
            sync = new MapRectangleSync();
        }

        public void Draw(MapData data, float worldY, float unitsPerMeter)
        {
            if (data.Locked)
            {
                return;
            }

            bool keepRatio = data.ImageState == MapImageState.Custom;
            MapFrame frame = data.CreateFrame();
            Vector2 size = data.RectangleSize;
            Vector3 centerWorld = new Vector3(data.RectangleCenter.x * unitsPerMeter, worldY, data.RectangleCenter.z * unitsPerMeter);
            float handleSize = HandleUtility.GetHandleSize(centerWorld) * HandleSizeFactor;

            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 cornerMap = GetCornerMap(corner, size);
                Vector3 cornerTrue = frame.MapToTrue(cornerMap, 0f);
                Vector3 cornerWorld = new Vector3(cornerTrue.x * unitsPerMeter, worldY, cornerTrue.z * unitsPerMeter);

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
    }
}
