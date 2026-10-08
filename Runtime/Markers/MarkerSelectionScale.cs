using UnityEngine;

namespace Gley.NavigationSystem
{
    public class MarkerSelectionScale : MonoBehaviour, IMapMarkerSelectable
    {
        [SerializeField] private float selectedScale = 1.25f;

        public float SelectedScale { get { return selectedScale; } }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                transform.localScale = Vector3.one * selectedScale;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        internal void SetSelectedScale(float value)
        {
            selectedScale = value;
        }
    }
}
