using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class MinimapMath
    {
        public float SpeedZoom(float speed, float minMeters, float maxMeters, float slowSpeed, float fastSpeed)
        {
            float t = Mathf.InverseLerp(slowSpeed, fastSpeed, speed);
            return Mathf.Lerp(minMeters, maxMeters, t);
        }

        public float MaxZoomToFitMap(Vector2 mapSize, bool round, Vector2 viewportSize)
        {
            float smallerMapSide = Mathf.Min(mapSize.x, mapSize.y);
            float viewSpan;
            if (round)
            {
                viewSpan = Mathf.Min(viewportSize.x, viewportSize.y);
            }
            else
            {
                viewSpan = viewportSize.magnitude;
            }

            if (viewSpan <= 0f)
            {
                return smallerMapSide;
            }

            return smallerMapSide * viewportSize.x / viewSpan;
        }

        public Vector2 ClampCenter(Vector2 desiredCenter, Vector2 mapSize, float viewHalfExtentMeters, bool round, float rotation, Vector2 viewHalfSizeMeters)
        {
            float halfExtentX;
            float halfExtentY;
            if (round)
            {
                halfExtentX = viewHalfExtentMeters;
                halfExtentY = viewHalfExtentMeters;
            }
            else
            {
                float radians = rotation * Mathf.Deg2Rad;
                float cos = Mathf.Abs(Mathf.Cos(radians));
                float sin = Mathf.Abs(Mathf.Sin(radians));
                halfExtentX = cos * viewHalfSizeMeters.x + sin * viewHalfSizeMeters.y;
                halfExtentY = sin * viewHalfSizeMeters.x + cos * viewHalfSizeMeters.y;
            }

            float x = ClampAxis(desiredCenter.x, mapSize.x, halfExtentX);
            float y = ClampAxis(desiredCenter.y, mapSize.y, halfExtentY);
            return new Vector2(x, y);
        }

        public float SmoothAngle(float current, float target, float smoothTime, ref float velocity, float deltaTime)
        {
            if (smoothTime <= 0f)
            {
                velocity = 0f;
                return Mathf.DeltaAngle(0f, target);
            }

            float result = Mathf.SmoothDampAngle(current, target, ref velocity, smoothTime, Mathf.Infinity, deltaTime);
            return Mathf.DeltaAngle(0f, result);
        }

        public Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }

        private float ClampAxis(float value, float mapLength, float halfExtent)
        {
            if (halfExtent * 2f >= mapLength)
            {
                return mapLength * 0.5f;
            }

            return Mathf.Clamp(value, halfExtent, mapLength - halfExtent);
        }
    }
}
