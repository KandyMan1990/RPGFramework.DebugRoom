#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RPGFramework.Core.Input;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// The actions the debug room reads, declared for the editor's check of its scene's map: the UI map, as its pages
    /// are UI Toolkit's.
    /// </summary>
    internal sealed class DebugRoomActions : IRequiredActions
    {
        private static readonly RequiredAction[] m_Required =
        {
            new RequiredAction(CoreActions.CANCEL, RequiredActionKind.Button, "closes the name being typed, or the list on top")
        };

        Type IRequiredActions.Module => typeof(DebugRoomModuleMonoBehaviour);

        string IRequiredActions.Map => "UI";

        bool IRequiredActions.Moves => false;

        IReadOnlyList<RequiredAction> IRequiredActions.Actions => m_Required;
    }
}
#endif
