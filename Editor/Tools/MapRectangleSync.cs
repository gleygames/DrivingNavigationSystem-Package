using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class MapRectangleSync
    {
        public bool SnapObjectToAsset(Transform t, MapData data, float unitsPerMeter)
        {
            Vector3 position = t.position;
            position.x = data.RectangleCenter.x * unitsPerMeter;
            position.z = data.RectangleCenter.z * unitsPerMeter;
            t.position = position;
            t.rotation = Quaternion.Euler(0f, data.RectangleRotationY, 0f);
            t.localScale = Vector3.one;

            return WriteEditTimePosition(data, t.position);
        }

        public void SnapSceneObjectsToAsset(MapData data, float unitsPerMeter, string undoName)
        {
            NavigationMap[] maps = Object.FindObjectsOfType<NavigationMap>(true);
            for (int i = 0; i < maps.Length; i++)
            {
                NavigationMap map = maps[i];
                if (map.MapData != data)
                {
                    continue;
                }

                Undo.RecordObject(map.transform, undoName);
                SnapObjectToAsset(map.transform, data, unitsPerMeter);
            }
        }

        public MapRectangleSyncResult ApplyTransformChange(Transform t, MapData data, float unitsPerMeter)
        {
            t.localScale = Vector3.one;

            Vector3 position = t.position;
            float assetX = data.RectangleCenter.x * unitsPerMeter;
            float assetZ = data.RectangleCenter.z * unitsPerMeter;
            float rotationY = t.eulerAngles.y;

            bool xzChanged = !Mathf.Approximately(position.x, assetX) || !Mathf.Approximately(position.z, assetZ);
            bool rotationChanged = !Mathf.Approximately(rotationY, data.RectangleRotationY);
            bool structuralChanged = xzChanged || rotationChanged;
            bool yChanged = !Mathf.Approximately(position.y, data.RectangleCenter.y);

            if (!structuralChanged && !yChanged)
            {
                return MapRectangleSyncResult.NoChange;
            }

            if (structuralChanged && data.Locked)
            {
                position.x = assetX;
                position.z = assetZ;
                t.position = position;
                t.rotation = Quaternion.Euler(0f, data.RectangleRotationY, 0f);

                Vector3 revertedCenter = data.RectangleCenter;
                revertedCenter.y = position.y;
                data.SetRectangleCenter(revertedCenter);
                data.SetEditTimeWorldPosition(position);

                return MapRectangleSyncResult.Reverted;
            }

            Vector3 newCenter = data.RectangleCenter;
            newCenter.x = position.x / unitsPerMeter;
            newCenter.z = position.z / unitsPerMeter;
            newCenter.y = position.y;
            data.SetRectangleCenter(newCenter);
            data.SetRectangleRotationY(rotationY);
            data.SetEditTimeWorldPosition(position);

            return MapRectangleSyncResult.Written;
        }

        public void ResizeFromCorner(MapData data, int corner, Vector2 newCornerMap, bool keepRatio)
        {
            float oldWidth = data.RectangleSize.x;
            float oldHeight = data.RectangleSize.y;

            int opposite = (corner + 2) % 4;
            bool oppositeRight = IsRightCorner(opposite);
            bool oppositeTop = IsTopCorner(opposite);

            Vector2 oppositeMap = new Vector2(Extent(oppositeRight, oldWidth), Extent(oppositeTop, oldHeight));

            MapFrame oldFrame = data.CreateFrame();
            Vector3 oppositeTrue = oldFrame.MapToTrue(oppositeMap, data.RectangleCenter.y);

            float newWidth = Mathf.Abs(newCornerMap.x - oppositeMap.x);
            float newHeight = Mathf.Abs(newCornerMap.y - oppositeMap.y);

            if (keepRatio)
            {
                float scaleX = newWidth / oldWidth;
                float scaleY = newHeight / oldHeight;
                float scale;
                if (Mathf.Abs(scaleX - 1f) >= Mathf.Abs(scaleY - 1f))
                {
                    scale = scaleX;
                }
                else
                {
                    scale = scaleY;
                }
                newWidth = oldWidth * scale;
                newHeight = oldHeight * scale;
            }

            float newHalfWidth = newWidth * 0.5f;
            float newHalfHeight = newHeight * 0.5f;

            float localOffsetX;
            if (oppositeRight)
            {
                localOffsetX = newHalfWidth;
            }
            else
            {
                localOffsetX = -newHalfWidth;
            }

            float localOffsetZ;
            if (oppositeTop)
            {
                localOffsetZ = newHalfHeight;
            }
            else
            {
                localOffsetZ = -newHalfHeight;
            }

            float radians = data.RectangleRotationY * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            float rotatedOffsetX = localOffsetX * cos + localOffsetZ * sin;
            float rotatedOffsetZ = -localOffsetX * sin + localOffsetZ * cos;

            Vector3 newCenter = data.RectangleCenter;
            newCenter.x = oppositeTrue.x - rotatedOffsetX;
            newCenter.z = oppositeTrue.z - rotatedOffsetZ;

            data.SetRectangleCenter(newCenter);
            data.SetRectangleSize(new Vector2(newWidth, newHeight));
        }

        private bool WriteEditTimePosition(MapData data, Vector3 position)
        {
            if (data.EditTimeWorldPosition == position)
            {
                return false;
            }

            data.SetEditTimeWorldPosition(position);
            return true;
        }

        private bool IsRightCorner(int corner)
        {
            if (corner == 1 || corner == 2)
            {
                return true;
            }
            return false;
        }

        private bool IsTopCorner(int corner)
        {
            if (corner == 2 || corner == 3)
            {
                return true;
            }
            return false;
        }

        private float Extent(bool atFarSide, float size)
        {
            if (atFarSide)
            {
                return size;
            }
            return 0f;
        }
    }
}
