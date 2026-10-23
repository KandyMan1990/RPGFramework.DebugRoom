using System;
using RPGFramework.Core.Editor;
using UnityEngine;

namespace RPGFramework.DebugRoom.Authoring
{
    /// <summary>
    /// One value a scenario sets apart from the map's default: a variable by its permanent id, the element of an array
    /// or the record, and for a record the field by its id and the element of a field that is an array.
    /// </summary>
    [Serializable]
    internal struct ScenarioValue
    {
        internal const string VARIABLE_ID = nameof(m_VariableId);
        internal const string INDEX       = nameof(m_Index);
        internal const string FIELD_ID    = nameof(m_FieldId);
        internal const string FIELD_INDEX = nameof(m_FieldIndex);
        internal const string VALUE       = nameof(m_Value);

        [SerializeField]
        private uint m_VariableId;
        [SerializeField]
        private int m_Index;
        [SerializeField]
        [Tooltip("The record's field, by its id; 0 for a value or an array of values")]
        private ushort m_FieldId;
        [SerializeField]
        private int m_FieldIndex;
        [SerializeField]
        [Tooltip("The value as the bytes it occupies in the bank, lowest first")]
        private ulong m_Value;

        internal ulong Value => m_Value;

        internal ScenarioValue(PersistentValue value, ulong bits)
        {
            m_VariableId = value.VariableId;
            m_Index      = value.Index;
            m_FieldId    = value.FieldId;
            m_FieldIndex = value.FieldIndex;
            m_Value      = bits;
        }

        internal bool IsFor(PersistentValue value)
        {
            return m_VariableId == value.VariableId && m_Index == value.Index && m_FieldId == value.FieldId && m_FieldIndex == value.FieldIndex;
        }
    }
}
