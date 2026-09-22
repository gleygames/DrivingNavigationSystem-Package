using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class OffScreenArrowMath
    {
        public bool ComputeEdgePoint(Vector2 targetInViewport, Vector2 viewportHalfSize, EdgeShape shape, float inset, out Vector2 edgePoint, out float angleDegrees)
        {
            if (shape == EdgeShape.Circle)
            {
                return ComputeCircleEdgePoint(targetInViewport, viewportHalfSize, inset, out edgePoint, out angleDegrees);
            }

            return ComputeRectangleEdgePoint(targetInViewport, viewportHalfSize, inset, out edgePoint, out angleDegrees);
        }

        private bool ComputeCircleEdgePoint(Vector2 targetInViewport, Vector2 viewportHalfSize, float inset, out Vector2 edgePoint, out float angleDegrees)
        {
            float radius = Mathf.Min(viewportHalfSize.x, viewportHalfSize.y) - inset;
            float distance = targetInViewport.magnitude;

            if (distance <= radius)
            {
                edgePoint = targetInViewport;
                angleDegrees = ComputeAngle(targetInViewport);
                return false;
            }

            Vector2 direction = targetInViewport / distance;
            edgePoint = direction * radius;
            angleDegrees = ComputeAngle(direction);
            return true;
        }

        private bool ComputeRectangleEdgePoint(Vector2 targetInViewport, Vector2 viewportHalfSize, float inset, out Vector2 edgePoint, out float angleDegrees)
        {
            Vector2 halfSize = new Vector2(viewportHalfSize.x - inset, viewportHalfSize.y - inset);

            if (Mathf.Abs(targetInViewport.x) <= halfSize.x && Mathf.Abs(targetInViewport.y) <= halfSize.y)
            {
                edgePoint = targetInViewport;
                angleDegrees = ComputeAngle(targetInViewport);
                return false;
            }

            float tx = Mathf.Infinity;
            if (targetInViewport.x != 0f)
            {
                tx = halfSize.x / Mathf.Abs(targetInViewport.x);
            }

            float ty = Mathf.Infinity;
            if (targetInViewport.y != 0f)
            {
                ty = halfSize.y / Mathf.Abs(targetInViewport.y);
            }

            float t = Mathf.Min(tx, ty);
            edgePoint = targetInViewport * t;
            angleDegrees = ComputeAngle(targetInViewport);
            return true;
        }

        private float ComputeAngle(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0f)
            {
                return 0f;
            }

            return Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        }
    }
}
