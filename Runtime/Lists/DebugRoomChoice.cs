namespace RPGFramework.DebugRoom
{
    /// <summary>What choosing a row did, for the room to carry on from.</summary>
    internal readonly struct DebugRoomChoice
    {
        /// <summary>It acted, and the room stays on the list with its rows read again.</summary>
        internal static readonly DebugRoomChoice Done = new DebugRoomChoice(null, null, 0, false, true);

        /// <summary>As <see cref="Done" />, without the room's sound: the row played one of its own.</summary>
        internal static readonly DebugRoomChoice DoneQuietly = new DebugRoomChoice(null, null, 0, false, false);

        /// <summary>The row cannot be chosen now, which the room's error sound says.</summary>
        internal static readonly DebugRoomChoice Refused = new DebugRoomChoice(null, null, 0, true, false);

        internal IDebugRoomList   List      { get; }
        internal IDebugRoomNaming Naming    { get; }
        internal byte             Outcome   { get; }
        internal bool             IsRefused { get; }
        internal bool             Sounds    { get; }

        private DebugRoomChoice(IDebugRoomList list, IDebugRoomNaming naming, byte outcome, bool isRefused, bool sounds)
        {
            List      = list;
            Naming    = naming;
            Outcome   = outcome;
            IsRefused = isRefused;
            Sounds    = sounds;
        }

        /// <summary>The list the row opens, over the one it is on.</summary>
        internal static DebugRoomChoice Open(IDebugRoomList list)
        {
            return new DebugRoomChoice(list, null, 0, false, true);
        }

        /// <summary>The room asks for a name, typed, and gives it to <paramref name="naming" />.</summary>
        internal static DebugRoomChoice AskForName(IDebugRoomNaming naming)
        {
            return new DebugRoomChoice(null, naming, 0, false, true);
        }

        /// <summary>The room leaves with the outcome, for the game's module router.</summary>
        internal static DebugRoomChoice Leave(byte outcome)
        {
            return new DebugRoomChoice(null, null, outcome, false, true);
        }
    }
}
