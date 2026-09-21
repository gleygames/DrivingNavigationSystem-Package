using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public struct IntersectionRecord
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private int id;
        [SerializeField] private int firstLink;
        [SerializeField] private int linkCount;

        public Vector3 Position { get { return position; } }
        public int Id { get { return id; } }
        public int FirstLink { get { return firstLink; } }
        public int LinkCount { get { return linkCount; } }

        public IntersectionRecord(int id, Vector3 position, int firstLink, int linkCount)
        {
            this.id = id;
            this.position = position;
            this.firstLink = firstLink;
            this.linkCount = linkCount;
        }
    }
}
