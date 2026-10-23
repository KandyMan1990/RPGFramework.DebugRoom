# RPGFramework.DebugRoom

A module for the people making the game: jump to any field and spawn point, set any variable, try any song or sound and
start any battle, without playing through. Its pages read the game's own data — the field database, the variable map,
the audio providers, the arena database — so it grows with the game without being edited.

Requires Unity 6000.6 or newer, RPGFramework.Core, RPGFramework.Audio, RPGFramework.DI, RPGFramework.Hashing, and the
Core, Field, Battle and Debug Room shared types. **Compiled only where `ENABLE_RPG_FRAMEWORK_DEBUG` is defined**: add it
to a build profile's scripting defines — a debug profile beside your development and release ones — and the room, the
title's button to it and its routes are compiled in; leave it out and none of it is.

---

## The pages

| Page | Lists | Does |
| --- | --- | --- |
| **Fields** | every field, by name, each opening its spawn points, read from the field's bundle | jumps there |
| **Variables** | every variable in the map; an array's elements, a record's fields and an array's records each open a list of their own | shows each value in its width; left and right step it, held at the width's limits, and choosing a bool turns it over |
| **Audio** | every song, each opening Play, Crossfade and its stem states; every sound | plays a song, crossfades to it, fades the playing song to a stem state, plays a sound, stops the music, and steps both volumes for the session |
| **Battles** | every arena, then each battle flag, then the battle mode's two game-over flags | starts a battle in the arena with the flags ticked; a battle comes back to the room |

The buttons along the top choose a page, and the last one the size of a step — 1, 10, up to 1,000,000 — reached by
moving up from the first row. Cancel closes the list on top, and from a page's own list goes back to the title. It
needs only the moves, submit and cancel every menu has, and its sounds are your audio intents', as the menus' are.

**The room plays the playthrough it is opened in.** From the title, the Debug Room button begins one as New Game does:
every variable at its default, nothing written until a save is made. **From any field, hold L1, L2, R1, R2 and Select**
— both shoulders, both triggers and Select — and the room replaces the field, the playthrough carrying on as the field
left it. Either way the music stops as the room opens, so it starts silent. Set the story up on the Variables page, then jump, and the field plays as it would have; the playthrough's
clock starts with the jump. A battle from the room comes back to its Battles page,
the music the battle paused carrying on; a lost one goes to your game-over field unless the battle mode allows it, as
anywhere else.

## Setting it up

Everything below sits inside `#if ENABLE_RPG_FRAMEWORK_DEBUG`, or in an assembly with it as a define constraint.

1. **A scene for the room**, named for `ISceneDatabase`, with a `UIDocument` showing this package's
   `Runtime/UI/DebugRoom.uxml`, an `InputAdapter` with your UI actions, and a `SceneInstallerMonoBehaviour` with the
   room's installer. List the scene in the debug profile's own scene list, so other profiles leave it out.
2. **The room's installer**, binding your generated databases and the module:

   ```csharp
   internal sealed class DebugRoomSceneInstaller : SceneInstallerBase
   {
       public override void InstallBindings(IDIContainer container)
       {
           container.BindInterfacesToSelfSingleton<FieldDatabase>();
           container.BindSingleton<IDebugSpawnPointLoader, DebugSpawnPointLoader>();
           container.BindSingleton<IDebugBattleArenaDatabase, BattleArenaDatabase>();
           container.BindSingleton<IDebugRoomModule, DebugRoomModule>();
       }
   }
   ```

   `FieldDatabase` and `BattleArenaDatabase` are the classes the Field Designer's export and the arena database
   generate; export them again with this version for them to list their names. They are generated `internal`, so put
   this installer in the assembly they are generated into, or have that assembly grant the installer's access, inside
   the define:

   ```csharp
   #if ENABLE_RPG_FRAMEWORK_DEBUG
   [assembly: InternalsVisibleTo("YourGame.DebugRoom")]
   #endif
   ```

   `DebugSpawnPointLoader` is the Field package's.
3. **Your audio providers**, in your global installer, the same instances your players use:

   ```csharp
   container.BindSingletonFromInstance((IDebugMusicAssetProvider)musicProvider);
   container.BindSingletonFromInstance((IDebugSfxAssetProvider)sfxProvider);
   ```

4. **Its module id and scene**: `DebugRoomConstants.MODULE_ID => typeof(IDebugRoomModule)` in your `IModuleDatabase`,
   and its scene's name for `IDebugRoomModule` in your `ISceneDatabase`.
5. **The way back from a field**: give your field scene's `InputAdapter` actions for both shoulders, both triggers and
   Select; the buttons they are bound to are the combination. Five keys held at once are awkward on a keyboard, and
   some keyboards do not register them all, so bind one key to all five actions as well: holding it holds the whole
   combination.

The title's Debug Room button, the field's combination and the routes in and out are the Menu package's, the Field
package's and the default module router's, compiled in with the define.

## Not in this version

- **Scenarios** — named states written and loaded by the room — and **a game's own pages** are planned
  (`DEBUG_ROOM.md`, step 3); the four pages are the room's own.
- **The room is reached from the title only**, not opened over a field.
- **Its text is not localised**: it is for the people making the game.
- **A value is stepped, not typed**: a large one is reached by a larger step.
- **The bundled audio providers list nothing** where StreamingAssets cannot be listed, as on Android.
