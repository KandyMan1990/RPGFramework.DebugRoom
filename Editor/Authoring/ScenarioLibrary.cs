using System.Collections.Generic;
using System.IO;
using RPGFramework.Core.Editor;
using RPGFramework.Core.Memory;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.DebugRoom.Authoring
{
    /// <summary>The project's scenario assets: found, written as save files, and made from saves the room captured.</summary>
    internal sealed class ScenarioLibrary
    {
        private const string FIRST_FOLDER = "Assets/Scenarios";

        // One per variable map, as laying a map's values out reads every variable.
        private readonly Dictionary<VariableMapAsset, IPersistentValues> m_ValuesByMap;

        internal ScenarioLibrary()
        {
            m_ValuesByMap = new Dictionary<VariableMapAsset, IPersistentValues>();
        }

        /// <summary>Every scenario in the project with a variable map to be written by.</summary>
        internal ScenarioAsset[] FindAll()
        {
            string[]            guids     = AssetDatabase.FindAssets("t:" + nameof(ScenarioAsset));
            List<ScenarioAsset> scenarios = new List<ScenarioAsset>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                ScenarioAsset scenario = AssetDatabase.LoadAssetAtPath<ScenarioAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));

                if (scenario != null && scenario.VariableMap != null)
                {
                    scenarios.Add(scenario);
                }
            }

            return scenarios.ToArray();
        }

        /// <summary>The scenario as a save file, as the game would have written one in that state.</summary>
        internal byte[] WriteSave(ScenarioAsset scenario)
        {
            IPersistentValues values = ValuesOf(scenario.VariableMap);

            return values.WriteSave(scenario.ToBank(values));
        }

        /// <summary>
        /// A save the room captured, as the scenario asset of that name with its values replaced, or a new one beside
        /// the project's others — in <c>Assets/Scenarios</c> for the first; false for a save that cannot be read, or
        /// with no variable map in the project.
        /// </summary>
        internal bool TryCapture(string name, byte[] save)
        {
            ScenarioAsset[]  scenarios = FindAll();
            ScenarioAsset    existing  = null;
            VariableMapAsset map       = FindVariableMap();

            for (int i = 0; i < scenarios.Length && existing == null; i++)
            {
                if (scenarios[i].name == name)
                {
                    existing = scenarios[i];
                    map      = existing.VariableMap;
                }
            }

            if (map == null)
            {
                return false;
            }

            IPersistentValues values = ValuesOf(map);

            if (!values.TryReadSave(save, out byte[] bank))
            {
                return false;
            }

            if (existing != null)
            {
                existing.TakeFrom(map, values, bank);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);

                return true;
            }

            string folder = scenarios.Length > 0 ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(scenarios[0])).Replace('\\', '/') : FIRST_FOLDER;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder), Path.GetFileName(folder));
            }

            ScenarioAsset captured = ScriptableObject.CreateInstance<ScenarioAsset>();

            captured.TakeFrom(map, values, bank);
            AssetDatabase.CreateAsset(captured, folder + "/" + name + ".asset");

            return true;
        }

        /// <summary>The project's variable map, the first if there are several.</summary>
        internal VariableMapAsset FindVariableMap()
        {
            string[] maps = AssetDatabase.FindAssets("t:" + nameof(VariableMapAsset));

            return maps.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<VariableMapAsset>(AssetDatabase.GUIDToAssetPath(maps[0]));
        }

        /// <summary>The map's persistent values, laid out once.</summary>
        internal IPersistentValues ValuesOf(VariableMapAsset map)
        {
            if (!m_ValuesByMap.TryGetValue(map, out IPersistentValues values))
            {
                values             = new PersistentValues(map, map);
                m_ValuesByMap[map] = values;
            }

            return values;
        }
    }
}
