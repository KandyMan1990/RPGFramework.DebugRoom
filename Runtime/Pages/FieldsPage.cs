using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using RPGFramework.Core.Store;
using RPGFramework.DebugRoom.SharedTypes.Constants;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.SharedTypes.Stores;
using RPGFramework.Hashing;

namespace RPGFramework.DebugRoom
{
    /// <summary>Every field in the game's field database, each opening its spawn points, read from its bundle.</summary>
    internal sealed class FieldsPage : IDebugRoomList
    {
        private readonly IReadOnlyList<string>  m_FieldNames;
        private readonly IDebugSpawnPointLoader m_SpawnPointLoader;
        private readonly IFieldArgsStore        m_FieldArgsStore;
        private readonly IPlayTimeStore         m_PlayTimeStore;

        string IDebugRoomList.Title => "Fields";
        int IDebugRoomList.   Count => m_FieldNames.Count;

        internal FieldsPage(IDebugFieldDatabase    fieldDatabase,
                            IDebugSpawnPointLoader spawnPointLoader,
                            IFieldArgsStore        fieldArgsStore,
                            IPlayTimeStore         playTimeStore)
        {
            m_FieldNames       = fieldDatabase.FieldNames;
            m_SpawnPointLoader = spawnPointLoader;
            m_FieldArgsStore   = fieldArgsStore;
            m_PlayTimeStore    = playTimeStore;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return m_FieldNames[row];
        }

        async Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            string                         name        = m_FieldNames[row];
            ulong                          fieldId     = Fnv1a64.Hash(name);
            IReadOnlyList<DebugSpawnPoint> spawnPoints = await m_SpawnPointLoader.LoadAsync(fieldId);

            if (spawnPoints.Count == 0)
            {
                return DebugRoomChoice.Refused;
            }

            return DebugRoomChoice.Open(new SpawnPointList(name, fieldId, spawnPoints, m_FieldArgsStore, m_PlayTimeStore));
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }

    /// <summary>A field's spawn points, each a jump: the playthrough's clock starts, as New Game starts it.</summary>
    internal sealed class SpawnPointList : IDebugRoomList
    {
        private readonly string                         m_FieldName;
        private readonly ulong                          m_FieldId;
        private readonly IReadOnlyList<DebugSpawnPoint> m_SpawnPoints;
        private readonly IFieldArgsStore                m_FieldArgsStore;
        private readonly IPlayTimeStore                 m_PlayTimeStore;

        string IDebugRoomList.Title => m_FieldName;
        int IDebugRoomList.   Count => m_SpawnPoints.Count;

        internal SpawnPointList(string                         fieldName,
                                ulong                          fieldId,
                                IReadOnlyList<DebugSpawnPoint> spawnPoints,
                                IFieldArgsStore                fieldArgsStore,
                                IPlayTimeStore                 playTimeStore)
        {
            m_FieldName      = fieldName;
            m_FieldId        = fieldId;
            m_SpawnPoints    = spawnPoints;
            m_FieldArgsStore = fieldArgsStore;
            m_PlayTimeStore  = playTimeStore;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            DebugSpawnPoint spawnPoint = m_SpawnPoints[row];

            return $"{spawnPoint.Id.ToString(CultureInfo.InvariantCulture)}  {spawnPoint.Name}";
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            m_FieldArgsStore.Set(new FieldArgs(m_FieldId, m_SpawnPoints[row].Id));
            m_PlayTimeStore.StartCounting();

            return Task.FromResult(DebugRoomChoice.Leave(DebugRoomOutcomes.FIELD));
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }
}
