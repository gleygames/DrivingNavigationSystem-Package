using System.Diagnostics;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace Gley.NavigationSystem
{
    internal static class CustomLogger
    {
        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Log(object message, Object context = null)
        {
            UnityDebug.Log(message, context);
        }

        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context = null)
        {
            UnityDebug.LogWarning(message, context);
        }

        public static void LogError(object message, Object context = null)
        {
            UnityDebug.LogError(message, context);
        }
    }
}
