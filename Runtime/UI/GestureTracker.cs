using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    internal readonly struct GestureHistorySample
    {
        public GestureHistorySample(Vector2 position, float time)
        {
            Position = position;
            Time = time;
        }

        public Vector2 Position { get; }
        public float Time { get; }
    }

    internal class GesturePointer
    {
        public Vector2 Position { get; set; }
        public int Id { get; set; }
    }

    internal class GestureTracker
    {
        private const float DoubleTapWindowSeconds = 0.25f;
        private const float DoubleTapRadius = 40f;
        private const float FlingHistorySeconds = 0.1f;
        private const float FlingMinSpeed = 500f;
        private const float FlingStopSpeed = 20f;
        private const float FlingHalfLifeSeconds = 0.15f;

        private readonly List<GesturePointer> pointers = new List<GesturePointer>();
        private readonly List<GestureHistorySample> panHistory = new List<GestureHistorySample>();
        private readonly IMapGestureTarget target;

        private Vector2 pendingTapPosition;
        private Vector2 flingVelocity;
        private float pendingTapTime;
        private bool hasPendingTap;
        private bool isFlinging;

        public float MouseWheelStep { get; set; } = 1.25f;
        public float DoubleTapStep { get; set; } = 2f;
        public bool DoubleTapEnabled { get; set; } = true;
        public bool FlingEnabled { get; set; } = true;

        public GestureTracker(IMapGestureTarget target)
        {
            this.target = target;
        }

        public void UpdateGestureLogic(float time, float deltaTime)
        {
            if (hasPendingTap && time - pendingTapTime >= DoubleTapWindowSeconds)
            {
                hasPendingTap = false;
                target.Tap(pendingTapPosition);
            }

            if (isFlinging)
            {
                target.Pan(flingVelocity * deltaTime);
                flingVelocity *= Mathf.Pow(0.5f, deltaTime / FlingHalfLifeSeconds);
                if (flingVelocity.magnitude < FlingStopSpeed)
                {
                    isFlinging = false;
                    flingVelocity = Vector2.zero;
                }
            }
        }

        public void PointerDown(int id, Vector2 pos, float time)
        {
            StopFling();

            GesturePointer pointer = new GesturePointer();
            pointer.Position = pos;
            pointer.Id = id;
            pointers.Add(pointer);

            panHistory.Clear();
            if (pointers.Count == 1)
            {
                RecordHistory(pos, time);
            }
        }

        public void PointerMove(int id, Vector2 pos, float time)
        {
            int index = FindPointerIndex(id);
            if (index < 0)
            {
                return;
            }

            Vector2 previous = pointers[index].Position;
            pointers[index].Position = pos;

            if (pointers.Count == 1)
            {
                target.Pan(pos - previous);
                RecordHistory(pos, time);
            }
            else if (pointers.Count == 2)
            {
                int otherIndex;
                if (index == 0)
                {
                    otherIndex = 1;
                }
                else
                {
                    otherIndex = 0;
                }

                Vector2 otherPos = pointers[otherIndex].Position;
                float previousDistance = Vector2.Distance(previous, otherPos);
                float currentDistance = Vector2.Distance(pos, otherPos);
                Vector2 previousMidpoint = (previous + otherPos) * 0.5f;
                Vector2 currentMidpoint = (pos + otherPos) * 0.5f;

                if (previousDistance > 0f)
                {
                    target.Zoom(currentDistance / previousDistance, currentMidpoint);
                }

                target.Pan(currentMidpoint - previousMidpoint);
            }
        }

        public void PointerUp(int id, Vector2 pos, float time, bool wasClick)
        {
            int index = FindPointerIndex(id);
            if (index < 0)
            {
                return;
            }

            bool wasSinglePointerDrag = pointers.Count == 1 && !wasClick;
            Vector2 flingVelocityCandidate = Vector2.zero;
            if (wasSinglePointerDrag)
            {
                flingVelocityCandidate = ComputeFlingVelocity(pos, time);
            }

            pointers.RemoveAt(index);
            panHistory.Clear();

            if (wasClick)
            {
                HandleTap(pos, time);
                return;
            }

            if (wasSinglePointerDrag && FlingEnabled && flingVelocityCandidate.magnitude >= FlingMinSpeed)
            {
                isFlinging = true;
                flingVelocity = flingVelocityCandidate;
            }
        }

        public void Scroll(float notches, Vector2 pos)
        {
            float factor = Mathf.Pow(MouseWheelStep, notches);
            target.Zoom(factor, pos);
        }

        private void StopFling()
        {
            isFlinging = false;
            flingVelocity = Vector2.zero;
        }

        private void RecordHistory(Vector2 pos, float time)
        {
            panHistory.Add(new GestureHistorySample(pos, time));
            TrimHistory(time);
        }

        private void TrimHistory(float currentTime)
        {
            while (panHistory.Count > 1 && panHistory[0].Time < currentTime - FlingHistorySeconds)
            {
                panHistory.RemoveAt(0);
            }
        }

        private int FindPointerIndex(int id)
        {
            for (int i = 0; i < pointers.Count; i++)
            {
                if (pointers[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private Vector2 ComputeFlingVelocity(Vector2 currentPos, float currentTime)
        {
            TrimHistory(currentTime);
            if (panHistory.Count == 0)
            {
                return Vector2.zero;
            }

            GestureHistorySample oldest = panHistory[0];
            float elapsed = currentTime - oldest.Time;
            if (elapsed <= 0f)
            {
                return Vector2.zero;
            }

            return (currentPos - oldest.Position) / elapsed;
        }

        private void HandleTap(Vector2 pos, float time)
        {
            if (!DoubleTapEnabled)
            {
                target.Tap(pos);
                return;
            }

            if (hasPendingTap && time - pendingTapTime <= DoubleTapWindowSeconds && Vector2.Distance(pos, pendingTapPosition) <= DoubleTapRadius)
            {
                hasPendingTap = false;
                target.Zoom(DoubleTapStep, pos);
                return;
            }

            hasPendingTap = true;
            pendingTapPosition = pos;
            pendingTapTime = time;
        }
    }
}
