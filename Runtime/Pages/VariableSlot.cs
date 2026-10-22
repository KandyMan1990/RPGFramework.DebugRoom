using System;
using System.Globalization;
using RPGFramework.Core;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// One value in a memory bank, by the name a script reads it by: a variable, an element of an array, or a field of
    /// a record. Read and stepped in its width, held at the width's limits rather than wrapping past them.
    /// </summary>
    internal readonly struct VariableSlot
    {
        internal string Name { get; }

        private readonly MemoryBank    m_Bank;
        private readonly ushort        m_Address;
        private readonly VariableWidth m_Width;

        internal VariableSlot(string name, MemoryBank bank, int offset, VariableWidth width)
        {
            Name      = name;
            m_Bank    = bank;
            m_Address = (ushort)offset;
            m_Width   = width;
        }

        internal string Read(IMemoryService memory)
        {
            return m_Width switch
                   {
                       VariableWidth.Bool   => memory.ReadBool(m_Bank, m_Address) ? "true" : "false",
                       VariableWidth.SByte  => ((sbyte)memory.ReadByte(m_Bank, m_Address)).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.Byte   => memory.ReadByte(m_Bank, m_Address).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.Short  => ((short)memory.ReadUshort(m_Bank, m_Address)).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.UShort => memory.ReadUshort(m_Bank, m_Address).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.Int    => memory.ReadInt(m_Bank, m_Address).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.UInt   => ((uint)memory.ReadInt(m_Bank, m_Address)).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.Long   => ((long)memory.ReadUlong(m_Bank, m_Address)).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.ULong  => memory.ReadUlong(m_Bank, m_Address).ToString(CultureInfo.InvariantCulture),
                       VariableWidth.Float  => memory.ReadFloat(m_Bank, m_Address).ToString(CultureInfo.InvariantCulture),
                       _                    => "?"
                   };
        }

        /// <summary>Turns a bool over; false for any other width, which is stepped instead.</summary>
        internal bool Toggle(IMemoryService memory)
        {
            if (m_Width != VariableWidth.Bool)
            {
                return false;
            }

            memory.WriteBool(m_Bank, m_Address, !memory.ReadBool(m_Bank, m_Address));

            return true;
        }

        /// <summary>
        /// Adds <paramref name="steps" />, or turns a bool over; false when the value is at the limit it steps towards.
        /// </summary>
        internal bool Step(IMemoryService memory, int steps)
        {
            switch (m_Width)
            {
                case VariableWidth.Bool:
                    return Toggle(memory);
                case VariableWidth.Float:
                    memory.WriteFloat(m_Bank, m_Address, memory.ReadFloat(m_Bank, m_Address) + steps);
                    return true;
                case VariableWidth.ULong:
                    return StepUnsignedLong(memory, steps);
            }

            long value   = ReadInteger(memory);
            long stepped = Clamp(value, steps, Minimum(m_Width), Maximum(m_Width));

            if (stepped == value)
            {
                return false;
            }

            WriteInteger(memory, stepped);

            return true;
        }

        private bool StepUnsignedLong(IMemoryService memory, int steps)
        {
            ulong value    = memory.ReadUlong(m_Bank, m_Address);
            ulong distance = (ulong)Math.Abs((long)steps);
            ulong stepped  = steps < 0
                                 ? value < distance ? 0UL : value - distance
                                 : value > ulong.MaxValue - distance ? ulong.MaxValue : value + distance;

            if (stepped == value)
            {
                return false;
            }

            memory.WriteUlong(m_Bank, m_Address, stepped);

            return true;
        }

        private long ReadInteger(IMemoryService memory)
        {
            return m_Width switch
                   {
                       VariableWidth.SByte  => (sbyte)memory.ReadByte(m_Bank, m_Address),
                       VariableWidth.Byte   => memory.ReadByte(m_Bank, m_Address),
                       VariableWidth.Short  => (short)memory.ReadUshort(m_Bank, m_Address),
                       VariableWidth.UShort => memory.ReadUshort(m_Bank, m_Address),
                       VariableWidth.Int    => memory.ReadInt(m_Bank, m_Address),
                       VariableWidth.UInt   => (uint)memory.ReadInt(m_Bank, m_Address),
                       _                    => (long)memory.ReadUlong(m_Bank, m_Address)
                   };
        }

        private void WriteInteger(IMemoryService memory, long value)
        {
            switch (m_Width)
            {
                case VariableWidth.SByte:
                case VariableWidth.Byte:
                    memory.WriteByte(m_Bank, m_Address, (byte)value);
                    break;
                case VariableWidth.Short:
                case VariableWidth.UShort:
                    memory.WriteUshort(m_Bank, m_Address, (ushort)value);
                    break;
                case VariableWidth.Int:
                case VariableWidth.UInt:
                    memory.WriteInt(m_Bank, m_Address, (int)value);
                    break;
                default:
                    memory.WriteUlong(m_Bank, m_Address, (ulong)value);
                    break;
            }
        }

        private static long Clamp(long value, int steps, long minimum, long maximum)
        {
            if (steps < 0)
            {
                return value < minimum - steps ? minimum : value + steps;
            }

            return value > maximum - steps ? maximum : value + steps;
        }

        private static long Minimum(VariableWidth width)
        {
            return width switch
                   {
                       VariableWidth.SByte => sbyte.MinValue,
                       VariableWidth.Short => short.MinValue,
                       VariableWidth.Int   => int.MinValue,
                       VariableWidth.Long  => long.MinValue,
                       _                   => 0L
                   };
        }

        private static long Maximum(VariableWidth width)
        {
            return width switch
                   {
                       VariableWidth.SByte  => sbyte.MaxValue,
                       VariableWidth.Byte   => byte.MaxValue,
                       VariableWidth.Short  => short.MaxValue,
                       VariableWidth.UShort => ushort.MaxValue,
                       VariableWidth.Int    => int.MaxValue,
                       VariableWidth.UInt   => uint.MaxValue,
                       _                    => long.MaxValue
                   };
        }
    }
}
