using UnityEngine;
using UnityEngine.EventSystems;

namespace Gley.NavigationSystem
{
    public class MinimapTapToOpen : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private MinimapTapAction tapAction = MinimapTapAction.OpenFullMap;
        [SerializeField] private GameObject fullMap;

        public MinimapTapAction TapAction { get { return tapAction; } }

        public void SetTapAction(MinimapTapAction value)
        {
            tapAction = value;
        }

        public void SetFullMap(GameObject value)
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
                return;
            }

            fullMap.SetActive(true);
        }
    }
}
