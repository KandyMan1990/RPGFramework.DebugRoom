using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// Every Persistent and Session variable in the map, by name, each value shown and set in its width: a single value
    /// on the page itself, an array's elements, a record's fields and an array of records' records each in a list of
    /// their own.
    /// </summary>
    internal sealed class VariablesPage : IDebugRoomList
    {
        private readonly IMemoryService       m_MemoryService;
        private readonly VariableDefinition[] m_Variables;

        string IDebugRoomList.Title => "Variables";
        int IDebugRoomList.   Count => m_Variables.Length;

        internal VariablesPage(IMemoryService memoryService, IVariableMap variableMap)
        {
            m_MemoryService = memoryService;
            m_Variables     = InMemoryService(variableMap.Variables);
        }

        // The banks the memory service holds. A Temp variable is a running script's own, with no value outside it.
        private static VariableDefinition[] InMemoryService(IReadOnlyList<VariableDefinition> variables)
        {
            int count = 0;

            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i].Bank is MemoryBank.Persistent or MemoryBank.Session)
                {
                    count++;
                }
            }

            VariableDefinition[] inMemoryService = new VariableDefinition[count];
            int                  next            = 0;

            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i].Bank is MemoryBank.Persistent or MemoryBank.Session)
                {
                    inMemoryService[next++] = variables[i];
                }
            }

            return inMemoryService;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            VariableDefinition variable = m_Variables[row];

            if (IsSingleValue(variable))
            {
                return $"{variable.Name} = {Slot(variable).Read(m_MemoryService)}";
            }

            return variable.IsRecord && variable.Count == 1 ? $"{variable.Name} {{ }}" : $"{variable.Name} [{variable.Count}]";
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            VariableDefinition variable = m_Variables[row];

            if (IsSingleValue(variable))
            {
                return Task.FromResult(Slot(variable).Toggle(m_MemoryService) ? DebugRoomChoice.Done : DebugRoomChoice.Refused);
            }

            IDebugRoomList list = variable.IsRecord && variable.Count > 1
                                      ? new RecordList(variable, m_MemoryService)
                                      : new VariableSlotList(variable.Name, Slots(variable), m_MemoryService);

            return Task.FromResult(DebugRoomChoice.Open(list));
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            VariableDefinition variable = m_Variables[row];

            return IsSingleValue(variable) && Slot(variable).Step(m_MemoryService, steps);
        }

        private static bool IsSingleValue(VariableDefinition variable)
        {
            return !variable.IsRecord && variable.Count == 1;
        }

        private static VariableSlot Slot(VariableDefinition variable)
        {
            return new VariableSlot(variable.Name, variable.Bank, variable.Offset, variable.Width);
        }

        // An array's elements, or one record's fields, an array field's elements each a slot.
        private static VariableSlot[] Slots(VariableDefinition variable)
        {
            if (!variable.IsRecord)
            {
                VariableSlot[] elements = new VariableSlot[variable.Count];

                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i] = new VariableSlot($"{variable.Name}[{i}]", variable.Bank, variable.GetElementOffset(i), variable.Width);
                }

                return elements;
            }

            return RecordSlots(variable, 0, variable.Name);
        }

        internal static VariableSlot[] RecordSlots(VariableDefinition variable, int record, string recordName)
        {
            IReadOnlyList<VariableRecordField> fields = variable.Fields;
            int                                count  = 0;

            for (int i = 0; i < fields.Count; i++)
            {
                count += fields[i].Count;
            }

            VariableSlot[] slots       = new VariableSlot[count];
            int            next        = 0;
            int            fieldOffset = variable.GetElementOffset(record);

            for (int i = 0; i < fields.Count; i++)
            {
                VariableRecordField field = fields[i];
                int                 size  = field.Width.GetByteCount();

                for (int j = 0; j < field.Count; j++)
                {
                    string name = field.Count == 1 ? $"{recordName}.{field.Name}" : $"{recordName}.{field.Name}[{j}]";

                    slots[next++] = new VariableSlot(name, variable.Bank, fieldOffset + j * size, field.Width);
                }

                fieldOffset += size * field.Count;
            }

            return slots;
        }
    }

    /// <summary>The records of an array of them, each opening its fields.</summary>
    internal sealed class RecordList : IDebugRoomList
    {
        private readonly VariableDefinition m_Variable;
        private readonly IMemoryService     m_MemoryService;

        string IDebugRoomList.Title => m_Variable.Name;
        int IDebugRoomList.   Count => m_Variable.Count;

        internal RecordList(VariableDefinition variable, IMemoryService memoryService)
        {
            m_Variable      = variable;
            m_MemoryService = memoryService;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return $"{m_Variable.Name}[{row}]";
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            string         name  = $"{m_Variable.Name}[{row}]";
            VariableSlot[] slots = VariablesPage.RecordSlots(m_Variable, row, name);

            return Task.FromResult(DebugRoomChoice.Open(new VariableSlotList(name, slots, m_MemoryService)));
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }

    /// <summary>Values each shown and set in its width, as an array's elements or a record's fields.</summary>
    internal sealed class VariableSlotList : IDebugRoomList
    {
        private readonly string         m_Title;
        private readonly VariableSlot[] m_Slots;
        private readonly IMemoryService m_MemoryService;

        string IDebugRoomList.Title => m_Title;
        int IDebugRoomList.   Count => m_Slots.Length;

        internal VariableSlotList(string title, VariableSlot[] slots, IMemoryService memoryService)
        {
            m_Title         = title;
            m_Slots         = slots;
            m_MemoryService = memoryService;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return $"{m_Slots[row].Name} = {m_Slots[row].Read(m_MemoryService)}";
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            return Task.FromResult(m_Slots[row].Toggle(m_MemoryService) ? DebugRoomChoice.Done : DebugRoomChoice.Refused);
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return m_Slots[row].Step(m_MemoryService, steps);
        }
    }
}
