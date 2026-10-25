using UnityEditor;
using UnityEngine;

namespace RPGFramework.DebugRoom.Editor
{
    internal static class DebugRoomModuleCreator
    {
        [MenuItem("GameObject/RPG Framework/Debug Room/Debug Room Module", false, 10)]
        private static void Create(MenuCommand command)
        {
            GameObject module = new GameObject("Debug Room Module", typeof(DebugRoomModuleMonoBehaviour));

            GameObjectUtility.SetParentAndAlign(module, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(module, "Create Debug Room Module");
            Selection.activeObject = module;
        }
    }
}
