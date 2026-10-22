using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Core;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// The game's songs and sounds, by name, from its providers: a song played, crossfaded to or switched between its
    /// stem states, a sound played, the music stopped, and both players' volumes stepped, for this session only.
    /// </summary>
    internal sealed class AudioPage : IDebugRoomList
    {
        private const int   MUSIC_ROW          = 0;
        private const int   SOUNDS_ROW         = 1;
        private const int   STOP_ROW           = 2;
        private const int   MUSIC_VOLUME_ROW   = 3;
        private const int   SOUND_VOLUME_ROW   = 4;
        private const float VOLUME_STEP        = 0.1f;
        private const float STEM_STATE_SECONDS = 1f;
        private const float STOP_SECONDS       = 1f;

        internal const float CROSSFADE_SECONDS = 2f;

        private readonly IDebugMusicAssetProvider m_MusicProvider;
        private readonly IDebugSfxAssetProvider   m_SfxProvider;
        private readonly IMusicPlayer             m_MusicPlayer;
        private readonly ISfxPlayer               m_SfxPlayer;

        // The song last played from here, which only its own stem states switch.
        private ulong m_PlayingSong;

        string IDebugRoomList.Title => "Audio";
        int IDebugRoomList.   Count => 5;

        internal AudioPage(IDebugMusicAssetProvider musicProvider,
                           IDebugSfxAssetProvider   sfxProvider,
                           IMusicPlayer             musicPlayer,
                           ISfxPlayer               sfxPlayer)
        {
            m_MusicProvider = musicProvider;
            m_SfxProvider   = sfxProvider;
            m_MusicPlayer   = musicPlayer;
            m_SfxPlayer     = sfxPlayer;
            m_PlayingSong   = 0;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return row switch
                   {
                       MUSIC_ROW        => "Music",
                       SOUNDS_ROW       => "Sounds",
                       STOP_ROW         => "Stop the music",
                       MUSIC_VOLUME_ROW => $"Music volume  {Percent(m_MusicPlayer.GetVolume())}",
                       _                => $"Sound volume  {Percent(m_SfxPlayer.GetVolume())}"
                   };
        }

        async Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            switch (row)
            {
                case MUSIC_ROW:
                    return DebugRoomChoice.Open(new SongList(this, await m_MusicProvider.LoadNamesAsync()));
                case SOUNDS_ROW:
                    return DebugRoomChoice.Open(new SoundList(m_SfxPlayer, await m_SfxProvider.LoadNamesAsync()));
                case STOP_ROW:
                    m_PlayingSong = 0;
                    m_MusicPlayer.StopAsync(STOP_SECONDS).FireAndForget();
                    return DebugRoomChoice.Done;
                default:
                    return DebugRoomChoice.Refused;
            }
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            float step = VOLUME_STEP * Math.Sign(steps);

            switch (row)
            {
                case MUSIC_VOLUME_ROW:
                    m_MusicPlayer.SetVolume(Mathf.Clamp01(m_MusicPlayer.GetVolume() + step));
                    return true;
                case SOUND_VOLUME_ROW:
                    m_SfxPlayer.SetVolume(Mathf.Clamp01(m_SfxPlayer.GetVolume() + step));
                    return true;
                default:
                    return false;
            }
        }

        internal Task<IReadOnlyList<string>> LoadStemStateNamesAsync(ulong song)
        {
            return m_MusicProvider.LoadStemStateNamesAsync(song);
        }

        internal void Play(ulong song, bool crossfade)
        {
            m_PlayingSong = song;

            Task playing = crossfade ? m_MusicPlayer.CrossfadeAsync(song, 0, CROSSFADE_SECONDS) : m_MusicPlayer.PlayAsync(song);

            playing.FireAndForget();
        }

        /// <summary>Fades the playing song to the stem state; false when the song is not the one playing.</summary>
        internal bool SwitchStemState(ulong song, ulong stemState)
        {
            if (song != m_PlayingSong || !m_MusicPlayer.IsPlaying())
            {
                return false;
            }

            m_MusicPlayer.SetStemStateFadeAsync(stemState, STEM_STATE_SECONDS).FireAndForget();

            return true;
        }

        private static string Percent(float volume)
        {
            return Mathf.RoundToInt(volume * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }
    }

    /// <summary>Every song, each opening what can be done with it.</summary>
    internal sealed class SongList : IDebugRoomList
    {
        private readonly AudioPage             m_Page;
        private readonly IReadOnlyList<string> m_Names;

        string IDebugRoomList.Title => "Music";
        int IDebugRoomList.   Count => m_Names.Count;

        internal SongList(AudioPage page, IReadOnlyList<string> names)
        {
            m_Page  = page;
            m_Names = names;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return m_Names[row];
        }

        async Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            ulong                 song       = Fnv1a64.Hash(m_Names[row]);
            IReadOnlyList<string> stemStates = await m_Page.LoadStemStateNamesAsync(song);

            return DebugRoomChoice.Open(new SongActionList(m_Page, m_Names[row], song, stemStates));
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }

    /// <summary>One song: played, crossfaded to, or, while it plays, switched to each of its stem states.</summary>
    internal sealed class SongActionList : IDebugRoomList
    {
        private const int PLAY_ROW      = 0;
        private const int CROSSFADE_ROW = 1;
        private const int ACTION_ROWS   = 2;

        private readonly AudioPage             m_Page;
        private readonly string                m_Name;
        private readonly ulong                 m_Song;
        private readonly IReadOnlyList<string> m_StemStates;

        string IDebugRoomList.Title => m_Name;
        int IDebugRoomList.   Count => ACTION_ROWS + m_StemStates.Count;

        internal SongActionList(AudioPage page, string name, ulong song, IReadOnlyList<string> stemStates)
        {
            m_Page       = page;
            m_Name       = name;
            m_Song       = song;
            m_StemStates = stemStates;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return row switch
                   {
                       PLAY_ROW      => "Play",
                       CROSSFADE_ROW => $"Crossfade to it over {AudioPage.CROSSFADE_SECONDS.ToString(CultureInfo.InvariantCulture)} seconds",
                       _             => $"Stem state: {m_StemStates[row - ACTION_ROWS]}"
                   };
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            if (row < ACTION_ROWS)
            {
                m_Page.Play(m_Song, row == CROSSFADE_ROW);

                return Task.FromResult(DebugRoomChoice.DoneQuietly);
            }

            bool switched = m_Page.SwitchStemState(m_Song, Fnv1a64.Hash(m_StemStates[row - ACTION_ROWS]));

            return Task.FromResult(switched ? DebugRoomChoice.DoneQuietly : DebugRoomChoice.Refused);
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }

    /// <summary>Every sound, each played as it is chosen.</summary>
    internal sealed class SoundList : IDebugRoomList
    {
        private readonly ISfxPlayer            m_SfxPlayer;
        private readonly IReadOnlyList<string> m_Names;

        string IDebugRoomList.Title => "Sounds";
        int IDebugRoomList.   Count => m_Names.Count;

        internal SoundList(ISfxPlayer sfxPlayer, IReadOnlyList<string> names)
        {
            m_SfxPlayer = sfxPlayer;
            m_Names     = names;
        }

        string IDebugRoomList.GetRowText(int row)
        {
            return m_Names[row];
        }

        Task<DebugRoomChoice> IDebugRoomList.ChooseAsync(int row)
        {
            m_SfxPlayer.PlayAsync(Fnv1a64.Hash(m_Names[row])).FireAndForget();

            return Task.FromResult(DebugRoomChoice.DoneQuietly);
        }

        bool IDebugRoomList.Step(int row, int steps)
        {
            return false;
        }
    }
}
