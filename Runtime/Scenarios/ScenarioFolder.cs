using System;
using System.IO;
using System.Threading.Tasks;

namespace RPGFramework.DebugRoom
{
    /// <summary>Scenarios in a folder on disk, read and written as files.</summary>
    internal sealed class ScenarioFolder : IScenarioSource
    {
        private readonly string m_Path;

        internal ScenarioFolder(string path)
        {
            m_Path = path;
        }

        Task<string[]> IScenarioSource.LoadNamesAsync()
        {
            return Task.FromResult(LoadNames());
        }

        Task<byte[]> IScenarioSource.ReadAsync(string name)
        {
            return File.ReadAllBytesAsync(PathOf(name));
        }

        private string[] LoadNames()
        {
            if (!Directory.Exists(m_Path))
            {
                return Array.Empty<string>();
            }

            string[] files = Directory.GetFiles(m_Path, "*" + ScenarioPaths.EXTENSION);
            string[] names = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            Array.Sort(names, StringComparer.OrdinalIgnoreCase);

            return names;
        }

        /// <summary>Writes the scenario, replacing one of the same name.</summary>
        internal void Write(string name, byte[] save)
        {
            Directory.CreateDirectory(m_Path);
            File.WriteAllBytes(PathOf(name), save);
        }

        private string PathOf(string name)
        {
            return Path.Combine(m_Path, name + ScenarioPaths.EXTENSION);
        }
    }
}
