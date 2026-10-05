using Gley.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(99)]
    [RequireComponent(typeof(RectTransform))]
    public class NavigationMinimap : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private MinimapFollowSettings followSettings = new MinimapFollowSettings();
        [SerializeField] private MinimapShapeSettings shapeSettings = new MinimapShapeSettings();
        [SerializeField] private MinimapTapAction tapAction = MinimapTapAction.OpenFullMap;
        [SerializeField] private NavigationFullMap fullMap;
        [SerializeField] private Button compassButton;
        [SerializeField] private RectTransform compassIcon;
        private MapView view;
        private MapViewFollowCar followCar;
        private MinimapShape shape;
        private MinimapCompass compass;
        private bool partsEnabled;

        public MapView View { get { return view; } }
        public MinimapFollowSettings FollowSettings { get { return followSettings; } }
        public MinimapShapeSettings ShapeSettings { get { return shapeSettings; } }
        public MinimapRotationMode RotationMode { get { return followSettings.RotationMode; } }
        public MinimapTapAction TapAction { get { return tapAction; } }
        internal RectTransform Viewport { get { return viewport; } }
        internal MapViewFollowCar FollowCar { get { return followCar; } }

        private void OnEnable()
        {
            if (!EnsureParts())
            {
                return;
            }

            ApplyShape();
            view.SetEdgeShape(shapeSettings.Outline);
            followCar.Enable();
            compass.Enable();
            partsEnabled = true;
        }

        private void LateUpdate()
        {
            UpdateMinimapVisuals(Time.unscaledDeltaTime);
        }

        public void UpdateMinimapVisuals(float deltaTime)
        {
            if (!partsEnabled)
            {
                return;
            }

            view.SetEdgeShape(shapeSettings.Outline);
            followCar.UpdateFollowCarVisuals(deltaTime);
            compass.UpdateCompassVisuals();
        }

        public void SetRotationMode(MinimapRotationMode value)
        {
            followSettings.SetRotationMode(value);
        }

        public void ToggleRotationMode()
        {
            followSettings.ToggleRotationMode();
        }

        public void SetTapAction(MinimapTapAction value)
        {
            tapAction = value;
        }

        public void SetFullMap(NavigationFullMap value)
        {
            fullMap = value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (tapAction != MinimapTapAction.OpenFullMap)
            {
                return;
            }

            if (fullMap == null)
            {
                fullMap = FindAnyObjectByType<NavigationFullMap>(FindObjectsInactive.Include);
            }

            if (fullMap == null)
            {
                CustomLogger.LogError("NavigationMinimap on '" + name + "': no full map found in the loaded scenes.", this);
                return;
            }

            fullMap.Open();
        }

        internal void SetViewport(RectTransform value)
        {
            viewport = value;
        }

        internal void SetCompass(Button button, RectTransform icon)
        {
            compassButton = button;
            compassIcon = icon;
        }

        internal void ApplyShape()
        {
            if (viewport == null)
            {
                return;
            }

            if (shape == null)
            {
                shape = new MinimapShape(viewport, shapeSettings);
            }
            shape.Apply();
        }

        private bool EnsureParts()
        {
            if (view != null)
            {
                return true;
            }

            if (viewport == null)
            {
                CustomLogger.LogError("NavigationMinimap on '" + name + "': Viewport is not assigned.", this);
                return false;
            }

            view = viewport.GetComponent<MapView>();
            if (view == null)
            {
                CustomLogger.LogError("NavigationMinimap on '" + name + "': no MapView on the Viewport.", this);
                return false;
            }

            followCar = new MapViewFollowCar(view, followSettings, shapeSettings);
            if (shape == null)
            {
                shape = new MinimapShape(viewport, shapeSettings);
            }
            compass = new MinimapCompass(compassButton, compassIcon, view, followSettings);
            return true;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }
            UnityEditor.EditorApplication.delayCall += DelayedApplyShape;
#endif
        }

#if UNITY_EDITOR
        private void DelayedApplyShape()
        {
            if (this == null)
            {
                return;
            }
            ApplyShape();
        }
#endif

        private void OnDisable()
        {
            if (!partsEnabled)
            {
                return;
            }

            compass.Disable();
            followCar.Disable();
            partsEnabled = false;
        }
    }
}
