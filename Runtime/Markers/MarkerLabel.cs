using System.Text;
using UnityEngine;

namespace Gley.NavigationSystem
{
    [DefaultExecutionOrder(101)]
    public class MarkerLabel : MonoBehaviour, IMapMarkerVisual
    {
        [SerializeField] private Component text;

        private readonly NavigationTextOutput textOutput = new NavigationTextOutput();
        private readonly StringBuilder scratch = new StringBuilder(32);
        private Transform layerTransform;

        public Component Text { get { return text; } }

        public void UpdateMarkerLabelVisuals(float deltaTime)
        {
            if (text == null || layerTransform == null)
            {
                return;
            }

            text.transform.rotation = layerTransform.rotation;
        }

        public void Bind(MapMarker marker, MapView view)
        {
            if (text == null)
            {
                return;
            }

            bool visible = marker != null && view != null && view.ShowMarkerLabels && !string.IsNullOrEmpty(marker.DisplayName);
            SetTextVisible(visible);

            if (visible)
            {
                scratch.Length = 0;
                scratch.Append(marker.DisplayName);
                textOutput.Write(text, view.TextWriter, scratch);
            }

            layerTransform = transform.parent;
            enabled = visible && marker.RotationMode != MarkerRotationMode.Upright;
        }

        public void Unbind()
        {
            if (text == null)
            {
                return;
            }

            SetTextVisible(false);
            enabled = false;
        }

        internal void SetText(Component value)
        {
            text = value;
        }

        private void LateUpdate()
        {
            UpdateMarkerLabelVisuals(Time.deltaTime);
        }

        private void SetTextVisible(bool visible)
        {
            Behaviour behaviour = text as Behaviour;
            if (behaviour != null)
            {
                behaviour.enabled = visible;
            }
        }
    }
}
