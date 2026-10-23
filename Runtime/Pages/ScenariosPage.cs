using System.Threading.Tasks;
using RPGFramework.Core.SaveData;
using RPGFramework.Core.Store;
using RPGFramework.DebugRoom.SharedTypes.Constants;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// The playthrough saved under a name of its own — on the device in a build, in the editor a scenario asset once play
    /// stops — and the scenarios there are, each loaded to go straight to where it was saved or to stay in the room. A
    /// scenario is a save file, kept apart from the player's saves.
    /// </summary>
    internal sealed class ScenariosPage : IDebugRoomList, IDebugRoomNaming
    {
        private const int SAVE_ROW = 0;

        private readonly IDebugSaveDataService m_SaveDataService;
        private readonly IPlayTimeStore        m_PlayTimeStore;
        private readonly ScenarioStore         m_Store;

        string IDebugRoomList.  Title  => "Scenarios";
        int IDebugRoomList.     Count  => 2;
        string IDebugRoomNaming.Prompt => "Save the playthrough as";

        internal ScenariosPage(IDebugSaveDataService saveDataService, IPlayTimeStore playTimeStore)
        {
            m_SaveDataService = saveDataService;
            m_PlayTimeStore   = playTimeStore;
            m_Store           = new ScenarioStore();
        }

        string IDebugRoomList.GetRowText(int row)
        {
            if (row == SAVE_ROW)
            {
                return m_Store.SavesOnDevice ? "Save the playthrough as a scenario on this device" : "Save the playthrough as a scenario asset, made as play stops";
            }

            return "Load a scenario";
        }

        async Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            if (row == SAVE_ROW)
            {
                return DebugRoomChoice.AskForName(this);
            }

            ScenarioEntry[] entries = await m_Store.LoadEntriesAsync();

            if (entries.Length == 0)
            {
                return DebugRoomChoice.Refused;
            }

            return DebugRoomChoice.Open(new ScenarioPickList(entries, m_Store, m_SaveDataService, m_PlayTimeStore));
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }

        DebugRoomChoice IDebugRoomNaming.Name(string name)
        {
            string fileName = ScenarioPaths.ToFileName(name);

            if (fileName.Length == 0)
            {
                return DebugRoomChoice.Refused;
            }

            m_Store.Save(fileName, m_SaveDataService.CaptureSave());

            return DebugRoomChoice.Done;
        }
    }

    /// <summary>Every scenario there is to load, those the room saved itself marked as such.</summary>
    internal sealed class ScenarioPickList : IDebugRoomList
    {
        private readonly ScenarioEntry[]       m_Entries;
        private readonly ScenarioStore         m_Store;
        private readonly IDebugSaveDataService m_SaveDataService;
        private readonly IPlayTimeStore        m_PlayTimeStore;

        string IDebugRoomList.Title => "Load";
        int IDebugRoomList.   Count => m_Entries.Length;

        internal ScenarioPickList(ScenarioEntry[] entries, ScenarioStore store, IDebugSaveDataService saveDataService, IPlayTimeStore playTimeStore)
        {
            m_Entries         = entries;
            m_Store           = store;
            m_SaveDataService = saveDataService;
            m_PlayTimeStore   = playTimeStore;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            ScenarioEntry entry = m_Entries[row];

            if (!entry.IsSaved)
            {
                return entry.Name;
            }

            return m_Store.SavesOnDevice ? $"{entry.Name}  (saved on this device)" : $"{entry.Name}  (saved in play)";
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            ScenarioList scenario = new ScenarioList(m_Entries[row], m_Store, m_SaveDataService, m_PlayTimeStore);

            return Task.FromResult(DebugRoomChoice.Open(scenario));
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }

    /// <summary>
    /// One scenario: loaded to go to the field it was saved in, entered by the spawn point the field was entered by, or
    /// loaded to stay in the room. Either begins the playthrough afresh from it, as a jump arrives rather than a load.
    /// </summary>
    internal sealed class ScenarioList : IDebugRoomList
    {
        private const int GO_ROW = 0;

        private readonly ScenarioEntry         m_Entry;
        private readonly ScenarioStore         m_Store;
        private readonly IDebugSaveDataService m_SaveDataService;
        private readonly IPlayTimeStore        m_PlayTimeStore;

        string IDebugRoomList.Title => m_Entry.Name;
        int IDebugRoomList.   Count => 2;

        internal ScenarioList(ScenarioEntry entry, ScenarioStore store, IDebugSaveDataService saveDataService, IPlayTimeStore playTimeStore)
        {
            m_Entry           = entry;
            m_Store           = store;
            m_SaveDataService = saveDataService;
            m_PlayTimeStore   = playTimeStore;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return row == GO_ROW ? "Load it and go to where it was saved" : "Load it and stay in the room";
        }

        async Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            if (!m_SaveDataService.TryBeginSave(await m_Store.ReadAsync(m_Entry)))
            {
                return DebugRoomChoice.Refused;
            }

            if (row != GO_ROW)
            {
                return DebugRoomChoice.Done;
            }

            m_PlayTimeStore.StartCounting();

            return DebugRoomChoice.Leave(DebugRoomOutcomes.SCENARIO);
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }
}
