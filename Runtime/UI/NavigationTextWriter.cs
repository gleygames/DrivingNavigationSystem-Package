using System.Text;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public abstract class NavigationTextWriter : ScriptableObject
    {
        public abstract bool CanWrite(Component target);

        public abstract void Write(Component target, StringBuilder text);

        public abstract Component FindText(GameObject root);
    }
}
