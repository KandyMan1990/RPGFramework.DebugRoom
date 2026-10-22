namespace RPGFramework.DebugRoom
{
    /// <summary>A list open in the room, and the row the cursor was on as another opened over it.</summary>
    internal readonly struct DebugRoomLevel
    {
        internal IDebugRoomList List       { get; }
        internal int            FocusedRow { get; }

        internal DebugRoomLevel(IDebugRoomList list, int focusedRow)
        {
            List       = list;
            FocusedRow = focusedRow;
        }
    }
}
