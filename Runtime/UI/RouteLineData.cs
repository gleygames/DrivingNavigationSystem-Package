using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class RouteLineData
    {
        private const float MinDottedPieceDistance = 1f;

        private readonly List<Vector3> truePoints = new List<Vector3>();

        public void Convert(Route route, MapFrame frame, List<Vector2> outPoints, List<float> outDistances, List<bool> outDashed)
        {
            outPoints.Clear();
            outDistances.Clear();
            outDashed.Clear();

            route.GetTruePoints(truePoints);
            if (truePoints.Count == 0)
            {
                return;
            }

            Vector2 previousPoint = frame.TrueToMap(truePoints[0]);
            outPoints.Add(previousPoint);
            outDistances.Add(0f);

            float cumulative = 0f;
            for (int i = 1; i < truePoints.Count; i++)
            {
                Vector2 point = frame.TrueToMap(truePoints[i]);
                cumulative += Vector2.Distance(previousPoint, point);
                outPoints.Add(point);
                outDistances.Add(cumulative);
                outDashed.Add(false);
                previousPoint = point;
            }

            AppendDottedPiece(route, frame, previousPoint, cumulative, outPoints, outDistances, outDashed);
        }

        private void AppendDottedPiece(Route route, MapFrame frame, Vector2 lastPoint, float lastDistance, List<Vector2> outPoints, List<float> outDistances, List<bool> outDashed)
        {
            Vector2 destination = frame.TrueToMap(route.Destination);
            float gap = Vector2.Distance(lastPoint, destination);
            if (gap <= MinDottedPieceDistance)
            {
                return;
            }

            outPoints.Add(destination);
            outDistances.Add(lastDistance + gap);
            outDashed.Add(true);
        }
    }
}
