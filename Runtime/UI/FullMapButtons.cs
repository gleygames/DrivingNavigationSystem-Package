using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class FullMapButtons
    {
        [SerializeField] private Button stopButton;
        [SerializeField] private Button centerButton;
        [SerializeField] private Button closeButton;

        public Button StopButton { get { return stopButton; } }
        public Button CenterButton { get { return centerButton; } }
        public Button CloseButton { get { return closeButton; } }

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
    }
}
