using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [System.Serializable]
    public class AuthoringRoad
    {
        [SerializeField] private List<AuthoringKeyPoint> keyPoints = new List<AuthoringKeyPoint>();
        [SerializeField] private List<Vector3> points = new List<Vector3>();
        [SerializeField] private List<int> groundMissIndices = new List<int>();
        [SerializeField] private string sourceTag = string.Empty;
        [SerializeField] private float speedOverride;
        [SerializeField] private float widthOverride;
        [SerializeField] private int id;
        [SerializeField] private int typeId;
        [SerializeField] private int startIntersectionId;
        [SerializeField] private int endIntersectionId;
        [SerializeField] private bool oneWay;
        [SerializeField] private bool modifiedAfterImport;

        public List<AuthoringKeyPoint> KeyPoints { get { return keyPoints; } }
        public List<Vector3> Points { get { return points; } }
        public List<int> GroundMissIndices { get { return groundMissIndices; } }
        public string SourceTag { get { return sourceTag; } }
        public float SpeedOverride { get { return speedOverride; } }
        public float WidthOverride { get { return widthOverride; } }
        public int Id { get { return id; } }
        public int TypeId { get { return typeId; } }
        public int StartIntersectionId { get { return startIntersectionId; } }
        public int EndIntersectionId { get { return endIntersectionId; } }
        public bool OneWay { get { return oneWay; } }
        public bool ModifiedAfterImport { get { return modifiedAfterImport; } }

        public AuthoringRoad(int id)
        {
            this.id = id;
        }

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

        internal void SetSourceTag(string value)
        {
            sourceTag = value;
        }

        internal void SetModifiedAfterImport(bool value)
        {
            modifiedAfterImport = value;
        }

        internal void SetStartIntersectionId(int value)
        {
            startIntersectionId = value;
        }

        internal void SetEndIntersectionId(int value)
        {
            endIntersectionId = value;
        }
    }
}
