using System.Text;
using UnityEngine;

namespace Gley.NavigationSystem
{
    public abstract class NavigationTextTarget : MonoBehaviour
    {
        public abstract void SetText(StringBuilder text);
    }
}
