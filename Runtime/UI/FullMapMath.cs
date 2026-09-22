using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class FullMapMath
    {
        public float MaxZoomMeters(Vector2 mapSize, Vector2 viewportSize, bool fit)
        {
            float viewportAspect = viewportSize.x / viewportSize.y;
            float widenedByHeight = mapSize.y * viewportAspect;

            if (fit)
            {
                return Mathf.Max(mapSize.x, widenedByHeight);
            }

            return Mathf.Min(mapSize.x, widenedByHeight);
        }

        public Vector2 ClampCenter(Vector2 center, Vector2 mapSize, Vector2 viewHalfSizeMeters)
        {
            float x = ClampAxis(center.x, mapSize.x, viewHalfSizeMeters.x);
            float y = ClampAxis(center.y, mapSize.y, viewHalfSizeMeters.y);
            return new Vector2(x, y);
        }

        public Vector2 ZoomAroundPivot(Vector2 center, Vector2 pivotMap, float oldZoom, float newZoom)
        {
            float ratio = newZoom / oldZoom;
            return pivotMap + (center - pivotMap) * ratio;
        }

        private float ClampAxis(float value, float mapLength, float viewHalfSize)
        {
            if (viewHalfSize * 2f >= mapLength)
            {
                return mapLength * 0.5f;
            }

            return Mathf.Clamp(value, viewHalfSize, mapLength - viewHalfSize);
        }
    }
}
