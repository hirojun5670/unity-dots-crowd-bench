using UnityEditor;

[InitializeOnLoad]
public static class ForceRefreshOnPlay
{
    static ForceRefreshOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            AssetDatabase.Refresh();
        }
    }
}
