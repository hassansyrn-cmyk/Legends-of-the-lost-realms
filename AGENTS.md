# Project guardrails for AI agents working on Legend of the Lost Realms

Unity project lives in the inner folder `unity-3d` (Unity 6000.6.0f1). Branch: `chatgpt-astra/unity-3d`.

## CRITICAL: Aster character animation glitches

The character ("walk glitch", "growing/teleporting at walk start", horizontal snapping at idle->walk->run, sinking into the floor) has been FIXED. Do not regress it.

How the fix works (restore exactly this if it ever breaks):
- `unity-3d/Assets/Editor/AsterPhase1.cs` > `NormalizeRoot()` flattens **ALL** `RootT` curves (x/y/z) to constant 0 for every baked clip. The game capsule supplies all locomotion; ramped Mixamo root curves are the glitch source.
- `BuildPrefab()` reads `baselineRootY` from the idle source clip's `RootT.y` and pushes the model's `localPosition.y += baselineRootY * scale` so the sink from zeroing the root is baked into the prefab geometry.
- The clip files `unity-3d/Assets/Resources/Animations/Aster/*.anim` are **generated assets**. They are written ONLY by the Unity menu **`Lost Realms > Phase 1 > Prepare Aster Mixamo Character`**.
- After ANY change to an Aster clip FBX, the base model, or `AsterPhase1.cs`, regenerate via that menu. Never hand-edit or delete those `.anim` files, and never "fix" them another way.

What has gone wrong in the past (do not repeat):
- Replacing weapon FBX / reimporting weapon assets caused the fixed `.anim` clips to be lost; a rollback (`git reset --hard c1e81dd`) restored old glitchy clips and the glitch returned. The working commit for character animation is `c1e81dd`.
- A committed `.anim` set can be stale vs. the fixed baker; if the glitch reappears, first re-run the Phase 1 menu before changing any code.

## Locked physics (do not change)

`unity-3d/Assets/Scripts/Hero.cs`:
- All movement in `FixedUpdate` with `Time.fixedDeltaTime` (set to 1/60 in `RealmGame.Awake`); single `Controller.Move` per step.
- `MaxMoveSpeed=4.8f`, `GroundResponse=18f`, `AirResponse=8f`, `StopResponse=22f`, dash 9.5/0.22s/0.9s, jump vertical 8.4, apex float 11/23, fall clamp -20, jumpGrace 0.14, jumpBuffer 0.13, knockback 4.5.

## CI guards

- `.github/scripts/verify-aster-motion-tuning.sh`: `Hero.cs` must contain `const float MaxMoveSpeed=4.8f`, `Vector3.MoveTowards(velocity,desiredVelocity,response*dt)`, `"double_jump"`; forbidden `Visual.Restart("double_jump")`, `wish*6.1f`, `speedRatio*.82f`, `SetSpeed(3.1f)`.
- `.github/scripts/verify-weapon-system.sh`: `WeaponSystem.cs` must contain `Weapons/Aster_Axe`, `public sealed class WeaponDrop`, `public sealed class EquippedWeapon`, `model=created?created.transform:null;`; weapon FBX must stay under 2,000,000 bytes.

## Workflow

- User play-tests in the Unity Editor and cannot share screenshots; verify by asking the user.
- Only PowerShell is available (Windows; no bash/Python toolchain).
- Do not commit/push unless the user explicitly asks.
- Never run the Phase 1 menu for weapon-only changes.