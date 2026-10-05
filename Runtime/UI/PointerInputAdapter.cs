using UnityEngine;
using UnityEngine.EventSystems;

namespace Gley.NavigationSystem
{
    internal class PointerInputAdapter
    {
        private readonly GestureTracker tracker;
        private readonly MapViewInteractive target;
        private readonly RectTransform viewport;
        private readonly FullMapInteractionSettings settings;

        internal GestureTracker Tracker { get { return tracker; } }

        internal PointerInputAdapter(MapViewInteractive target, RectTransform viewport, FullMapInteractionSettings settings)
        {
            this.target = target;
            this.viewport = viewport;
            this.settings = settings;
            tracker = new GestureTracker(target);
        }

        internal void UpdatePointerInputLogic(float time, float deltaTime)
        {
            ApplySettings();
            tracker.UpdateGestureLogic(time, deltaTime);
        }

        internal void ApplySettings()
        {
            tracker.MouseWheelStep = settings.MouseWheelStep;
            tracker.DoubleTapStep = settings.DoubleTapStep;
            tracker.DoubleTapEnabled = settings.DoubleTapZoom;
            tracker.FlingEnabled = settings.Fling;
        }

        internal void HandlePointerDown(PointerEventData eventData)
        {
            ApplySettings();
            target.NotifyPointerInput();
            tracker.PointerDown(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime);
        }

        internal void HandleBeginDrag(PointerEventData eventData)
        {
            ApplySettings();
            tracker.PointerMove(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime);
        }

        internal void HandleDrag(PointerEventData eventData)
        {
            ApplySettings();
            tracker.PointerMove(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime);
        }

        internal void HandlePointerUp(PointerEventData eventData)
        {
            ApplySettings();
            tracker.PointerUp(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime, !eventData.dragging);
        }

        internal void HandleScroll(PointerEventData eventData)
        {
            ApplySettings();
            target.NotifyPointerInput();
            tracker.Scroll(eventData.scrollDelta.y, ToViewportLocal(eventData));
        }

        private Vector2 ToViewportLocal(PointerEventData eventData)
        {
            Camera eventCamera = eventData.pressEventCamera;
            if (eventCamera == null)
            {
                eventCamera = eventData.enterEventCamera;
            }

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventCamera, out localPoint);
            return localPoint;
        }
    }
}
