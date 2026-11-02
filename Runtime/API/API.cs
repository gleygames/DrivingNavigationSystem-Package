using UnityEngine;

namespace Gley.NavigationSystem
{
    public static class API
    {
        public static NavigationManager GetNavigationManager()
        {
#if UNITY_6000_4_OR_NEWER
            NavigationManager[] managers = Object.FindObjectsByType<NavigationManager>();
#else
            NavigationManager[] managers = Object.FindObjectsByType<NavigationManager>(FindObjectsSortMode.None);
#endif
            if (managers.Length == 0)
            {
                return null;
            }

            return managers[0];
        }
    }
}
