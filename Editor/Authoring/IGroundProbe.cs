using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public interface IGroundProbe
    {
        bool Probe(Vector3 truePos, out float trueY);
    }
}
