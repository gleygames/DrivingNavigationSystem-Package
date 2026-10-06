using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    public class MapViewInteractive : IMapGestureTarget
    {
        private const float GamepadPanSpeedFraction = 0.5f;
        private const float GamepadZoomSpeedPerSecond = 2f;

        private readonly FullMapMath math = new FullMapMath();
        private readonly CrosshairModeLogic crosshairLogic = new CrosshairModeLogic();
        private readonly ProfilerMarker interactiveMarker = new ProfilerMarker("Gley.Nav.Interactive");
        private readonly NavigationFullMap owner;
        private readonly MapView view;
        private readonly FullMapInteractionSettings settings;
        private readonly Image crosshairImage;

        private NavigationManager manager;
        private bool isFollowingCar;
        private bool needsSnap = true;
        private bool crosshairVisible;

        public float ZoomMeters { get { return view.ZoomMeters; } }
        public bool IsFollowingCar { get { return isFollowingCar; } }
        public bool IsCrosshairActive { get { return crosshairLogic.IsCrosshairActive; } }

        internal MapViewInteractive(NavigationFullMap owner, MapView view, FullMapInteractionSettings settings, Image crosshairImage)
        {
            this.owner = owner;
            this.view = view;
            this.settings = settings;
            this.crosshairImage = crosshairImage;
        }

        internal void Enable()
        {
            isFollowingCar = true;
            needsSnap = true;
            crosshairLogic.Mode = settings.CrosshairMode;
            ApplyCrosshairVisible(crosshairLogic.IsCrosshairActive);
        }

        internal void UpdateInteractiveMapLogic(float deltaTime)
        {
            using (interactiveMarker.Auto())
            {
                NavigationManager activeManager = ResolveManagerWithFrame();
                if (activeManager == null)
                {
                    return;
                }

                if (needsSnap)
                {
                    view.SetZoomMeters(settings.OpenZoomMeters, ComputeMaxZoomMeters(activeManager));
                    ApplyClampedCenter(activeManager.CarMapPosition, activeManager.Frame.Size);
                    needsSnap = false;
                    owner.NotifyOpened();
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

                crosshairLogic.Mode = settings.CrosshairMode;
                bool crosshairActive = crosshairLogic.IsCrosshairActive;
                if (crosshairActive != crosshairVisible)
                {
                    ApplyCrosshairVisible(crosshairActive);
                }
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
            if (markerLayer != null && markerLayer.FindNearestDestinationMarker(screenPoint, settings.MarkerTapRadius, out marker, out markerTruePosition))
            {
                worldPoint = activeManager.Converter.TrueToWorld(markerTruePosition);
            }
            else
            {
                if (settings.TapTarget == FullMapTapTarget.MarkersOnly)
                {
                    return;
                }

                Vector2 mapPoint = view.ViewportToMap(screenPoint);
                Vector3 truePoint = activeManager.Frame.MapToTrue(mapPoint, activeManager.Frame.Center.y);
                worldPoint = activeManager.Converter.TrueToWorld(truePoint);
            }

            if (settings.ConfirmStep)
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

        internal void NotifyPointerInput()
        {
            crosshairLogic.NotifyPointerInput();
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
            bool fit = settings.ZoomOutMode == FullMapZoomOutMode.Fit;
            return math.MaxZoomMeters(activeManager.Frame.Size, view.Viewport.rect.size, fit);
        }

        private void ApplyClampedCenter(Vector2 desiredCenter, Vector2 mapSize)
        {
            Vector2 viewHalfSizeMeters = view.Viewport.rect.size * 0.5f / view.CanvasUnitsPerMeter;
            Vector2 clampedCenter = math.ClampCenter(desiredCenter, mapSize, viewHalfSizeMeters);
            view.SetCenter(clampedCenter);
        }

        internal void Disable()
        {
            if (manager != null)
            {
                manager.CancelPreview();
            }
        }
    }
}
