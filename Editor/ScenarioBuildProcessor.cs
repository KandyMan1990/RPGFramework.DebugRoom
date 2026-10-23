using System;
using RPGFramework.DebugRoom.Authoring;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;

namespace RPGFramework.DebugRoom.Editor
{
    /// <summary>
    /// Puts the project's scenario assets, as save files with an index of their names, in a debug build's
    /// StreamingAssets, written to the project's Temp folder so Assets is never touched. A build without the debug room's
    /// define gets none, though this is compiled only where the editor has it.
    /// </summary>
    internal sealed class ScenarioBuildProcessor : BuildPlayerProcessor
    {
        private const string DEFINE = "ENABLE_RPG_FRAMEWORK_DEBUG";

        public override int callbackOrder => 0;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (!HasDefine(buildPlayerContext.BuildPlayerOptions))
            {
                return;
            }

            new ScenarioFiles(new ScenarioLibrary()).WriteAll(ScenarioPaths.BuildFolder);

            buildPlayerContext.AddAdditionalPathToStreamingAssets(ScenarioPaths.BuildFolder, ScenarioPaths.FOLDER);
        }

        // The defines the player is compiled with: the build's own, the active build profile's, and the player settings'.
        private static bool HasDefine(BuildPlayerOptions options)
        {
            if (Array.IndexOf(options.extraScriptingDefines ?? Array.Empty<string>(), DEFINE) >= 0)
            {
                return true;
            }

            BuildProfile profile = BuildProfile.GetActiveBuildProfile();

            if (profile != null && Array.IndexOf(profile.scriptingDefines, DEFINE) >= 0)
            {
                return true;
            }

            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(options.targetGroup), out string[] defines);

            return Array.IndexOf(defines, DEFINE) >= 0;
        }
    }
}
