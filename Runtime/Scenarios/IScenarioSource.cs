using System.Threading.Tasks;

namespace RPGFramework.DebugRoom
{
    /// <summary>Somewhere scenarios are read from: a folder, or a build's StreamingAssets behind a URL.</summary>
    internal interface IScenarioSource
    {
        /// <summary>Every scenario's name, in order; none where there are none.</summary>
        Task<string[]> LoadNamesAsync();

        Task<byte[]> ReadAsync(string name);
    }
}
