using System.Threading.Tasks;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// One list the room shows: a page's own, or one a row of it opened. Each row is a line of text; choosing one acts,
    /// opens another list or refuses, and stepping one changes the value it shows.
    /// </summary>
    internal interface IDebugRoomList
    {
        /// <summary>What the room shows the list as, after the lists it was opened from.</summary>
        string Title { get; }

        int Count { get; }

        string GetRowText(int row);

        Task<DebugRoomChoice> ChooseAsync(int row);

        /// <summary>
        /// Steps the row's value by <paramref name="steps" />, negative for down; false when the row has no value to
        /// step or it is already at its limit.
        /// </summary>
        bool Step(int row, int steps);
    }
}
