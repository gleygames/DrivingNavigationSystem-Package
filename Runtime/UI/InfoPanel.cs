using System.Text;

namespace Gley.NavigationSystem
{
    internal class InfoPanel
    {
        private readonly StringBuilder titleScratch = new StringBuilder(32);
        private readonly NavigationTextOutput textOutput = new NavigationTextOutput();
        private readonly InfoPanelSlots slots;
        private readonly MapViewSettings viewSettings;

        private NavigationManager cachedManager;

        internal InfoPanel(InfoPanelSlots slots, MapViewSettings viewSettings)
        {
            this.slots = slots;
            this.viewSettings = viewSettings;
        }

        internal void Enable(NavigationManager manager)
        {
            if (manager != null)
            {
                cachedManager = manager;
                manager.MarkerSelected += HandleMarkerSelected;
                manager.MarkerDeselected += HandleMarkerDeselected;
                manager.MarkerVisualsChanged += HandleMarkerVisualsChanged;
            }

            if (slots.CloseButton != null)
            {
                slots.CloseButton.onClick.AddListener(HandleCloseClicked);
            }

            if (cachedManager != null)
            {
                Refresh(cachedManager.SelectedMarker);
            }
            else
            {
                Refresh(null);
            }
        }

        private void Refresh(MapMarker marker)
        {
            bool visible = marker != null && !string.IsNullOrEmpty(marker.DisplayName);

            if (slots.PanelRoot != null)
            {
                slots.PanelRoot.SetActive(visible);
            }

            if (visible && slots.TitleText != null)
            {
                titleScratch.Length = 0;
                titleScratch.Append(marker.DisplayName);
                textOutput.Write(slots.TitleText, viewSettings.TextWriter, titleScratch);
            }
        }

        private void HandleMarkerSelected(MapMarker marker)
        {
            Refresh(marker);
        }

        private void HandleMarkerDeselected(MapMarker marker)
        {
            Refresh(null);
        }

        private void HandleMarkerVisualsChanged(MapMarker marker)
        {
            if (cachedManager != null && ReferenceEquals(marker, cachedManager.SelectedMarker))
            {
                Refresh(marker);
            }
        }

        private void HandleCloseClicked()
        {
            if (cachedManager != null)
            {
                cachedManager.ClearSelection();
            }
        }

        internal void Disable()
        {
            if (cachedManager != null)
            {
                cachedManager.MarkerSelected -= HandleMarkerSelected;
                cachedManager.MarkerDeselected -= HandleMarkerDeselected;
                cachedManager.MarkerVisualsChanged -= HandleMarkerVisualsChanged;
                cachedManager = null;
            }

            if (slots.CloseButton != null)
            {
                slots.CloseButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (slots.PanelRoot != null)
            {
                slots.PanelRoot.SetActive(false);
            }
        }
    }
}
