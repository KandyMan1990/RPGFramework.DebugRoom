using System;
using System.Collections.Generic;
using RPGFramework.Core.Editor;
using RPGFramework.Core.Memory;
using UnityEngine;

namespace RPGFramework.DebugRoom.Authoring
{
    /// <summary>
    /// A state of the game the debug room can start from: the values it sets apart from the variable map's defaults,
    /// every other starting as a new game does — the field and spawn point among them. Written as a save file as play
    /// begins in the editor and as a debug build is made.
    /// </summary>
    [CreateAssetMenu(fileName = "Scenario", menuName = "RPG Framework/Debug Room/Scenario")]
    internal sealed class ScenarioAsset : ScriptableObject
    {
        internal const string VARIABLE_MAP = nameof(m_VariableMap);
        internal const string VALUES       = nameof(m_Values);

        [SerializeField]
        [Tooltip("The game's variable map, whose persistent variables the scenario sets")]
        private VariableMapAsset m_VariableMap;

        [SerializeField]
        private ScenarioValue[] m_Values = Array.Empty<ScenarioValue>();

        internal VariableMapAsset VariableMap => m_VariableMap;

        /// <summary>The persistent bank the scenario starts with: the map's defaults, with its own values in place.</summary>
        internal byte[] ToBank(IPersistentValues values)
        {
            byte[]                         bank       = values.CreateDefaultBank();
            IReadOnlyList<PersistentValue> persistent = values.Values;

            for (int i = 0; i < persistent.Count; i++)
            {
                for (int v = 0; v < m_Values.Length; v++)
                {
                    if (m_Values[v].IsFor(persistent[i]))
                    {
                        values.Write(bank, persistent[i], m_Values[v].Value);
                    }
                }
            }

            return bank;
        }

        /// <summary>Takes every value of <paramref name="bank" /> that differs from the map's default as its own.</summary>
        internal void TakeFrom(VariableMapAsset map, IPersistentValues values, byte[] bank)
        {
            IReadOnlyList<PersistentValue> persistent = values.Values;
            List<ScenarioValue>            taken      = new List<ScenarioValue>();

            for (int i = 0; i < persistent.Count; i++)
            {
                ulong bits = values.Read(bank, persistent[i]);

                if (bits != persistent[i].Default)
                {
                    taken.Add(new ScenarioValue(persistent[i], bits));
                }
            }

            m_VariableMap = map;
            m_Values      = taken.ToArray();
        }

        private void Reset()
        {
            m_VariableMap = new ScenarioLibrary().FindVariableMap();
        }
    }
}
