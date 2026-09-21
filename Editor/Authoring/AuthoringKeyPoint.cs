using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    [System.Serializable]
    public class AuthoringKeyPoint
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 inHandle;
        [SerializeField] private Vector3 outHandle;
        [SerializeField] private bool manualHandles;

        public Vector3 Position { get { return position; } }
        public Vector3 InHandle { get { return inHandle; } }
        public Vector3 OutHandle { get { return outHandle; } }
        public bool ManualHandles { get { return manualHandles; } }

        public AuthoringKeyPoint(Vector3 position)
        {
            this.position = position;
        }

        internal void SetPosition(Vector3 value)
        {
            position = value;
        }

        internal void SetInHandle(Vector3 value)
        {
            inHandle = value;
        }

        internal void SetOutHandle(Vector3 value)
        {
            outHandle = value;
        }

        internal void SetManualHandles(bool value)
        {
            manualHandles = value;
        }
    }
}
