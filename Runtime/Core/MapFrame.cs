using UnityEngine;

namespace Gley.NavigationSystem
{
    public class MapFrame
    {
        private readonly float cosRotation;
        private readonly float sinRotation;
        private readonly float halfWidth;
        private readonly float halfHeight;

        public Vector3 Center { get; }
        public Vector2 Size { get; }
        public float RotationY { get; }

        public MapFrame(Vector3 center, Vector2 size, float rotationY)
        {
            Center = center;
            Size = size;
            RotationY = rotationY;

            float radians = rotationY * Mathf.Deg2Rad;
            cosRotation = Mathf.Cos(radians);
            sinRotation = Mathf.Sin(radians);
            halfWidth = size.x * 0.5f;
            halfHeight = size.y * 0.5f;
        }

        public Vector2 TrueToMap(Vector3 truePos)
        {
            float relativeX = truePos.x - Center.x;
            float relativeZ = truePos.z - Center.z;

            float localX = relativeX * cosRotation - relativeZ * sinRotation;
            float localZ = relativeX * sinRotation + relativeZ * cosRotation;

            return new Vector2(localX + halfWidth, localZ + halfHeight);
        }

        public Vector3 MapToTrue(Vector2 mapPos, float y)
        {
            float localX = mapPos.x - halfWidth;
            float localZ = mapPos.y - halfHeight;

            float relativeX = localX * cosRotation + localZ * sinRotation;
            float relativeZ = -localX * sinRotation + localZ * cosRotation;

            return new Vector3(Center.x + relativeX, y, Center.z + relativeZ);
        }

        public Vector2 TrueDirectionToMap(Vector3 dir)
        {
            float localX = dir.x * cosRotation - dir.z * sinRotation;
            float localZ = dir.x * sinRotation + dir.z * cosRotation;

            return new Vector2(localX, localZ).normalized;
        }

        public Vector3 MapDirectionToTrue(Vector2 dir)
        {
            float relativeX = dir.x * cosRotation + dir.y * sinRotation;
            float relativeZ = -dir.x * sinRotation + dir.y * cosRotation;

            return new Vector3(relativeX, 0f, relativeZ).normalized;
        }

        public Vector2 MapToNormalized(Vector2 mapPos)
        {
            return new Vector2(mapPos.x / Size.x, mapPos.y / Size.y);
        }

        public bool ContainsMap(Vector2 mapPos)
        {
            if (mapPos.x < 0f || mapPos.x > Size.x)
            {
                return false;
            }
            if (mapPos.y < 0f || mapPos.y > Size.y)
            {
                return false;
            }
            return true;
        }

        public float HeadingToMapAngle(Vector3 trueDir)
        {
            Vector2 mapDir = TrueDirectionToMap(trueDir);
            float angle = Mathf.Atan2(mapDir.x, mapDir.y) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }
            return angle;
        }
    }
}
