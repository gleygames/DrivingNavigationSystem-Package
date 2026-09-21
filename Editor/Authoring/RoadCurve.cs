using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadCurve
    {
        public Vector3 Evaluate(AuthoringRoad road, int segment, float t)
        {
            AuthoringKeyPoint start = road.KeyPoints[segment];
            AuthoringKeyPoint end = road.KeyPoints[segment + 1];

            Vector3 p0 = start.Position;
            Vector3 p1 = start.Position + start.OutHandle;
            Vector3 p2 = end.Position + end.InHandle;
            Vector3 p3 = end.Position;

            float oneMinusT = 1f - t;
            float a = oneMinusT * oneMinusT * oneMinusT;
            float b = 3f * oneMinusT * oneMinusT * t;
            float c = 3f * oneMinusT * t * t;
            float d = t * t * t;

            return p0 * a + p1 * b + p2 * c + p3 * d;
        }

        public void UpdateAutoHandles(AuthoringRoad road)
        {
            List<AuthoringKeyPoint> keyPoints = road.KeyPoints;
            int lastIndex = keyPoints.Count - 1;

            for (int i = 0; i <= lastIndex; i++)
            {
                AuthoringKeyPoint keyPoint = keyPoints[i];
                if (keyPoint.ManualHandles)
                {
                    continue;
                }

                Vector3 position = keyPoint.Position;
                Vector3 direction;
                if (i == 0)
                {
                    direction = (keyPoints[i + 1].Position - position).normalized;
                }
                else if (i == lastIndex)
                {
                    direction = (position - keyPoints[i - 1].Position).normalized;
                }
                else
                {
                    direction = (keyPoints[i + 1].Position - keyPoints[i - 1].Position).normalized;
                }

                if (i == 0)
                {
                    keyPoint.SetInHandle(Vector3.zero);
                }
                else
                {
                    float distanceToPrevious = Vector3.Distance(position, keyPoints[i - 1].Position);
                    keyPoint.SetInHandle(-direction * (distanceToPrevious / 3f));
                }

                if (i == lastIndex)
                {
                    keyPoint.SetOutHandle(Vector3.zero);
                }
                else
                {
                    float distanceToNext = Vector3.Distance(position, keyPoints[i + 1].Position);
                    keyPoint.SetOutHandle(direction * (distanceToNext / 3f));
                }
            }
        }

        public AuthoringKeyPoint Split(AuthoringRoad road, int segment, float t, out Vector3 newStartOutHandle, out Vector3 newEndInHandle)
        {
            AuthoringKeyPoint start = road.KeyPoints[segment];
            AuthoringKeyPoint end = road.KeyPoints[segment + 1];

            Vector3 p0 = start.Position;
            Vector3 p1 = start.Position + start.OutHandle;
            Vector3 p2 = end.Position + end.InHandle;
            Vector3 p3 = end.Position;

            Vector3 q0 = Vector3.Lerp(p0, p1, t);
            Vector3 q1 = Vector3.Lerp(p1, p2, t);
            Vector3 q2 = Vector3.Lerp(p2, p3, t);
            Vector3 r0 = Vector3.Lerp(q0, q1, t);
            Vector3 r1 = Vector3.Lerp(q1, q2, t);
            Vector3 s = Vector3.Lerp(r0, r1, t);

            newStartOutHandle = q0 - p0;
            newEndInHandle = q2 - p3;

            AuthoringKeyPoint splitPoint = new AuthoringKeyPoint(s);
            splitPoint.SetInHandle(r0 - s);
            splitPoint.SetOutHandle(r1 - s);
            splitPoint.SetManualHandles(true);
            return splitPoint;
        }
    }
}
