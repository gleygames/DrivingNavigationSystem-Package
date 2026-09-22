using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    public class PreviewPanel : MonoBehaviour
    {
        private readonly StringBuilder distanceScratch = new StringBuilder(16);
        private readonly StringBuilder etaScratch = new StringBuilder(16);

        [SerializeField] private NavigationManager manager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private NavigationTextTarget distanceText;
        [SerializeField] private NavigationTextTarget etaText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        private NavigationManager cachedManager;

        private void OnEnable()
        {
            NavigationManager found = FindManager();
            if (found != null)
            {
                cachedManager = found;
                found.PreviewReady += HandlePreviewReady;
                found.PreviewFailed += HandlePreviewFailed;
                found.PreviewCanceled += HandlePreviewCanceled;
                found.NavigationStarted += HandleNavigationStarted;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmClicked);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancelClicked);
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

        internal void SetManager(NavigationManager value)
        {
            manager = value;
        }

        internal void SetPanelRoot(GameObject value)
        {
            panelRoot = value;
        }

        internal void SetDistanceText(NavigationTextTarget value)
        {
            distanceText = value;
        }

        internal void SetEtaText(NavigationTextTarget value)
        {
            etaText = value;
        }

        internal void SetConfirmButton(Button value)
        {
            confirmButton = value;
        }

        internal void SetCancelButton(Button value)
        {
            cancelButton = value;
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

        private void HandlePreviewReady(Route route, MapMarker marker)
        {
            SetVisible(true);
            UpdatePreviewPanelTexts(route);
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void UpdatePreviewPanelTexts(Route route)
        {
            if (cachedManager == null || cachedManager.Formatter == null || route == null)
            {
                return;
            }

            if (distanceText != null)
            {
                distanceScratch.Length = 0;
                cachedManager.Formatter.FormatDistance(route.Length, distanceScratch);
                distanceText.SetText(distanceScratch);
            }

            if (etaText != null)
            {
                etaScratch.Length = 0;
                cachedManager.Formatter.FormatDuration(route.Eta, etaScratch);
                etaText.SetText(etaScratch);
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

        private void OnDisable()
        {
            if (cachedManager != null)
            {
                cachedManager.PreviewReady -= HandlePreviewReady;
                cachedManager.PreviewFailed -= HandlePreviewFailed;
                cachedManager.PreviewCanceled -= HandlePreviewCanceled;
                cachedManager.NavigationStarted -= HandleNavigationStarted;
                cachedManager = null;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
            }
        }
    }
}
