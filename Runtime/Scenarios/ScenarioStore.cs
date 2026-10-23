using System.Threading.Tasks;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// The scenarios a room can load — the project's, as the save files its assets are written as, read only — and those
    /// it saves itself: on the device in a build, for a tester to send on, and in the editor until play stops, when
    /// they become the project's assets.
    /// </summary>
    internal sealed class ScenarioStore
    {
        private readonly IScenarioSource m_Project;
        private readonly ScenarioFolder  m_Saved;

        /// <summary>A scenario saved stays on this device, rather than becoming one of the project's as play stops.</summary>
        internal bool SavesOnDevice { get; }

        internal ScenarioStore()
        {
#if UNITY_EDITOR
            m_Project     = new ScenarioFolder(ScenarioPaths.EditorFolder);
            m_Saved       = new ScenarioFolder(ScenarioPaths.CapturesFolder);
            SavesOnDevice = false;
#else
            m_Saved       = new ScenarioFolder(ScenarioPaths.DeviceFolder);
            SavesOnDevice = true;
#if UNITY_ANDROID || UNITY_WEBGL
            m_Project = new WebScenarioSource(ScenarioPaths.StreamingUrl);
#else
            m_Project = new ScenarioFolder(ScenarioPaths.StreamingFolder);
#endif
#endif
        }

        /// <summary>The project's scenarios, then those the room saved.</summary>
        internal async Task<ScenarioEntry[]> LoadEntriesAsync()
        {
            IScenarioSource saved   = m_Saved;
            string[]        project = await m_Project.LoadNamesAsync();
            string[]        own     = await saved.LoadNamesAsync();
            ScenarioEntry[] entries = new ScenarioEntry[project.Length + own.Length];

            for (int i = 0; i < project.Length; i++)
            {
                entries[i] = new ScenarioEntry(project[i], false);
            }

            for (int i = 0; i < own.Length; i++)
            {
                entries[project.Length + i] = new ScenarioEntry(own[i], true);
            }

            return entries;
        }

        internal Task<byte[]> ReadAsync(ScenarioEntry entry)
        {
            IScenarioSource source = entry.IsSaved ? m_Saved : m_Project;

            return source.ReadAsync(entry.Name);
        }

        /// <summary>Writes a save of the room's own, replacing one of the same name.</summary>
        internal void Save(string name, byte[] save)
        {
            m_Saved.Write(name, save);
        }
    }
}
