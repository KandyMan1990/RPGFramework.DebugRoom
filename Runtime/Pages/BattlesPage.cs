using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Stores;
using RPGFramework.DebugRoom.SharedTypes.Constants;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// Every arena in the game's arena database, each starting a battle there with the flags set below them, and the
    /// battle mode's flags, which the router reads rather than a battle's own.
    /// </summary>
    internal sealed class BattlesPage : IDebugRoomList
    {
        private static readonly BattleFlags[] m_ModeFlags = { BattleFlags.DISABLE_GAME_OVER_ON_TIME_OUT, BattleFlags.DISABLE_GAME_OVER_ON_DEFEAT };

        private readonly IReadOnlyList<string> m_ArenaNames;
        private readonly BattleFlags[]         m_BattleFlags;
        private readonly IBattleArgsStore      m_BattleArgsStore;
        private readonly IBattleModeStore      m_BattleModeStore;

        private BattleFlags m_Flags;

        string IDebugRoomList.Title => "Battles";
        int IDebugRoomList.   Count => m_ArenaNames.Count + m_BattleFlags.Length + m_ModeFlags.Length;

        internal BattlesPage(IDebugBattleArenaDatabase arenaDatabase, IBattleArgsStore battleArgsStore, IBattleModeStore battleModeStore)
        {
            m_ArenaNames      = arenaDatabase.ArenaNames;
            m_BattleFlags     = BattleOwnFlags();
            m_BattleArgsStore = battleArgsStore;
            m_BattleModeStore = battleModeStore;
            m_Flags           = BattleFlags.DEFAULT;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            if (row < m_ArenaNames.Count)
            {
                return $"Fight in {m_ArenaNames[row]}";
            }

            row -= m_ArenaNames.Count;

            if (row < m_BattleFlags.Length)
            {
                return FlagText(m_BattleFlags[row], m_Flags, string.Empty);
            }

            return FlagText(m_ModeFlags[row - m_BattleFlags.Length], m_BattleModeStore.Mode, " (battle mode)");
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            if (row < m_ArenaNames.Count)
            {
                m_BattleArgsStore.Set(new BattleArgs(row, 0, m_Flags, 0, 0));

                return Task.FromResult(DebugRoomChoice.Leave(DebugRoomOutcomes.BATTLE));
            }

            row -= m_ArenaNames.Count;

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
            return false;
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
