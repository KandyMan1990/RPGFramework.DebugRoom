# RPGFramework.DebugRoom

A module for the people making the game: jump to any field and spawn point, save and load named scenarios, set any
variable, try any song or sound and start any battle, without playing through. Its pages read the game's own data — the field database, the variable map,
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
| **Scenarios** | every scenario, by name, those saved on a device marked | saves the playthrough as a scenario under a name you type; loads one either to go straight to where it was saved, or to stay in the room |
| **Variables** | every variable in the map; an array's elements, a record's fields and an array's records each open a list of their own | shows each value in its width; left and right step it, held at the width's limits, and choosing a bool turns it over |
| **Audio** | every song, each opening Play, Crossfade and its stem states; every sound | plays a song, crossfades to it, fades the playing song to a stem state, plays a sound, stops the music, and steps both volumes for the session |
| **Battles** | every arena, then each battle flag, then the battle mode's two game-over flags | starts a battle in the arena with the flags ticked; a battle comes back to the room |

The buttons along the top choose a page, and the last one the size of a step — 1, 10, up to 1,000,000 — reached by
moving up from the first row. Cancel closes the list on top, and from a page's own list goes back to the title. It
needs only the moves, submit and cancel every menu has, and its sounds are your audio intents', as the menus' are.

**The room plays the playthrough it is opened in.** From the title, the Debug Room button begins one as New Game does:
every variable at its default, nothing written until a save is made. **From any field, hold L1, L2, R1, R2 and Select**
— both shoulders, both triggers and Select — and the room replaces the field, the playthrough carrying on as the field
left it. Either way the music stops as the room opens, so it starts silent. Set the story up on the Variables page, then
jump, and the field plays as it would have; the playthrough's clock starts with the jump. A battle from the room comes
back to its Battles page, the music the battle paused carrying on; a lost one goes to your game-over field unless the
battle mode allows it, as anywhere else.

**A scenario is an asset**, made with **Create › RPG Framework › Debug Room › Scenario** and kept in `Assets` with the
rest of the game: the values it sets apart from the variable map's defaults, every other persistent variable starting as
a new game does — the field, the spawn point and the module among them. Its inspector lists the map's persistent
variables in their own types, arrays and records opened out, with the Variable Map's own pickers — a field by name, a
spawn point from that field's — and keeps only what differs from the map, shown in bold, each with a reset. It is
authored whichever build profile is active.

**The room loads them as save files**, written from the assets as play begins in the editor and as a debug build is
made — into the project's `Temp` folder, `Assets` untouched — and added to the build's StreamingAssets; a build without
`ENABLE_RPG_FRAMEWORK_DEBUG` carries none. Loaded to go, a scenario arrives as a jump does: in its field, by its spawn
point. Loaded to stay, every variable is as it says, ready for the Variables page and a jump.

**Saving the playthrough as a scenario** in the editor keeps it as a save for the rest of play, listed and loadable
at once, and makes it an asset as play stops — a new one beside the project's others, in `Assets/Scenarios` for the
first, or the one of that name, its values replaced — ready to tidy up in its inspector. In a build, a tester's saves go
to the device's persistent data, in `DebugRoom/Scenarios`, listed with the build's and marked as the device's.

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

- **A game's own pages** are planned; the five pages are the room's own.
- **A scenario saved on a device is a save file**, not an asset: it loads in that build, and is not yet turned back into
  an asset in the editor.
- **The room is reached from the title only**, not opened over a field.
- **Its text is not localised**: it is for the people making the game.
- **A value is stepped, not typed**: a large one is reached by a larger step.
- **The bundled audio providers list nothing** where StreamingAssets cannot be listed, as on Android.
