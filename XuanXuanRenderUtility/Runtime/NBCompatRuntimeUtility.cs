using UnityEngine;

namespace NBShader
{
    public static class UnityObjectFindCompat
    {
        public static T FindAny<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<T>();
#else
#pragma warning disable 0618
            return Object.FindObjectOfType<T>();
#pragma warning restore 0618
#endif
        }

        public static T[] FindAll<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
#pragma warning disable 0618
            return Object.FindObjectsOfType<T>();
#pragma warning restore 0618
#endif
        }
    }
}
