using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(99)]
    [RequireComponent(typeof(MapView))]
    public class MapViewInteractive : MonoBehaviour, IMapGestureTarget
    {
        private const float GamepadPanSpeedFraction = 0.5f;
        private const float GamepadZoomSpeedPerSecond = 2f;

        private readonly FullMapMath math = new FullMapMath();
        private readonly CrosshairModeLogic crosshairLogic = new CrosshairModeLogic();

        private MapView view;
        private NavigationManager manager;
        [SerializeField] private FullMapZoomOutMode zoomOutMode = FullMapZoomOutMode.Fit;
        [SerializeField] private CrosshairMode crosshairMode = CrosshairMode.Auto;
        [SerializeField] private Image crosshairImage;
        [SerializeField] private float openZoomMeters = 1000f;
        [SerializeField] private float mouseWheelStep = 1.25f;
        [SerializeField] private float doubleTapStep = 2f;
        [SerializeField] private float markerTapRadius = 40f;
        [SerializeField] private bool confirmStep = true;
        private bool isFollowingCar;
        private bool needsSnap = true;
        private bool crosshairVisible;

        public event Action Opened;
        public event Action Closed;

        public FullMapZoomOutMode ZoomOutMode { get { return zoomOutMode; } }
        public CrosshairMode CrosshairMode { get { return crosshairMode; } }
        public float ZoomMeters { get { return view.ZoomMeters; } }
        public float OpenZoomMeters { get { return openZoomMeters; } }
        public float MouseWheelStep { get { return mouseWheelStep; } }
        public float DoubleTapStep { get { return doubleTapStep; } }
        public float MarkerTapRadius { get { return markerTapRadius; } }
        public bool ConfirmStep { get { return confirmStep; } }
        public bool IsFollowingCar { get { return isFollowingCar; } }
        public bool IsCrosshairActive { get { return crosshairLogic.IsCrosshairActive; } }

        private void OnEnable()
        {
            view = GetComponent<MapView>();
            isFollowingCar = true;
            needsSnap = true;
            crosshairLogic.Mode = crosshairMode;
            ApplyCrosshairVisible(crosshairLogic.IsCrosshairActive);
        }

        private void LateUpdate()
        {
            UpdateInteractiveMapLogic(Time.unscaledDeltaTime);
        }

        public void UpdateInteractiveMapLogic(float deltaTime)
        {
            NavigationManager activeManager = ResolveManagerWithFrame();
            if (activeManager == null)
            {
                return;
            }

            if (needsSnap)
            {
                view.SetZoomMeters(openZoomMeters, ComputeMaxZoomMeters(activeManager));
                ApplyClampedCenter(activeManager.CarMapPosition, activeManager.Frame.Size);
                needsSnap = false;
                if (Opened != null)
                {
                    Opened();
                }
            }
            else if (isFollowingCar)
            {
                ApplyClampedCenter(activeManager.CarMapPosition, activeManager.Frame.Size);
            }
            else
            {
                ApplyClampedCenter(view.CenterMap, activeManager.Frame.Size);
            }

            view.SetRotation(0f);

            crosshairLogic.Mode = crosshairMode;
            bool crosshairActive = crosshairLogic.IsCrosshairActive;
            if (crosshairActive != crosshairVisible)
            {
                ApplyCrosshairVisible(crosshairActive);
            }
        }

        public void Pan(Vector2 screenDelta)
        {
            NavigationManager activeManager = ResolveManagerWithFrame();
            if (activeManager == null)
            {
                return;
            }

            isFollowingCar = false;
            Vector2 mapDelta = screenDelta / view.CanvasUnitsPerMeter;
            Vector2 desiredCenter = view.CenterMap - mapDelta;
            ApplyClampedCenter(desiredCenter, activeManager.Frame.Size);
        }

        public void Zoom(float factor, Vector2 screenPivot)
        {
            NavigationManager activeManager = ResolveManagerWithFrame();
            if (activeManager == null)
            {
                return;
            }

            Vector2 pivotMap;
            if (isFollowingCar)
            {
                pivotMap = activeManager.CarMapPosition;
            }
            else
            {
                pivotMap = view.ViewportToMap(screenPivot);
            }

            float oldZoom = view.ZoomMeters;
            float requestedZoom = oldZoom / factor;
            Vector2 desiredCenter = math.ZoomAroundPivot(view.CenterMap, pivotMap, oldZoom, requestedZoom);
            view.SetZoomMeters(requestedZoom, ComputeMaxZoomMeters(activeManager));
            ApplyClampedCenter(desiredCenter, activeManager.Frame.Size);
        }

        public void Tap(Vector2 pos)
        {
            TapAt(pos);
        }

        public void TapAt(Vector2 screenPoint)
        {
            NavigationManager activeManager = ResolveManagerWithFrame();
            if (activeManager == null)
            {
                return;
            }

            MapMarker marker = null;
            Vector3 worldPoint;
            MarkerLayer markerLayer = view.MarkerLayer;
            Vector3 markerTruePosition;
            if (markerLayer != null && markerLayer.FindNearestDestinationMarker(screenPoint, markerTapRadius, out marker, out markerTruePosition))
            {
                worldPoint = activeManager.Converter.TrueToWorld(markerTruePosition);
            }
            else
            {
                Vector2 mapPoint = view.ViewportToMap(screenPoint);
                Vector3 truePoint = activeManager.Frame.MapToTrue(mapPoint, activeManager.Frame.Center.y);
                worldPoint = activeManager.Converter.TrueToWorld(truePoint);
            }

            if (confirmStep)
            {
                activeManager.PreviewDestination(worldPoint, marker);
            }
            else
            {
                activeManager.StartNavigation(worldPoint, marker);
            }
        }

        public void SetZoomMeters(float meters)
        {
            NavigationManager activeManager = ResolveManagerWithFrame();
            if (activeManager == null)
            {
                return;
            }

            view.SetZoomMeters(meters, ComputeMaxZoomMeters(activeManager));
            ApplyClampedCenter(view.CenterMap, activeManager.Frame.Size);
        }

        public void CenterOnCar()
        {
            isFollowingCar = true;
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

        public void SetCrosshairMode(bool active)
        {
            crosshairLogic.ForceActive(active);
        }

        public void ConfirmAtCrosshair()
        {
            TapAt(Vector2.zero);
        }

        public void PanByStick(Vector2 stick, float deltaTime)
        {
            crosshairLogic.NotifyCrosshairInput();
            if (view == null)
            {
                return;
            }

            float speed = view.Viewport.rect.width * GamepadPanSpeedFraction;
            Pan(stick * speed * deltaTime);
        }

        public void ZoomBySpeed(float axis, float deltaTime)
        {
            crosshairLogic.NotifyCrosshairInput();
            float factor = Mathf.Pow(GamepadZoomSpeedPerSecond, axis * deltaTime);
            Zoom(factor, Vector2.zero);
        }

        internal void SetZoomOutMode(FullMapZoomOutMode value)
        {
            zoomOutMode = value;
        }

        internal void SetCrosshairMode(CrosshairMode value)
        {
            crosshairMode = value;
            crosshairLogic.Mode = value;
        }

        internal void SetCrosshairImage(Image value)
        {
            crosshairImage = value;
        }

        internal void NotifyPointerInput()
        {
            crosshairLogic.NotifyPointerInput();
        }

        internal void SetOpenZoomMeters(float value)
        {
            openZoomMeters = value;
        }

        internal void SetMouseWheelStep(float value)
        {
            mouseWheelStep = value;
        }

        internal void SetDoubleTapStep(float value)
        {
            doubleTapStep = value;
        }

        internal void SetMarkerTapRadius(float value)
        {
            markerTapRadius = value;
        }

        internal void SetConfirmStep(bool value)
        {
            confirmStep = value;
        }

        private void ApplyCrosshairVisible(bool visible)
        {
            crosshairVisible = visible;
            if (crosshairImage != null)
            {
                crosshairImage.gameObject.SetActive(visible);
            }
        }

        private NavigationManager ResolveManagerWithFrame()
        {
            if (view == null)
            {
                return null;
            }

            NavigationManager found = view.Manager;
            if (found == null || found.Frame == null)
            {
                return null;
            }

            manager = found;
            return found;
        }

        private float ComputeMaxZoomMeters(NavigationManager activeManager)
        {
            bool fit = zoomOutMode == FullMapZoomOutMode.Fit;
            return math.MaxZoomMeters(activeManager.Frame.Size, view.Viewport.rect.size, fit);
        }

        private void ApplyClampedCenter(Vector2 desiredCenter, Vector2 mapSize)
        {
            Vector2 viewHalfSizeMeters = view.Viewport.rect.size * 0.5f / view.CanvasUnitsPerMeter;
            Vector2 clampedCenter = math.ClampCenter(desiredCenter, mapSize, viewHalfSizeMeters);
            view.SetCenter(clampedCenter);
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.CancelPreview();
            }

            if (Closed != null)
            {
                Closed();
            }
        }
    }
}
