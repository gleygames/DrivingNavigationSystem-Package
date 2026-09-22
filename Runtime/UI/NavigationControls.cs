using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    public class NavigationControls : MonoBehaviour
    {
        [SerializeField] private NavigationManager manager;
        [SerializeField] private MapViewInteractive interactive;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button centerButton;
        [SerializeField] private Button closeButton;
        private NavigationManager cachedManager;
        private bool centerVisible;

        private void OnEnable()
        {
            if (interactive == null)
            {
                interactive = GetComponentInParent<MapViewInteractive>();
            }

            NavigationManager found = FindManager();
            if (found != null)
            {
                cachedManager = found;
                found.NavigationStarted += HandleNavigationStarted;
                found.Arrived += HandleArrived;
                found.NavigationStopped += HandleNavigationStopped;
                found.RouteFailed += HandleRouteFailed;
            }

            if (stopButton != null)
            {
                stopButton.onClick.AddListener(HandleStopClicked);
            }
            if (centerButton != null)
            {
                centerButton.onClick.AddListener(HandleCenterClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            RefreshStopVisible();
            ApplyCenterVisible(ComputeCenterVisible());
        }

        private void LateUpdate()
        {
            UpdateNavigationControlsVisuals();
        }

        public void UpdateNavigationControlsVisuals()
        {
            bool visible = ComputeCenterVisible();
            if (visible == centerVisible)
            {
                return;
            }
            ApplyCenterVisible(visible);
        }

        internal void SetManager(NavigationManager value)
        {
            manager = value;
        }

        internal void SetInteractive(MapViewInteractive value)
        {
            interactive = value;
        }

        internal void SetStopButton(Button value)
        {
            stopButton = value;
        }

        internal void SetCenterButton(Button value)
        {
            centerButton = value;
        }

        internal void SetCloseButton(Button value)
        {
            closeButton = value;
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

        private void HandleNavigationStarted(Route route)
        {
            RefreshStopVisible();
        }

        private void RefreshStopVisible()
        {
            if (stopButton == null)
            {
                return;
            }

            bool visible = false;
            if (cachedManager != null)
            {
                visible = cachedManager.HasActiveRoute;
            }
            stopButton.gameObject.SetActive(visible);
        }

        private void HandleArrived()
        {
            RefreshStopVisible();
        }

        private void HandleNavigationStopped(StopReason reason)
        {
            RefreshStopVisible();
        }

        private void HandleRouteFailed(FailureReason reason)
        {
            RefreshStopVisible();
        }

        private bool ComputeCenterVisible()
        {
            if (interactive == null)
            {
                return false;
            }
            return !interactive.IsFollowingCar;
        }

        private void ApplyCenterVisible(bool visible)
        {
            centerVisible = visible;
            if (centerButton != null)
            {
                centerButton.gameObject.SetActive(visible);
            }
        }

        private void HandleStopClicked()
        {
            if (cachedManager != null)
            {
                cachedManager.StopNavigation();
            }
        }

        private void HandleCenterClicked()
        {
            if (interactive != null)
            {
                interactive.CenterOnCar();
            }
        }

        private void HandleCloseClicked()
        {
            if (interactive != null)
            {
                interactive.Close();
            }
        }

        private void OnDisable()
        {
            if (cachedManager != null)
            {
                cachedManager.NavigationStarted -= HandleNavigationStarted;
                cachedManager.Arrived -= HandleArrived;
                cachedManager.NavigationStopped -= HandleNavigationStopped;
                cachedManager.RouteFailed -= HandleRouteFailed;
                cachedManager = null;
            }

            if (stopButton != null)
            {
                stopButton.onClick.RemoveListener(HandleStopClicked);
            }
            if (centerButton != null)
            {
                centerButton.onClick.RemoveListener(HandleCenterClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }
        }
    }
}
