using UnityEngine;

namespace Gley.NavigationSystem
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Rect lastSafeArea;
        private bool hasLastSafeArea;

        private void OnEnable()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            hasLastSafeArea = false;
            ApplySafeArea();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled || rectTransform == null)
            {
                return;
            }
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                return;
            }

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (hasLastSafeArea && safeArea == lastSafeArea)
            {
                return;
            }
            lastSafeArea = safeArea;
            hasLastSafeArea = true;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }
}
