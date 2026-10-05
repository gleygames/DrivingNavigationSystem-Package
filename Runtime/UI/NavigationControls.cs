namespace Gley.NavigationSystem
{
    internal class NavigationControls
    {
        private readonly NavigationFullMap owner;
        private readonly MapViewInteractive interactive;
        private readonly FullMapButtons buttons;

        private NavigationManager cachedManager;
        private bool centerVisible;

        internal NavigationControls(NavigationFullMap owner, MapViewInteractive interactive, FullMapButtons buttons)
        {
            this.owner = owner;
            this.interactive = interactive;
            this.buttons = buttons;
        }

        internal void Enable(NavigationManager manager)
        {
            if (manager != null)
            {
                cachedManager = manager;
                manager.NavigationStarted += HandleNavigationStarted;
                manager.Arrived += HandleArrived;
                manager.NavigationStopped += HandleNavigationStopped;
                manager.RouteFailed += HandleRouteFailed;
            }

            if (buttons.StopButton != null)
            {
                buttons.StopButton.onClick.AddListener(HandleStopClicked);
            }
            if (buttons.CenterButton != null)
            {
                buttons.CenterButton.onClick.AddListener(HandleCenterClicked);
            }
            if (buttons.CloseButton != null)
            {
                buttons.CloseButton.onClick.AddListener(HandleCloseClicked);
            }

            RefreshStopVisible();
            ApplyCenterVisible(ComputeCenterVisible());
        }

        internal void UpdateNavigationControlsVisuals()
        {
            bool visible = ComputeCenterVisible();
            if (visible == centerVisible)
            {
                return;
            }
            ApplyCenterVisible(visible);
        }

        private void HandleNavigationStarted(Route route)
        {
            RefreshStopVisible();
        }

        private void RefreshStopVisible()
        {
            if (buttons.StopButton == null)
            {
                return;
            }

            bool visible = false;
            if (cachedManager != null)
            {
                visible = cachedManager.HasActiveRoute;
            }
            buttons.StopButton.gameObject.SetActive(visible);
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
            if (buttons.CenterButton != null)
            {
                buttons.CenterButton.gameObject.SetActive(visible);
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
            if (owner != null)
            {
                owner.Close();
            }
        }

        internal void Disable()
        {
            if (cachedManager != null)
            {
                cachedManager.NavigationStarted -= HandleNavigationStarted;
                cachedManager.Arrived -= HandleArrived;
                cachedManager.NavigationStopped -= HandleNavigationStopped;
                cachedManager.RouteFailed -= HandleRouteFailed;
                cachedManager = null;
            }

            if (buttons.StopButton != null)
            {
                buttons.StopButton.onClick.RemoveListener(HandleStopClicked);
            }
            if (buttons.CenterButton != null)
            {
                buttons.CenterButton.onClick.RemoveListener(HandleCenterClicked);
            }
            if (buttons.CloseButton != null)
            {
                buttons.CloseButton.onClick.RemoveListener(HandleCloseClicked);
            }
        }
    }
}
