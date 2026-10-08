# Grok upgrade pack (branch `Grok`)

Additive Unity 6 systems. They boot themselves after the scene loads via
`RuntimeInitializeOnLoadMethod(AfterSceneLoad)`.

**Does not touch** Aster Mixamo clips, `AsterPhase1.cs`, or locked locomotion
(`coyote 0.14`, `buffer 0.13`, jump `8.4`, move `4.8`, capsule `1.75 / 0.32`).

## What landed

| System | File | Effect |
| --- | --- | --- |
| Director | `GrokBootstrap.cs` | Spawns the pack once, DontDestroyOnLoad |
| Mobile quality | `GrokMobileQuality.cs` | 30/60 FPS, shadows, LOD, skin weights by GPU/RAM |
| Feel | `GrokFeel.cs` | Trauma² Perlin camera shake after FollowCamera, slash/dust/hurt sparks |
| VFX pool | `GrokVfxPool.cs` | 48 pooled billboards — no Instantiate in the hit path |
| Combat juice | `GrokCombatPlus.cs` | Counter flash, elite/boss auras, execution sparkles |
| Audio | `GrokAudioPlus.cs` | Whoosh stingers, music duck after RealmAudio writes volume |
| Content | `GrokContentPlus.cs` | Realm motes + up to 4 trail lanterns (2 lights) |
| Gameplay | `GrokGameplayPlus.cs` | Up to 3 aether wisps restore 14 energy on contact |
| Physics present | `GrokPhysicsPlus.cs` | Squash/stretch on Aster's visual child only |

## How to verify in Unity

1. Open `unity-3d` on branch `Grok`.
2. Play `Assets/Scenes/Main.unity`.
3. Console should log `GROK_QUALITY …`.
4. Hit an enemy — extra sparks + short camera trauma.
5. Walk the route — lanterns and collectible aether wisps.
6. Land from a jump — visual squash, dust puff.

Existing play-mode probes remain the source of truth for combat numbers.
This pack is presentation, a small optional energy pickup, and mobile quality.
