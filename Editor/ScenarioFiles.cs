using System.IO;
using RPGFramework.DebugRoom.Authoring;

namespace RPGFramework.DebugRoom.Editor
{
    /// <summary>
    /// The project's scenario assets written as the save files the room reads, with an index of their names; and the
    /// saves the room made in play taken in as assets.
    /// </summary>
    internal sealed class ScenarioFiles
    {
        private readonly ScenarioLibrary m_Library;

        internal ScenarioFiles(ScenarioLibrary library)
        {
            m_Library = library;
        }

        /// <summary>Empties the folder, then writes every scenario in it, each named as a file can be on every platform.</summary>
        internal void WriteAll(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);

            ScenarioAsset[] scenarios = m_Library.FindAll();
            string[]        names     = new string[scenarios.Length];

            for (int i = 0; i < scenarios.Length; i++)
            {
                names[i] = ScenarioPaths.ToFileName(scenarios[i].name);

                File.WriteAllBytes(Path.Combine(folder, names[i] + ScenarioPaths.EXTENSION), m_Library.WriteSave(scenarios[i]));
            }

            File.WriteAllLines(Path.Combine(folder, ScenarioPaths.INDEX), names);
        }

        /// <summary>
        /// Each save in the folder made the scenario asset of its name, then removed; one that cannot be read is left,
        /// and false is returned.
        /// </summary>
        internal bool TakeIn(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return true;
            }

            string[] saves = Directory.GetFiles(folder, "*" + ScenarioPaths.EXTENSION);
            bool     taken = true;

            for (int i = 0; i < saves.Length; i++)
            {
                if (m_Library.TryCapture(Path.GetFileNameWithoutExtension(saves[i]), File.ReadAllBytes(saves[i])))
                {
                    File.Delete(saves[i]);
                }
                else
                {
                    taken = false;
                }
            }

            return taken;
        }
    }
}
