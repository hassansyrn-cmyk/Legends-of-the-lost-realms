# Fantasy UI Design 2 — inventory and capture validation

## Pre-change inventory

The pre-change project contained one scene, `Assets/Scenes/Main.unity`. It had no
authored Canvas or TextMeshPro hierarchy; the game and its legacy IMGUI were
created at runtime. The source/asset census was:

| Kind | Count |
|---|---:|
| C# files | 78 |
| Prefabs | 177 |
| FBX models | 423 |
| Materials | 137 |
| PNG textures | 172 |
| JPG textures | 135 |
| Shaders | 8 |

The gameplay remains runtime-built. Level 10 is loaded by `RealmGame.LoadLevel(10)`
for validation rather than represented by a separate authored scene.

## UI callback map

The Canvas UI delegates to the existing runtime API instead of duplicating game
state:

| UI action | Runtime callback/state |
|---|---|
| Continue/chapter selection/restart | `RealmGame.LoadLevel(level)` |
| Pause | `RealmGame.Pause()` |
| Resume | `RealmGame.Resume()` |
| Arsenal | `RealmGame.OpenArsenal()` |
| Change element | `RealmGame.CycleElement()` |
| Menu, atlas, sanctuary | Set `RealmGame.Screen` to `Menu`, `Map`, or `Settings` |
| Touch actions | Existing `TouchRouter` layout and `RealmGame` input flags |

The capture harness only assigns screen state, pauses/resumes, and loads Level 10.
It does not call `Persist`, alter `Progress`, reset the journey, complete a level,
or invoke upgrade/equipment callbacks.

## Build and capture

Prepare and build the Linux capture player from the project directory:

```sh
Unity -batchmode -quit -projectPath . \
  -executeMethod FantasyCaptureBuild.Linux
```

The build method calls `RealmBuild.Prepare()` and `FantasyUIAssets.Prepare()`,
then writes `Builds/FantasyCapture/LostRealmsFantasyCapture.x86_64`.

Run the player normally with the explicit opt-in argument:

```sh
mkdir -p Validation/FantasyUI-Captures
xvfb-run -a -s "-screen 0 1920x1080x24" \
  Builds/FantasyCapture/LostRealmsFantasyCapture.x86_64 \
  -fantasyUiCapture "$(pwd)/Validation/FantasyUI-Captures"
```

Do not use `-nographics`; `ScreenCapture.CaptureScreenshot` needs a real Unity
render surface. A desktop session or Xvfb with enough space for 1560x960 is
required. Do not add `-realmTest`: that starts the runtime probe suite, which
owns process exit and is unrelated to screenshot capture.

The player waits for `RealmGame.I`, its live Player, and the runtime Canvas,
loads the actual Level 10 world, and captures menu, paused, and playing states
at 1280x720, 1440x720, 1560x720, and 1280x960. It waits for settled rendered
frames after every resolution/state transition. `manifest.json` records both
requested and actual dimensions for all 12 PNGs. A resolution mismatch, startup
timeout, missing screenshot, or other capture error is logged clearly and exits
nonzero; a complete run exits zero.

This environment did not run Unity, so the report records the reproducible
procedure rather than claiming generated screenshots or a successful render.