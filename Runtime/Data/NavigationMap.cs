using UnityEngine;

namespace Gley.NavigationSystem
{
    public class NavigationMap : MonoBehaviour
    {
        [SerializeField] private MapData mapData;
        [SerializeField] private NavigationManager manager;
        private NavigationManager cachedManager;

        public MapData MapData { get { return mapData; } }

        private void OnEnable()
        {
            NavigationManager found = FindManager();
            if (found == null)
            {
                return;
            }
            found.RegisterMap(this);
        }

        internal void SetMapData(MapData value)
        {
            mapData = value;
        }

        internal void AttachManager(NavigationManager value)
        {
            cachedManager = value;
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

        private void OnDisable()
        {
            if (cachedManager == null)
            {
                return;
            }
            cachedManager.UnregisterMap(this);
        }
    }
}
