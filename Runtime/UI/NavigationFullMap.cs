using System;
using Gley.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(99)]
    [RequireComponent(typeof(RectTransform))]
    public class NavigationFullMap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] private NavigationManager manager;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private MapViewSettings viewSettings = new MapViewSettings(MapViewSettings.FullMapChannelBit, true, true);
        [SerializeField] private FullMapInteractionSettings interactionSettings = new FullMapInteractionSettings();
        [SerializeField] private Image crosshairImage;
        [SerializeField] private PreviewPanelSlots previewPanel = new PreviewPanelSlots();
        [SerializeField] private FullMapButtons buttons = new FullMapButtons();
        private NavigationManager cachedManager;
        private MapView view;
        private MapViewInteractive interactive;
        private PointerInputAdapter pointerInput;
        private PreviewPanel panel;
        private NavigationControls controls;
        private bool partsEnabled;

        public event Action Opened;
        public event Action Closed;

        public MapView View { get { return view; } }
        public MapViewInteractive Interactive { get { return interactive; } }
        public MapViewSettings ViewSettings { get { return viewSettings; } }
        public FullMapInteractionSettings InteractionSettings { get { return interactionSettings; } }
        public PreviewPanelSlots PreviewPanelSlots { get { return previewPanel; } }
        public FullMapButtons Buttons { get { return buttons; } }
        internal RectTransform Viewport { get { return viewport; } }

        private void OnEnable()
        {
            if (!EnsureParts())
            {
                return;
            }

            cachedManager = FindManager();
            if (cachedManager == null)
            {
                CustomLogger.LogError("NavigationFullMap on '" + name + "': no NavigationManager found. Load the UI after the Navigation Manager.", this);
            }

            view.Enable(cachedManager);
            interactive.Enable();
            panel.Enable(cachedManager);
            controls.Enable(cachedManager);
            partsEnabled = true;
        }

        private void Update()
        {
            UpdateFullMapInputLogic(Time.unscaledTime, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            UpdateFullMapVisuals(Time.unscaledDeltaTime);
        }

        public void UpdateFullMapInputLogic(float time, float deltaTime)
        {
            if (!partsEnabled || !interactionSettings.BuiltInPointerInput)
            {
                return;
            }

            pointerInput.UpdatePointerInputLogic(time, deltaTime);
        }

        public void UpdateFullMapVisuals(float deltaTime)
        {
            if (!partsEnabled)
            {
                return;
            }

            interactive.UpdateInteractiveMapLogic(deltaTime);
            view.UpdateMapViewVisuals(deltaTime);
            controls.UpdateNavigationControlsVisuals();
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void CenterOnCar()
        {
            if (interactive != null)
            {
                interactive.CenterOnCar();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanHandlePointer())
            {
                return;
            }
            pointerInput.HandlePointerDown(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanHandlePointer())
            {
                return;
            }
            pointerInput.HandleBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!CanHandlePointer())
            {
                return;
            }
            pointerInput.HandleDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanHandlePointer())
            {
                return;
            }
            pointerInput.HandlePointerUp(eventData);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!CanHandlePointer())
            {
                return;
            }
            pointerInput.HandleScroll(eventData);
        }

        internal void SetManager(NavigationManager value)
        {
            manager = value;
        }

        internal void SetViewport(RectTransform value)
        {
            viewport = value;
        }

        internal void SetCrosshairImage(Image value)
        {
            crosshairImage = value;
        }

        internal void NotifyOpened()
        {
            if (Opened != null)
            {
                Opened();
            }
        }

        private bool EnsureParts()
        {
            if (view != null)
            {
                return true;
            }

            if (viewport == null)
            {
                CustomLogger.LogError("NavigationFullMap on '" + name + "': Viewport is not assigned.", this);
                return false;
            }

            view = new MapView(this, viewport, viewSettings);
            interactive = new MapViewInteractive(this, view, interactionSettings, crosshairImage);
            pointerInput = new PointerInputAdapter(interactive, viewport, interactionSettings);
            panel = new PreviewPanel(previewPanel, viewSettings);
            controls = new NavigationControls(this, interactive, buttons);
            return true;
        }

        private NavigationManager FindManager()
        {
            if (manager != null)
            {
                return manager;
            }
            if (cachedManager != null)
            {
                return cachedManager;
            }
            return FindAnyObjectByType<NavigationManager>();
        }

        private bool CanHandlePointer()
        {
            return partsEnabled && interactionSettings.BuiltInPointerInput;
        }

        private void OnDisable()
        {
            if (!partsEnabled)
            {
                return;
            }

            controls.Disable();
            panel.Disable();
            interactive.Disable();
            view.Disable();
            partsEnabled = false;
            cachedManager = null;
            if (Closed != null)
            {
                Closed();
            }
        }
    }
}
