namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// A scenario by its name, and whether the room saved it itself — on this device in a build, in play in the editor —
    /// rather than it being one of the project's.
    /// </summary>
    internal readonly struct ScenarioEntry
    {
        internal string Name    { get; }
        internal bool   IsSaved { get; }

        internal ScenarioEntry(string name, bool isSaved)
        {
            Name    = name;
            IsSaved = isSaved;
        }
    }
}
