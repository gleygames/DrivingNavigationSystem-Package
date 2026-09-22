using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(101)]
    [RequireComponent(typeof(Button))]
    public class CompassButton : MonoBehaviour
    {
        [SerializeField] private MapView mapView;
        [SerializeField] private MapViewFollowCar followCar;
        [SerializeField] private RectTransform icon;
        private Button button;

        private void OnEnable()
        {
            button = GetComponent<Button>();
            if (icon == null)
            {
                icon = (RectTransform)transform;
            }
            if (mapView == null)
            {
                mapView = GetComponentInParent<MapView>();
            }
            if (followCar == null && mapView != null)
            {
                followCar = mapView.GetComponent<MapViewFollowCar>();
            }

            transform.SetAsLastSibling();
            button.onClick.AddListener(HandleClick);
        }

        private void LateUpdate()
        {
            UpdateCompassButtonVisuals(Time.unscaledDeltaTime);
        }

        public void UpdateCompassButtonVisuals(float deltaTime)
        {
            if (mapView == null || icon == null)
            {
                return;
            }

            icon.localRotation = Quaternion.Euler(0f, 0f, mapView.RotationDegrees);
        }

        private void HandleClick()
        {
            if (followCar != null)
            {
                followCar.ToggleRotationMode();
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}
