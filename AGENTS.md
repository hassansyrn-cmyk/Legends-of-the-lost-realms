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

## Combat feel (added Sep 2026, do not regress)

`unity-3d/Assets/Scripts/Enemy.cs`:
- Per-kind tuning tables `NoticeRange`, `ChaseSpeed`, `MeleeDamage` (15 entries now) used in `Update()` / `CommitAttack()`. Kinds 0-10 are the original families/bosses; 11 Flyer, 12 Bomber (exploder), 13 Summoner, 14 Elite. `RealmWorld.Build` spawns the new kinds from stage>=3.
- New behavior: Flyer (`aerial`) hovers 2.3m up, bobs, ignores the ground clamp, and has no blocking collider; Bomber detonates a `StrikeZone` and self-destructs; Summoner holds range and spawns capped (16) goblin minions; Elite has 2× health, a bigger model and `MeleeDamage` 3.
- Every enemy gets `EnemyAura`: 3 orbiting motes tinted to its weakness element via `RealmGame.ElementColors`.
- Hit feedback: `DamageTip` floating damage numbers + `HitSpark` bursts on all hits (enemy melee, bolts, telegraphed strikes, and hero attacks). The enemy emission flash on damage was REMOVED per the user (no orange tint on damage/death); the red charge glow during `State.Windup` is kept as the telegraph.
- Enemies block the player: `Enemy.Configure` adds a non-trigger `CapsuleCollider` (no contact damage — damage stays manual), and `MoveTo()` keeps enemies from crowding inside the hero.
- Death (`EnemyDeathFall`): the dying animation plays (rigged enemies run their death clip, static meshes tip over), then the body bursts to ash and vanishes **in place** — it never moves down through the platform. Do not reintroduce the falling version the user rejected.
- Do not remove `DamageTip`, the windup `Glow`/`ClearGlow`, the stat tables, the aura, or the enemy collider without user approval.

## Combat response (added Sep 2026, do not regress)

Player defense + elemental weakness layer:
- **Perfect dodge**: `Hero.Damage()` rewards a hit shrugged off inside the active dash window (`Elapsed<dashUntil`): +30 energy, a counter opening, and a short `HitStop`. `dodgeRewarded` gates it to once per dodge (reset when the dash starts).
- **Parry**: `Hero.Parry()` opens a `.2s` negation window (`.55s` cooldown, grounded only). `Hero.ParrySuccess()` negates the hit, stuns the nearest attacker (`Enemy.Stun`), refunds 20 energy, opens a counter, and fires `HitStop`. Aster has no parry clip, so it braces in the `"charged"` pose. Input: desktop `L`, mobile `PARRY` button.
- **Counter**: after a perfect dodge/parry, `counterUntil` makes the next `Hero.Attack` deal ×1.6 with a golden spark; HUD shows `COUNTER READY`. Do not change `Hero.CounterReady`.
- **Elemental weaknesses**: `Enemy.Weakness` (11 kinds) + `public int WeakElement`; matching the player's element (`Power` 0 ember / 1 frost / 2 gale) boosts damage ×1.5 and forces the elemental reaction even on physical melee (`Hero.Attack` passes `Power`). The enemy health bar fill + pip use `RealmGame.ElementColors[foe.WeakElement]`.
- `RealmGame.HitStop(seconds,scale)` dips `Time.timeScale` briefly; `RealmGame.Update` restores it via `Time.unscaledTime`. Physics stays on the constant fixed step, so this only slows pacing, never the step. Do not remove the restore line before `if(!I||!Player)return;`.
- The 5th touch action (`TouchRouter.Role.Parry`, `ActionRect` width 112 / step 120) and the 5-button HUD (`RealmGame.Control`) are deliberate; keep both in sync.

## Weapons

- `unity-3d/Assets/Resources/Weapons/*.fbx` are generated from the user's decimated GLBs (`AXE_decimated.glb`, `Long_Sword_decimated.glb`, `Curved_Sword_decimated.glb` in `~/Downloads`) via Blender 5.2 (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`). Keep each FBX under the 2,000,000-byte CI budget.
- Textures live in `Resources/Weapons/Textures/<name>_basecolor|_normal|_rm.jpg`. `WeaponCatalog.WeaponSkin()` builds the runtime Standard material from `_basecolor` + `_normal`; the `_rm` (glTF packed) map is reference only and is not bound to Standard.
- Normal maps need `textureType: 1` (NormalMap) in their `.meta`; the basecolor maps import at default (sRGB).
- Weapon positioning/orientation is driven by `WeaponDefinition.EquipEuler` and `ModelScale` in `WeaponSystem.cs`.
- **Asset Store weapons:** Blink `BlinkAxe=4`/`BlinkMace=5`/`BlinkSpear=6` and MYFG `AxeIron=7`…`SwordGreat=16` (4 axes + 3 one-hand + 3 two-hand swords) use imported low-poly models `Resources/Weapons/*.fbx` + `Resources/Weapons/Textures/*_basecolor.jpg` (512² JPG, material built at runtime by `WeaponSkin`). `RealmWorld.Build` forces chapter 1's drop to the greataxe (`stage==1?4:random.Next(1,17)`); the save clamp is `0,16`. Keep each weapon FBX small (these are all < 70 KB).
- `EquippedWeapon` keeps the weapon **sheathed on Aster's back** and draws it into the right hand during attacks (state starts with `attack_`/`charged`), re-sheathing ~1.1s after. The sheath is **geometry-based**: `Measure()` reads the weapon's real bounds (pivot-agnostic), anchors to the chest bone with `BackGap`, stands the blade up the spine, and twists so the flat lies against the body. The hand pose is driven by each `EquipEuler`. Do not reintroduce fixed local-space `SheathOffset`/`SheathEuler` — they float/clip because the FBX pivots are arbitrary.

## Enemy models (new families)

- The 4 new families use the user's high-detail showcase meshes from `~/Downloads/converted_models` (14 FBX, ~480k tris, 2048² basecolor, no rig), decimated+grounded+texture-reduced via Blender 5.2 and exported to `unity-3d/Assets/Resources/Characters/<Role>.fbx` (~17-26k tris, ~1.1-1.3 MB each):
  Flyer=`model_11` (energy orb), Bomber=`model_12` (lava rock golem), Summoner=`model_02` (wizard), Elite=`model_09` (crystal knight).
- Basecolor maps live at `Resources/Characters/Textures/<Role>_basecolor.jpg` (512²), bound at runtime by `CharacterVisual.CharacterSkin()` (same pattern as `WeaponSkin`). Roles without a texture are untouched, so the original prefab families keep their materials.
- These meshes have **no skeleton**, so `Enemy.Configure` adds `EnemyIdleMotion` (procedural bob + sway) when the visual has no `Animator`. If a model faces the wrong way, set the yaw in `EnemyIdleMotion.Setup` in `Enemy.Configure` (currently `0f`; `180f` if backward).
- The remaining 10 source models are unused and could restyle the existing families/bosses later.

## Environment props

- Imported low-poly nature packs (SimpleNaturePack/B&J Studio, k0rveen, BrokenVector, CreativeCup, Gigel3d) live under `Assets/`. Their scripts/demo scenes/archives were removed so the minimal project keeps compiling — keep it that way.
- `RealmProps.cs` scatters selected SimpleNaturePack models (trees/rocks/bushes/grass) around each island at build time (`RealmWorld.Build` calls `RealmProps.Scatter`). Models are copied to `Resources/Props/Nature/*` (fresh GUIDs) and a Standard material per realm is built from `Resources/Props/Textures/Nature_basecolor.png` (lush) or `Nature_snow.png` (realm 2 snow-converted palette). Do not delete those textures.
- `RealmScenery.IslandTop()` replaces the square slab top with a rounded, irregular ring mesh (the `Realm surface` cube renderer is hidden but kept as the size reference; `Walkable stone` still supplies collision). Ring quads must wind up (fan + strips face +Y) or the whole top inverts and the middle turns transparent. Distant background crags also get tops.
- Scattered props block the player: `RealmProps.AddCollider` gives trees a `CapsuleCollider` (~0.3 m) and rocks/bushes a shrunk `BoxCollider`; scenery oaks/pines get trunk capsules and edge rocks get sphere colliders. `Art.Shape` destroys colliders unless `solid=true`, so pass-through returns whenever new shapes are added without explicit colliders.

## Workflow

- User play-tests in the Unity Editor and cannot share screenshots; verify by asking the user.
- Only PowerShell is available (Windows; no bash/Python toolchain).
- Do not commit/push unless the user explicitly asks.
- Never run the Phase 1 menu for weapon-only changes.