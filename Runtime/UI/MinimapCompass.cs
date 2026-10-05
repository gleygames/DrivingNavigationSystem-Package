using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    internal class MinimapCompass
    {
        private readonly Button button;
        private readonly MapView view;
        private readonly MinimapFollowSettings followSettings;

        private RectTransform icon;

        internal MinimapCompass(Button button, RectTransform icon, MapView view, MinimapFollowSettings followSettings)
        {
            this.button = button;
            this.icon = icon;
            this.view = view;
            this.followSettings = followSettings;
        }

        internal void UpdateCompassVisuals()
        {
            if (icon == null)
            {
                return;
            }

            icon.localRotation = Quaternion.Euler(0f, 0f, view.RotationDegrees);
        }

        internal void Enable()
        {
            if (button == null)
            {
                return;
            }

            if (icon == null)
            {
                icon = (RectTransform)button.transform;
            }
            button.transform.SetAsLastSibling();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            followSettings.ToggleRotationMode();
        }

        internal void Disable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}
