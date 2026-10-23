namespace RPGFramework.DebugRoom
{
    /// <summary>What a typed name is for, asked for by a row's <see cref="DebugRoomChoice.AskForName" />.</summary>
    internal interface IDebugRoomNaming
    {
        /// <summary>What the room shows beside the text field.</summary>
        string Prompt { get; }

        /// <summary>Acts on the name typed; refused for one it cannot take, the field staying open for another.</summary>
        DebugRoomChoice Name(string name);
    }
}
