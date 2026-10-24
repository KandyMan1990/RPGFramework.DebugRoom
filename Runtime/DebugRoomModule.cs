using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Audio.Music;
using RPGFramework.Battle.SharedTypes;
using RPGFramework.Battle.SharedTypes.Stores;
using RPGFramework.Core;
using RPGFramework.Core.Audio;
using RPGFramework.Core.Input;
using RPGFramework.Core.Memory;
using RPGFramework.Core.SaveData;
using RPGFramework.Core.SharedTypes;
using RPGFramework.Core.Store;
using RPGFramework.DebugRoom.SharedTypes;
using RPGFramework.DebugRoom.SharedTypes.Constants;
using RPGFramework.DI;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.SharedTypes.Stores;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// The debug room: pages that jump to any field and spawn point, save and load named scenarios, set any variable,
    /// try the game's audio and start any battle, reading the game's own data so they grow with it. It plays the playthrough it was opened in — one the
    /// title begins for it, or the one a field was playing — for its pages to set up before a jump. A jump or a battle
    /// is reported for the game's module router to carry out, a battle coming back here, and backing out of the first
    /// list goes back to the title.
    /// </summary>
    public sealed class DebugRoomModule : IDebugRoomModule, IInputContext
    {
        private const int   BATTLES_PAGE         = 4;
        private const int   LARGEST_STEP         = 1000000;
        private const float MUSIC_RESUME_SECONDS = 1f;

        private readonly ICoreModule          m_CoreModule;
        private readonly IDIResolver          m_DIResolver;
        private readonly IInputRouter         m_InputRouter;
        private readonly IAudioIntentPlayer   m_AudioIntentPlayer;
        private readonly IPausedMusicStore    m_PausedMusicStore;
        private readonly IMusicPlayer         m_MusicPlayer;
        private readonly IModuleScene         m_ModuleScene;
        private readonly IDebugRoomList[]     m_Pages;
        private readonly List<DebugRoomLevel> m_Levels;
        private readonly DebugRoomUI          m_UI;

        private InputAdapter     m_InputAdapter;
        private IDebugRoomNaming m_Naming;
        private int              m_NamingRow;
        private int              m_Page;
        private int              m_Step;
        private bool             m_Busy;

        public DebugRoomModule(ICoreModule               coreModule,
                               IDIResolver               diResolver,
                               IInputRouter              inputRouter,
                               IAudioIntentPlayer        audioIntentPlayer,
                               IDebugSaveDataService     saveDataService,
                               IPlayTimeStore            playTimeStore,
                               IPausedMusicStore         pausedMusicStore,
                               IMemoryService            memoryService,
                               IVariableMap              variableMap,
                               IMusicPlayer              musicPlayer,
                               ISfxPlayer                sfxPlayer,
                               IDebugMusicAssetProvider  musicProvider,
                               IDebugSfxAssetProvider    sfxProvider,
                               IDebugFieldDatabase       fieldDatabase,
                               IDebugSpawnPointLoader    spawnPointLoader,
                               IFieldArgsStore           fieldArgsStore,
                               IDebugBattleArenaDatabase arenaDatabase,
                               IBattleArgsStore          battleArgsStore,
                               IBattleModeStore          battleModeStore,
                               IModuleScene              moduleScene)
        {
            m_CoreModule        = coreModule;
            m_DIResolver        = diResolver;
            m_InputRouter       = inputRouter;
            m_AudioIntentPlayer = audioIntentPlayer;
            m_PausedMusicStore  = pausedMusicStore;
            m_MusicPlayer       = musicPlayer;
            m_ModuleScene       = moduleScene;
            m_Levels            = new List<DebugRoomLevel>();
            m_Naming            = null;
            m_NamingRow         = 0;
            m_Page              = 0;
            m_Step              = 1;
            m_Busy              = false;

            m_Pages = new IDebugRoomList[]
                      {
                          new FieldsPage(fieldDatabase, spawnPointLoader, fieldArgsStore, playTimeStore),
                          new ScenariosPage(saveDataService, playTimeStore),
                          new VariablesPage(memoryService, variableMap),
                          new AudioPage(musicProvider, sfxProvider, musicPlayer, sfxPlayer),
                          new BattlesPage(arenaDatabase, battleArgsStore, battleModeStore)
                      };

            string[] pageNames = new string[m_Pages.Length];

            for (int i = 0; i < m_Pages.Length; i++)
            {
                pageNames[i] = m_Pages[i].Title;
            }

            UIDocument document = m_ModuleScene.Find<UIDocument>();

            m_UI = new DebugRoomUI(document.rootVisualElement, pageNames);

            m_UI.RowChosen   += OnRowChosen;
            m_UI.RowStepped  += OnRowStepped;
            m_UI.PageChosen  += OnPageChosen;
            m_UI.StepChosen  += OnStepChosen;
            m_UI.NameEntered += OnNameEntered;
            m_UI.CursorMoved += OnCursorMoved;
        }

        // Back from a battle the room shows its battles again, and the music the battle paused carries on, as the
        // field's does.
        Task IModule.OnEnterAsync(byte entry)
        {
            m_InputAdapter = m_ModuleScene.Find<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);

            bool musicPaused = m_PausedMusicStore.TryTake(out MusicSnapshot pausedMusic);

            if (entry == ModuleEntries.RETURN)
            {
                if (musicPaused)
                {
                    m_MusicPlayer.ResumeAsync(pausedMusic, MUSIC_RESUME_SECONDS).FireAndForget();
                }

                m_Page = BATTLES_PAGE;
            }

            m_InputRouter.Push(this);

            ShowPage(m_Page);

            return Task.CompletedTask;
        }

        void IModule.OnRevealed()
        {
            SetInputEnabled(true);
        }

        Task IModule.OnExitAsync()
        {
            SetInputEnabled(false);
            m_InputRouter.Pop(this);

            return Task.CompletedTask;
        }

        Task IModule.OnSuspendAsync()
        {
            SetInputEnabled(false);

            return Task.CompletedTask;
        }

        Task IModule.OnResumeAsync()
        {
            return Task.CompletedTask;
        }

        // Cancel closes the name being typed, or else the list on top; every press is the room's.
        bool IInputContext.Handle(ControlSlot slot)
        {
            if (m_Busy || slot != ControlSlot.Secondary)
            {
                return true;
            }

            if (m_Naming != null)
            {
                PlaySound(AudioIntent.Cancel);
                CloseNaming();
            }
            else
            {
                Back();
            }

            return true;
        }

        bool IInputContext.HandleMove(Vector2 move) => true;

        private void SetInputEnabled(bool enabled)
        {
            m_UI.SetInputEnabled(enabled);

            if (enabled)
            {
                m_InputAdapter.Enable();
            }
            else
            {
                m_InputAdapter.Disable();
            }
        }

        private void ShowPage(int page)
        {
            m_Page = page;
            m_Levels.Clear();
            m_Levels.Add(new DebugRoomLevel(m_Pages[page], 0));

            m_UI.SetPage(page);
            ShowTop(0);
        }

        private void ShowTop(int focusedRow)
        {
            IDebugRoomList list  = Top();
            string[]       texts = new string[list.Count];

            for (int i = 0; i < texts.Length; i++)
            {
                texts[i] = list.GetRowText(i);
            }

            m_UI.SetRows(texts, Mathf.Min(focusedRow, texts.Length - 1));
            m_UI.SetPath(Path());
            ShowStep();
        }

        private void ShowStep()
        {
            string step = m_Step.ToString("N0", CultureInfo.InvariantCulture);

            m_UI.SetStep($"Step {step}");
            m_UI.SetHint($"Left, right: a value by {step}    Up from the first row: pages and step    Cancel: back");
        }

        private IDebugRoomList Top()
        {
            return m_Levels[m_Levels.Count - 1].List;
        }

        private string Path()
        {
            StringBuilder path = new StringBuilder();

            for (int i = 0; i < m_Levels.Count; i++)
            {
                path.Append(i == 0 ? string.Empty : "  >  ").Append(m_Levels[i].List.Title);
            }

            return path.ToString();
        }

        // Ten times larger each time, back to one after the largest.
        private void OnStepChosen()
        {
            if (IsWaiting())
            {
                return;
            }

            m_Step = m_Step >= LARGEST_STEP ? 1 : m_Step * 10;

            PlaySound(AudioIntent.Navigate);
            ShowStep();
        }

        private void Back()
        {
            PlaySound(AudioIntent.Cancel);

            if (m_Levels.Count == 1)
            {
                Leave(DebugRoomOutcomes.CLOSED);
                return;
            }

            m_Levels.RemoveAt(m_Levels.Count - 1);
            ShowTop(m_Levels[m_Levels.Count - 1].FocusedRow);
        }

        private void OnPageChosen(int page)
        {
            if (IsWaiting())
            {
                return;
            }

            PlaySound(AudioIntent.Navigate);
            ShowPage(page);
        }

        private void OnRowChosen(int row)
        {
            if (!IsWaiting())
            {
                ChooseAsync(row).FireAndForget();
            }
        }

        // Nothing is taken while a row loads what it opens, as a field's spawn points from its bundle.
        private async Task ChooseAsync(int row)
        {
            m_Busy = true;
            m_UI.SetInputEnabled(false);

            DebugRoomChoice choice;

            try
            {
                choice = await Top().ChooseAsync(row);
            }
            finally
            {
                m_Busy = false;
                m_UI.SetInputEnabled(true);
            }

            if (choice.IsRefused)
            {
                PlaySound(AudioIntent.Error);
                return;
            }

            if (choice.Sounds)
            {
                PlaySound(AudioIntent.Navigate);
            }

            if (choice.Outcome != 0)
            {
                Leave(choice.Outcome);
                return;
            }

            if (choice.Naming != null)
            {
                m_Naming    = choice.Naming;
                m_NamingRow = row;
                m_UI.AskForName(m_Naming.Prompt);
                return;
            }

            if (choice.List != null)
            {
                m_Levels[m_Levels.Count - 1] = new DebugRoomLevel(Top(), row);
                m_Levels.Add(new DebugRoomLevel(choice.List, 0));
                ShowTop(0);
                return;
            }

            ShowTop(row);
        }

        private void OnRowStepped(int row, int direction)
        {
            if (IsWaiting())
            {
                return;
            }

            IDebugRoomList list = Top();

            if (!list.Step(row, direction * m_Step))
            {
                PlaySound(AudioIntent.Error);
                return;
            }

            PlaySound(AudioIntent.Navigate);
            m_UI.SetRowText(row, list.GetRowText(row));
        }

        // A name the list cannot take leaves the field open for another.
        private void OnNameEntered(string name)
        {
            if (m_Busy || m_Naming == null)
            {
                return;
            }

            DebugRoomChoice choice = m_Naming.Name(name);

            if (choice.IsRefused)
            {
                PlaySound(AudioIntent.Error);
                return;
            }

            if (choice.Sounds)
            {
                PlaySound(AudioIntent.Navigate);
            }

            CloseNaming();
        }

        private void CloseNaming()
        {
            m_Naming = null;
            m_UI.CloseNameEntry();
            ShowTop(m_NamingRow);
        }

        // While a name is typed, only the name entry and Cancel act.
        private bool IsWaiting()
        {
            return m_Busy || m_Naming != null;
        }

        private void Leave(byte outcome)
        {
            m_Busy = true;
            SetInputEnabled(false);

            m_CoreModule.RequestModuleChangeAsync(outcome).FireAndForget();
        }

        private void OnCursorMoved()
        {
            PlaySound(AudioIntent.Navigate);
        }

        private void PlaySound(AudioIntent intent)
        {
            m_AudioIntentPlayer.PlayAsync(intent, AudioContext.Menu).FireAndForget();
        }
    }
}
