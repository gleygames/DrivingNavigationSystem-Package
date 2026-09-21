using UnityEngine;

namespace Gley.NavigationSystem
{
    public class MapData : ScriptableObject, IFormatVersioned
    {
        public const int CurrentFormatVersion = 1;

        [SerializeField] private Vector3 rectangleCenter;
        [SerializeField] private Vector3 editTimeWorldPosition;
        [SerializeField] private Vector2 rectangleSize;
        [SerializeField] private Color outsideMapColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private RoadNetworkData roadNetwork;
        [SerializeField] private Texture2D image;
        [SerializeField] private MapImageState imageState;
        [SerializeField] private float rectangleRotationY;
        [SerializeField] private int formatVersion = CurrentFormatVersion;
        [SerializeField] private bool locked;

        public Vector3 RectangleCenter { get { return rectangleCenter; } }
        public Vector3 EditTimeWorldPosition { get { return editTimeWorldPosition; } }
        public Vector2 RectangleSize { get { return rectangleSize; } }
        public Color OutsideMapColor { get { return outsideMapColor; } }
        public RoadNetworkData RoadNetwork { get { return roadNetwork; } }
        public Texture2D Image { get { return image; } }
        public MapImageState ImageState { get { return imageState; } }
        public float RectangleRotationY { get { return rectangleRotationY; } }
        public int FormatVersion { get { return formatVersion; } }
        int IFormatVersioned.CurrentFormatVersion { get { return CurrentFormatVersion; } }
        public bool Locked { get { return locked; } }

        public MapFrame CreateFrame()
        {
            return new MapFrame(rectangleCenter, rectangleSize, rectangleRotationY);
        }

        internal void SetRectangleCenter(Vector3 value)
        {
            rectangleCenter = value;
        }

        internal void SetEditTimeWorldPosition(Vector3 value)
        {
            editTimeWorldPosition = value;
        }

        internal void SetRectangleSize(Vector2 value)
        {
            rectangleSize = value;
        }

        internal void SetOutsideMapColor(Color value)
        {
            outsideMapColor = value;
        }

        internal void SetRoadNetwork(RoadNetworkData value)
        {
            roadNetwork = value;
        }

        internal void SetImage(Texture2D value)
        {
            image = value;
        }

        internal void SetImageState(MapImageState value)
        {
            imageState = value;
        }

        internal void SetRectangleRotationY(float value)
        {
            rectangleRotationY = value;
        }

        internal void SetFormatVersion(int value)
        {
            formatVersion = value;
        }

        internal void SetLocked(bool value)
        {
            locked = value;
        }
    }
}
