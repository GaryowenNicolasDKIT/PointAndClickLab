using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class DevLog
{
    // Calls to this method are compiled only in the Editor and in Development Builds.
    [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
    public static void Info(string message, UnityEngine.Object context = null)
    {
        Debug.Log(message, context);
    }
}