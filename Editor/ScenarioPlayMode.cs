using RPGFramework.DebugRoom.Authoring;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.DebugRoom.Editor
{
    /// <summary>
    /// The scenario assets written as save files as play begins, for the room to load in the editor, and the saves the
    /// room made in play taken in as assets as play stops.
    /// </summary>
    [InitializeOnLoad]
    internal static class ScenarioPlayMode
    {
        static ScenarioPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            ScenarioFiles files = new ScenarioFiles(new ScenarioLibrary());

            if (change == PlayModeStateChange.ExitingEditMode)
            {
                files.WriteAll(ScenarioPaths.EditorFolder);
                return;
            }

            if (change == PlayModeStateChange.EnteredEditMode && !files.TakeIn(ScenarioPaths.CapturesFolder))
            {
                Debug.LogWarning($"Some scenarios saved in play could not be read as this project's variable map, and are left in {ScenarioPaths.CapturesFolder}");
            }
        }
    }
}
