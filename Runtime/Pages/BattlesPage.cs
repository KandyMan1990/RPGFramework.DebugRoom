using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Stores;
using RPGFramework.DebugRoom.SharedTypes.Constants;
using RPGFramework.Hashing;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// The arena to fight in, stepped through the game's arenas; every encounter in the game's encounter database, each
    /// starting a battle there with the flags set below them; and the battle mode's flags, which the router reads rather
    /// than a battle's own.
    /// </summary>
    internal sealed class BattlesPage : IDebugRoomList
    {
        private const int ARENA_ROW = 0;

        private static readonly BattleFlags[] m_ModeFlags = { BattleFlags.DISABLE_GAME_OVER_ON_TIME_OUT, BattleFlags.DISABLE_GAME_OVER_ON_DEFEAT };

        private readonly IReadOnlyList<string> m_ArenaNames;
        private readonly IReadOnlyList<string> m_EncounterNames;
        private readonly BattleFlags[]         m_BattleFlags;
        private readonly IBattleArgsStore      m_BattleArgsStore;
        private readonly IBattleModeStore      m_BattleModeStore;

        private BattleFlags m_Flags;
        private int         m_Arena;

        string IDebugRoomList.Title => "Battles";
        int IDebugRoomList.   Count => 1 + m_EncounterNames.Count + m_BattleFlags.Length + m_ModeFlags.Length;

        internal BattlesPage(IDebugBattleEncounterDatabase encounterDatabase, IDebugBattleArenaDatabase arenaDatabase, IBattleArgsStore battleArgsStore, IBattleModeStore battleModeStore)
        {
            m_ArenaNames      = arenaDatabase.ArenaNames;
            m_EncounterNames  = encounterDatabase.EncounterNames;
            m_BattleFlags     = BattleOwnFlags();
            m_BattleArgsStore = battleArgsStore;
            m_BattleModeStore = battleModeStore;
            m_Flags           = BattleFlags.DEFAULT;
            m_Arena           = 0;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            if (row == ARENA_ROW)
            {
                return $"In  < {m_ArenaNames[m_Arena]} >";
            }

            row--;

            if (row < m_EncounterNames.Count)
            {
                return $"Fight {m_EncounterNames[row]}";
            }

            row -= m_EncounterNames.Count;

            if (row < m_BattleFlags.Length)
            {
                return FlagText(m_BattleFlags[row], m_Flags, string.Empty);
            }

            return FlagText(m_ModeFlags[row - m_BattleFlags.Length], m_BattleModeStore.Mode, " (battle mode)");
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            if (row == ARENA_ROW)
            {
                m_Arena = (m_Arena + 1) % m_ArenaNames.Count;

                return Task.FromResult(DebugRoomChoice.Done);
            }

            row--;

            if (row < m_EncounterNames.Count)
            {
                m_BattleArgsStore.Set(new BattleArgs(Fnv1a64.Hash(m_EncounterNames[row]), Fnv1a64.Hash(m_ArenaNames[m_Arena]), m_Flags, 0));

                return Task.FromResult(DebugRoomChoice.Leave(DebugRoomOutcomes.BATTLE));
            }

            row -= m_EncounterNames.Count;

            if (row < m_BattleFlags.Length)
            {
                m_Flags ^= m_BattleFlags[row];
            }
            else
            {
                m_BattleModeStore.SetMode(m_BattleModeStore.Mode ^ m_ModeFlags[row - m_BattleFlags.Length]);
            }

            return Task.FromResult(DebugRoomChoice.Done);
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            int arena = m_Arena + Math.Sign(steps);

            if (row != ARENA_ROW || arena < 0 || arena >= m_ArenaNames.Count)
            {
                return false;
            }

            m_Arena = arena;

            return true;
        }

        private static string FlagText(BattleFlags flag, BattleFlags set, string suffix)
        {
            string box = (set & flag) == flag ? "[x]" : "[ ]";

            return $"{box} {flag}{suffix}";
        }

        // Every flag but the battle mode's, which a battle's own flags never carry.
        private static BattleFlags[] BattleOwnFlags()
        {
            BattleFlags[] all   = (BattleFlags[])Enum.GetValues(typeof(BattleFlags));
            int           count = 0;

            for (int i = 0; i < all.Length; i++)
            {
                if (IsBattleOwn(all[i]))
                {
                    count++;
                }
            }

            BattleFlags[] flags = new BattleFlags[count];
            int           next  = 0;

            for (int i = 0; i < all.Length; i++)
            {
                if (IsBattleOwn(all[i]))
                {
                    flags[next++] = all[i];
                }
            }

            return flags;
        }

        private static bool IsBattleOwn(BattleFlags flag)
        {
            return flag != BattleFlags.DEFAULT && Array.IndexOf(m_ModeFlags, flag) < 0;
        }
    }
}
