using RPGFramework.Core;
using RPGFramework.DebugRoom.SharedTypes;
using RPGFramework.DI;
using UnityEngine;

namespace RPGFramework.DebugRoom
{
    internal sealed class DebugRoomModuleMonoBehaviour : MonoBehaviour, IModuleBindings
    {
        void IModuleBindings.Bind(IDIContainer sceneContainer)
        {
            sceneContainer.BindSingletonIfNotRegistered<IDebugRoomModule, DebugRoomModule>();
        }
    }
}
