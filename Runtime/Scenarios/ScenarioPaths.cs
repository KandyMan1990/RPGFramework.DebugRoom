using System.IO;
using UnityEngine;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// Where scenarios are read as save files: those the project's scenario assets are written as, in the project's
    /// Temp folder for the editor and for a debug build's StreamingAssets; and those the room saves itself, in a
    /// device's persistent data in a build, and in the editor in Temp until play stops and they become assets.
    /// </summary>
    internal static class ScenarioPaths
    {
        internal const string FOLDER    = "DebugRoom/Scenarios";
        internal const string EXTENSION = ".sav";

        /// <summary>The names in a build's folder, one to a line, where the folder cannot be listed.</summary>
        internal const string INDEX = "index.txt";

        private const string NOT_IN_FILE_NAMES = "<>:\"/\\|?*";

        internal static string StreamingFolder => Path.Combine(Application.streamingAssetsPath, FOLDER);
        internal static string StreamingUrl    => Application.streamingAssetsPath + "/" + FOLDER;
        internal static string DeviceFolder    => Path.Combine(Application.persistentDataPath, FOLDER);

#if UNITY_EDITOR
        internal static string EditorFolder   => Path.Combine(TempFolder, FOLDER);
        internal static string CapturesFolder => Path.Combine(TempFolder, "DebugRoom", "Captures");
        internal static string BuildFolder    => Path.Combine(TempFolder, "DebugRoomBuild", FOLDER);

        private static string TempFolder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp");
#endif

        /// <summary>
        /// The name as a file can hold it on every platform, since the files are shared: trimmed, without the full stops
        /// and spaces a file name cannot end in, and with any character one cannot have made an underscore; empty when
        /// nothing is left.
        /// </summary>
        internal static string ToFileName(string name)
        {
            char[] chars = name.Trim().TrimEnd('.', ' ').ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] < ' ' || NOT_IN_FILE_NAMES.IndexOf(chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
