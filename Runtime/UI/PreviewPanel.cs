using System.Text;

namespace Gley.NavigationSystem
{
    internal class PreviewPanel
    {
        private readonly StringBuilder distanceScratch = new StringBuilder(16);
        private readonly StringBuilder etaScratch = new StringBuilder(16);
        private readonly NavigationTextOutput textOutput = new NavigationTextOutput();
        private readonly PreviewPanelSlots slots;
        private readonly MapViewSettings viewSettings;

        private NavigationManager cachedManager;

        internal PreviewPanel(PreviewPanelSlots slots, MapViewSettings viewSettings)
        {
            this.slots = slots;
            this.viewSettings = viewSettings;
        }

        internal void Enable(NavigationManager manager)
        {
            if (manager != null)
            {
                cachedManager = manager;
                manager.PreviewReady += HandlePreviewReady;
                manager.PreviewFailed += HandlePreviewFailed;
                manager.PreviewCanceled += HandlePreviewCanceled;
                manager.NavigationStarted += HandleNavigationStarted;
            }

            if (slots.ConfirmButton != null)
            {
                slots.ConfirmButton.onClick.AddListener(HandleConfirmClicked);
            }
            if (slots.CancelButton != null)
            {
                slots.CancelButton.onClick.AddListener(HandleCancelClicked);
            }

            if (cachedManager != null)
            {
                SetVisible(cachedManager.HasPreview);
            }
            else
            {
                SetVisible(false);
            }
        }

        private void HandlePreviewReady(Route route, MapMarker marker)
        {
            SetVisible(true);
            UpdatePreviewPanelTexts(route);
        }

        private void SetVisible(bool visible)
        {
            if (slots.PanelRoot != null)
            {
                slots.PanelRoot.SetActive(visible);
            }
        }

        private void UpdatePreviewPanelTexts(Route route)
        {
            if (cachedManager == null || cachedManager.Formatter == null || route == null)
            {
                return;
            }

            if (slots.DistanceText != null)
            {
                distanceScratch.Length = 0;
                cachedManager.Formatter.FormatDistance(route.Length, distanceScratch);
                textOutput.Write(slots.DistanceText, viewSettings.TextWriter, distanceScratch);
            }

            if (slots.EtaText != null)
            {
                etaScratch.Length = 0;
                cachedManager.Formatter.FormatDuration(route.Eta, etaScratch);
                textOutput.Write(slots.EtaText, viewSettings.TextWriter, etaScratch);
            }
        }

        private void HandlePreviewFailed(FailureReason reason)
        {
            SetVisible(false);
        }

        private void HandlePreviewCanceled()
        {
            SetVisible(false);
        }

        private void HandleNavigationStarted(Route route)
        {
            SetVisible(false);
        }

        private void HandleConfirmClicked()
        {
            if (cachedManager != null)
            {
                cachedManager.ConfirmPreview();
            }
        }

        private void HandleCancelClicked()
        {
            if (cachedManager != null)
            {
                cachedManager.CancelPreview();
            }
        }

        internal void Disable()
        {
            if (cachedManager != null)
            {
                cachedManager.PreviewReady -= HandlePreviewReady;
                cachedManager.PreviewFailed -= HandlePreviewFailed;
                cachedManager.PreviewCanceled -= HandlePreviewCanceled;
                cachedManager.NavigationStarted -= HandleNavigationStarted;
                cachedManager = null;
            }

            if (slots.ConfirmButton != null)
            {
                slots.ConfirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }
            if (slots.CancelButton != null)
            {
                slots.CancelButton.onClick.RemoveListener(HandleCancelClicked);
            }
        }
    }
}
