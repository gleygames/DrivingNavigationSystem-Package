using UnityEngine;

namespace Gley.NavigationSystem
{
    public class NavigationMap : MonoBehaviour
    {
        [SerializeField] private MapData mapData;

        public MapData MapData { get { return mapData; } }
    }
}
