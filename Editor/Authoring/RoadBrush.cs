using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [System.Serializable]
    public class RoadBrush
    {
        [SerializeField] private float speedOverride;
        [SerializeField] private float widthOverride;
        [SerializeField] private int typeId;
        [SerializeField] private bool oneWay;

        public float SpeedOverride { get { return speedOverride; } }
        public float WidthOverride { get { return widthOverride; } }
        public int TypeId { get { return typeId; } }
        public bool OneWay { get { return oneWay; } }

        internal void SetTypeId(int value)
        {
            typeId = value;
        }

        internal void SetOneWay(bool value)
        {
            oneWay = value;
        }

        internal void SetSpeedOverride(float value)
        {
            speedOverride = value;
        }

        internal void SetWidthOverride(float value)
        {
            widthOverride = value;
        }
    }
}
