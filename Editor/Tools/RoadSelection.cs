using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class RoadSelection
    {
        private readonly List<int> roadIds;

        public List<int> RoadIds { get { return roadIds; } }
        public int KeyPointRoadId { get; private set; }
        public int KeyPointIndex { get; private set; }
        public int IntersectionId { get; private set; }

        public bool HasKeyPoint
        {
            get { return KeyPointIndex >= 0; }
        }

        public bool HasIntersection
        {
            get { return IntersectionId != 0; }
        }

        public bool IsEmpty
        {
            get { return roadIds.Count == 0 && !HasKeyPoint && !HasIntersection; }
        }

        public RoadSelection()
        {
            roadIds = new List<int>();
            KeyPointIndex = -1;
        }

        public void Click(SelectionHit hit, bool additive)
        {
            if (!additive)
            {
                Clear();
            }

            if (hit.Kind == SelectionHitKind.Road)
            {
                AddRoad(hit.RoadId);
            }
            else if (hit.Kind == SelectionHitKind.KeyPoint)
            {
                AddRoad(hit.RoadId);
                KeyPointRoadId = hit.RoadId;
                KeyPointIndex = hit.KeyPointIndex;
            }
            else if (hit.Kind == SelectionHitKind.Intersection)
            {
                IntersectionId = hit.IntersectionId;
            }
        }

        public void BoxSelect(Rect guiRect, List<int> roadIds, List<Vector2> roadGuiPoints, List<int> roadGuiPointStarts, bool additive)
        {
            if (!additive)
            {
                Clear();
            }

            for (int i = 0; i < roadIds.Count; i++)
            {
                int start = roadGuiPointStarts[i];
                int end = roadGuiPoints.Count;
                if (i + 1 < roadGuiPointStarts.Count)
                {
                    end = roadGuiPointStarts[i + 1];
                }

                for (int j = start; j < end; j++)
                {
                    if (guiRect.Contains(roadGuiPoints[j]))
                    {
                        AddRoad(roadIds[i]);
                        break;
                    }
                }
            }
        }

        public bool Contains(int roadId)
        {
            return roadIds.Contains(roadId);
        }

        public void RemoveRoad(int roadId)
        {
            roadIds.Remove(roadId);
            if (KeyPointRoadId == roadId)
            {
                ClearKeyPoint();
            }
        }

        public void ClearKeyPoint()
        {
            KeyPointRoadId = 0;
            KeyPointIndex = -1;
        }

        public void ClearIntersection()
        {
            IntersectionId = 0;
        }

        public void Clear()
        {
            roadIds.Clear();
            ClearKeyPoint();
            ClearIntersection();
        }

        private void AddRoad(int roadId)
        {
            if (!roadIds.Contains(roadId))
            {
                roadIds.Add(roadId);
            }
        }
    }
}
