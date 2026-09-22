using UnityEngine;
using UnityEngine.EventSystems;

namespace Gley.NavigationSystem
{
    [RequireComponent(typeof(MapViewInteractive))]
    public class PointerInputAdapter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        private GestureTracker tracker;
        private MapViewInteractive target;
        private RectTransform viewportRect;

        private void OnEnable()
        {
            target = GetComponent<MapViewInteractive>();
            viewportRect = GetComponent<RectTransform>();
            tracker = new GestureTracker(target);
        }

        private void Update()
        {
            tracker.UpdateGestureLogic(Time.unscaledTime, Time.unscaledDeltaTime);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            target.NotifyPointerInput();
            tracker.PointerDown(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            tracker.PointerMove(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime);
        }

        public void OnDrag(PointerEventData eventData)
        {
            tracker.PointerMove(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            tracker.DoubleTapStep = target.DoubleTapStep;
            tracker.PointerUp(eventData.pointerId, ToViewportLocal(eventData), Time.unscaledTime, !eventData.dragging);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }

        public void OnScroll(PointerEventData eventData)
        {
            target.NotifyPointerInput();
            tracker.MouseWheelStep = target.MouseWheelStep;
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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, eventData.position, eventCamera, out localPoint);
            return localPoint;
        }
    }
}
