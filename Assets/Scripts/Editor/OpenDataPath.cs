#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class OpenDataPath
    {
        [MenuItem("Tools/Open Data Path")]
        private static void OpenDataPathInExplorer()
        {
            Process.Start(Application.persistentDataPath);
        }
    }
}
#endif