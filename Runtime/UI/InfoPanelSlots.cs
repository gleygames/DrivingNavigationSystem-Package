using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class InfoPanelSlots
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Component titleText;
        [SerializeField] private Button closeButton;

        public GameObject PanelRoot { get { return panelRoot; } }
        public Component TitleText { get { return titleText; } }
        public Button CloseButton { get { return closeButton; } }

        internal void SetPanelRoot(GameObject value)
        {
            panelRoot = value;
        }

        internal void SetTitleText(Component value)
        {
            titleText = value;
        }

        internal void SetCloseButton(Button value)
        {
            closeButton = value;
        }
    }
}
