using UnityEngine;

namespace Gley.NavigationSystem
{
    public class MapMarker : MonoBehaviour
    {
        private const int MinimapChannelBit = 1 << 0;
        private const int FullMapChannelBit = 1 << 1;

        [SerializeField] private NavigationManager manager;
        [SerializeField] private GameObject prefab;
        [SerializeField] private MarkerRotationMode rotationMode = MarkerRotationMode.Upright;
        [SerializeField] private int channelMask = MinimapChannelBit | FullMapChannelBit;
        [SerializeField] private bool isStatic;
        [SerializeField] private bool canBeDestination;
        [SerializeField] private bool showOffScreenArrow;
        private NavigationManager cachedManager;

        public GameObject Prefab { get { return prefab; } }
        public MarkerRotationMode RotationMode { get { return rotationMode; } }
        public int ChannelMask { get { return channelMask; } }
        public bool IsStatic { get { return isStatic; } }
        public bool CanBeDestination { get { return canBeDestination; } }
        public bool ShowOffScreenArrow { get { return showOffScreenArrow; } }

        private void OnEnable()
        {
            NavigationManager found = FindManager();
            if (found == null)
            {
                return;
            }

            cachedManager = found;
            found.AddMarker(this);
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

        internal void SetManager(NavigationManager value)
        {
            manager = value;
        }

        internal void SetPrefab(GameObject value)
        {
            prefab = value;
        }

        internal void SetRotationMode(MarkerRotationMode value)
        {
            rotationMode = value;
        }

        internal void SetChannelMask(int value)
        {
            channelMask = value;
        }

        internal void SetIsStatic(bool value)
        {
            isStatic = value;
        }

        internal void SetCanBeDestination(bool value)
        {
            canBeDestination = value;
        }

        internal void SetShowOffScreenArrow(bool value)
        {
            showOffScreenArrow = value;
        }

        private void OnDisable()
        {
            if (cachedManager == null)
            {
                return;
            }
            cachedManager.RemoveMarker(this);
            cachedManager = null;
        }
    }
}
