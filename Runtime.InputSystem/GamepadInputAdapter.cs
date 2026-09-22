using UnityEngine;
using UnityEngine.InputSystem;

namespace Gley.NavigationSystem.InputSystem
{
    public class GamepadInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private MapViewInteractive target;
        [SerializeField] private NavigationManager manager;
        private NavigationManager cachedManager;
        private InputActionMap map;
        private InputAction panAction;
        private InputAction zoomAction;
        private InputAction confirmAction;
        private InputAction cancelAction;
        private InputAction centerOnCarAction;
        private InputAction closeAction;

        private void OnEnable()
        {
            if (actions == null)
            {
                return;
            }

            map = actions.FindActionMap("Map");
            if (map == null)
            {
                return;
            }

            panAction = map.FindAction("Pan");
            zoomAction = map.FindAction("Zoom");
            confirmAction = map.FindAction("Confirm");
            cancelAction = map.FindAction("Cancel");
            centerOnCarAction = map.FindAction("CenterOnCar");
            closeAction = map.FindAction("Close");

            if (confirmAction != null)
            {
                confirmAction.performed += HandleConfirmPerformed;
            }
            if (cancelAction != null)
            {
                cancelAction.performed += HandleCancelPerformed;
            }
            if (centerOnCarAction != null)
            {
                centerOnCarAction.performed += HandleCenterOnCarPerformed;
            }
            if (closeAction != null)
            {
                closeAction.performed += HandleClosePerformed;
            }

            map.Enable();
        }

        private void Update()
        {
            UpdateGamepadInputLogic(Time.unscaledDeltaTime);
        }

        public void UpdateGamepadInputLogic(float deltaTime)
        {
            if (target == null)
            {
                return;
            }

            if (panAction != null)
            {
                Vector2 pan = panAction.ReadValue<Vector2>();
                if (pan != Vector2.zero)
                {
                    target.PanByStick(pan, deltaTime);
                }
            }

            if (zoomAction != null)
            {
                float zoom = zoomAction.ReadValue<float>();
                if (zoom != 0f)
                {
                    target.ZoomBySpeed(zoom, deltaTime);
                }
            }
        }

        private void HandleConfirmPerformed(InputAction.CallbackContext context)
        {
            if (target == null)
            {
                return;
            }

            NavigationManager found = FindManager();
            if (found != null && found.HasPreview)
            {
                found.ConfirmPreview();
            }
            else
            {
                target.ConfirmAtCrosshair();
            }
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
            cachedManager = FindAnyObjectByType<NavigationManager>();
            return cachedManager;
        }

        private void HandleCancelPerformed(InputAction.CallbackContext context)
        {
            NavigationManager found = FindManager();
            if (found != null)
            {
                found.CancelPreview();
            }
        }

        private void HandleCenterOnCarPerformed(InputAction.CallbackContext context)
        {
            if (target != null)
            {
                target.CenterOnCar();
            }
        }

        private void HandleClosePerformed(InputAction.CallbackContext context)
        {
            if (target != null)
            {
                target.Close();
            }
        }

        private void OnDisable()
        {
            if (confirmAction != null)
            {
                confirmAction.performed -= HandleConfirmPerformed;
            }
            if (cancelAction != null)
            {
                cancelAction.performed -= HandleCancelPerformed;
            }
            if (centerOnCarAction != null)
            {
                centerOnCarAction.performed -= HandleCenterOnCarPerformed;
            }
            if (closeAction != null)
            {
                closeAction.performed -= HandleClosePerformed;
            }

            if (map != null)
            {
                map.Disable();
            }
        }
    }
}
