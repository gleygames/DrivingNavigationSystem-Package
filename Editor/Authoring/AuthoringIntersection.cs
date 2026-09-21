using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [System.Serializable]
    public class AuthoringIntersection
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private int id;

        public Vector3 Position { get { return position; } }
        public int Id { get { return id; } }

        public AuthoringIntersection(int id, Vector3 position)
        {
            this.id = id;
            this.position = position;
        }

        internal void SetPosition(Vector3 value)
        {
            position = value;
        }
    }
}
