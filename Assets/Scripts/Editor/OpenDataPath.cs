#if UNITY_EDITOR
using UnityEditor;
using System.Diagnostics;
using UnityEngine;

public class OpenDataPath
{
    [MenuItem("Tools/Open Data Path")]
    private static void OpenDataPathInExplorer()
    {
        Process.Start(Application.persistentDataPath);
    }
}
#endif