using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class MapViewSettings
    {
        public const int MinimapChannelBit = 1 << 0;
        public const int FullMapChannelBit = 1 << 1;

        [SerializeField] private RouteStyle routeStyle;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private NavigationTextWriter textWriter;
        [SerializeField] private float minZoomMeters = 50f;
        [SerializeField] private float edgeInset = 8f;
        [SerializeField] private int channelMask;
        [SerializeField] private bool showPreview;
        [SerializeField] private bool showOffScreenArrows = true;
        [SerializeField] private bool showArrowDistance = true;

        public RouteStyle RouteStyle { get { return routeStyle; } }
        public GameObject ArrowPrefab { get { return arrowPrefab; } }
        public NavigationTextWriter TextWriter { get { return textWriter; } }
        public float MinZoomMeters { get { return minZoomMeters; } }
        public float EdgeInset { get { return edgeInset; } }
        public int ChannelMask { get { return channelMask; } }
        public bool ShowPreview { get { return showPreview; } }
        public bool ShowOffScreenArrows { get { return showOffScreenArrows; } }
        public bool ShowArrowDistance { get { return showArrowDistance; } }

        public MapViewSettings()
        {
            channelMask = MinimapChannelBit | FullMapChannelBit;
            showPreview = true;
        }

        public MapViewSettings(int channelMask, bool showPreview)
        {
            this.channelMask = channelMask;
            this.showPreview = showPreview;
        }

        internal void SetRouteStyle(RouteStyle value)
        {
            routeStyle = value;
        }

        internal void SetArrowPrefab(GameObject value)
        {
            arrowPrefab = value;
        }

        internal void SetTextWriter(NavigationTextWriter value)
        {
            textWriter = value;
        }

        internal void SetMinZoomMeters(float value)
        {
            minZoomMeters = value;
        }

        internal void SetEdgeInset(float value)
        {
            edgeInset = value;
        }

        internal void SetChannelMask(int value)
        {
            channelMask = value;
        }

        internal void SetShowPreview(bool value)
        {
            showPreview = value;
        }

        internal void SetShowOffScreenArrows(bool value)
        {
            showOffScreenArrows = value;
        }

        internal void SetShowArrowDistance(bool value)
        {
            showArrowDistance = value;
        }
    }
}
