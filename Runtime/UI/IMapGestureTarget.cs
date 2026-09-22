using UnityEngine;

namespace Gley.NavigationSystem
{
    internal interface IMapGestureTarget
    {
        void Pan(Vector2 delta);
        void Zoom(float factor, Vector2 pivot);
        void Tap(Vector2 pos);
    }
}
