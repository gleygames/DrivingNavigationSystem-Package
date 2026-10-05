using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class PreviewPanelSlots
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Component distanceText;
        [SerializeField] private Component etaText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        public GameObject PanelRoot { get { return panelRoot; } }
        public Component DistanceText { get { return distanceText; } }
        public Component EtaText { get { return etaText; } }
        public Button ConfirmButton { get { return confirmButton; } }
        public Button CancelButton { get { return cancelButton; } }

        internal void SetPanelRoot(GameObject value)
        {
            panelRoot = value;
        }

        internal void SetDistanceText(Component value)
        {
            distanceText = value;
        }

        internal void SetEtaText(Component value)
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
    }
}
