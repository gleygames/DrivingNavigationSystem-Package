using Gley.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gley.NavigationSystem
{
    public class MinimapTapToOpen : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private MinimapTapAction tapAction = MinimapTapAction.OpenFullMap;
        [SerializeField] private MapViewInteractive fullMap;

        public MinimapTapAction TapAction { get { return tapAction; } }

        public void SetTapAction(MinimapTapAction value)
        {
            tapAction = value;
        }

        public void SetFullMap(MapViewInteractive value)
        {
            fullMap = value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (tapAction != MinimapTapAction.OpenFullMap)
            {
                return;
            }

            if (fullMap == null)
            {
                fullMap = FindAnyObjectByType<MapViewInteractive>(FindObjectsInactive.Include);
            }

            if (fullMap == null)
            {
                CustomLogger.LogError("MinimapTapToOpen on '" + name + "': no full map (MapViewInteractive) found in the loaded scenes.", this);
                return;
            }

            fullMap.Open();
        }
    }
}
