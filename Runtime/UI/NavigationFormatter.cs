using System.Text;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public abstract class NavigationFormatter : ScriptableObject
    {
        public abstract void FormatDistance(float meters, StringBuilder output);
        public abstract void FormatDuration(float seconds, StringBuilder output);
    }
}
