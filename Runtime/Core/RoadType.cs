using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class RoadType
    {
        [SerializeField] private Color editorColor;
        [SerializeField] private string name;
        [SerializeField] private float speedMetersPerSecond;
        [SerializeField] private float widthMeters;
        [SerializeField] private int id;

        public Color EditorColor { get { return editorColor; } }
        public string Name { get { return name; } }
        public float SpeedMetersPerSecond { get { return speedMetersPerSecond; } }
        public float WidthMeters { get { return widthMeters; } }
        public int Id { get { return id; } }

        public RoadType(int id, string name, float speedMetersPerSecond, float widthMeters, Color editorColor)
        {
            this.id = id;
            this.name = name;
            this.speedMetersPerSecond = speedMetersPerSecond;
            this.widthMeters = widthMeters;
            this.editorColor = editorColor;
        }

        public void SetName(string value)
        {
            name = value;
        }

        public void SetSpeed(float value)
        {
            speedMetersPerSecond = value;
        }

        public void SetWidth(float value)
        {
            widthMeters = value;
        }

        public void SetColor(Color value)
        {
            editorColor = value;
        }
    }
}
