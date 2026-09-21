using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public struct RoadRecord
    {
        [SerializeField] private float length;
        [SerializeField] private float speed;
        [SerializeField] private float width;
        [SerializeField] private int id;
        [SerializeField] private int typeId;
        [SerializeField] private int startIntersection;
        [SerializeField] private int endIntersection;
        [SerializeField] private int firstPoint;
        [SerializeField] private int pointCount;
        [SerializeField] private bool oneWay;

        public float Length { get { return length; } }
        public float Speed { get { return speed; } }
        public float Width { get { return width; } }
        public int Id { get { return id; } }
        public int TypeId { get { return typeId; } }
        public int StartIntersection { get { return startIntersection; } }
        public int EndIntersection { get { return endIntersection; } }
        public int FirstPoint { get { return firstPoint; } }
        public int PointCount { get { return pointCount; } }
        public bool OneWay { get { return oneWay; } }

        public RoadRecord(int id, int typeId, bool oneWay, int startIntersection, int endIntersection, int firstPoint, int pointCount, float length, float speed, float width)
        {
            this.id = id;
            this.typeId = typeId;
            this.oneWay = oneWay;
            this.startIntersection = startIntersection;
            this.endIntersection = endIntersection;
            this.firstPoint = firstPoint;
            this.pointCount = pointCount;
            this.length = length;
            this.speed = speed;
            this.width = width;
        }
    }
}
