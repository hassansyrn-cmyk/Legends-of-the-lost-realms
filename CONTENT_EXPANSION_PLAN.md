# Lost Realms — Content Expansion & Gameplay Improvement Plan

Goal: grow the game from 15 chapters / 4 realms into a long-tail action RPG
without regressing anything locked in `AGENTS.md` (Aster animation fix,
physics constants, CI guards, trap pipeline, touch rects).

How to execute each phase: `csc` standalone compile → editor probes
(`PropCensus`, `OrganizeSweep`, `RuntimeProbe -realmTest`) → the six
`verify-*.sh` CI scripts → playtest → Android APK. No phase merges with
probe failures or CI red. Commit per phase, never push unless asked.

---

## PART A — CONTENT EXPANSION

### A1. Realm 5: Stormveil Expanse (chapters 16–20) — L
New realm = the single biggest content drop. Follow the Emberfall pattern
(Round 8) file by file.

- Routes `RouteX/RouteY` stages 16–20 (stage 20 = 8-island boss climb);
  `IsBoss` gains 20; realm split `Level<=4?0:Level<=7?1:Level<=10?2:Level<=15?3:4`.
- Palette/atmosphere: storm-violet accent, lightning ambience, rain volumes
  (reuse `ga_vfx_Rain_01` pattern), 5th textures + palettes, zenith/horizon.
- Decor: 1 new prop set (storm pines/crystals/rocks) under `Resources/Props/Storm`,
  routed in `RealmProps.Folder/Set`; realm-4 branch in village mats (clamp 0..4).
- Enemies: kinds 22/23/24 via the rig pipeline (`rig_prep.py` pattern, ~24k tris,
  `SkeletonLimbMotion` drivers): Stormcaller (ranged), Galehound (fast melee),
  Tempest Herald (flying). Spawn rules from stage>=11 on free indices.
- Guardian: STORMVEIL MATRIARCH (kind 22, phase-3 "STORM SURGE"; Weakness=0/ember
  so every realm keeps a different weakness). Reuse `LavaBossCombat` dispatch
  shape with new bolt color/speed; heralds = Galehound.
- Traps: 2 new Tripo models through the proven GLB pipeline
  (apply → decimate ~40k → ground → game scale → 512 PNGs → FBX, identity import
  verified in Blender, `LoadTextured` + preserved root rotation): storm coil
  (chain-lightning totem) and gale fan (push trap). Unlock 16+/18+.
- Music: `storm_exploration_theme` + boss uses `boss_battle_theme`; menu stays
  `verdant_theme`. Wire into `SetMusic` stageTrack branches.
- Save migration: stars/best arrays 15→20 copy-migrate; `unlocked` clamp 1..20;
  treasury "/60 STARS"; menu "/20 CHAPTERS". `RuntimeProbe` extended to 11–20
  spawns + warden/matron kind checks + `Validation/Stormveil.png`.
- Gate thresholds (60/80/95) apply unchanged — percentages normalize automatically.

### A2. Arsenal round 3: weapons 41–50 — M
Same pipeline as round 2 (`export_enemy_restyle.py` pattern, under-2MB FBX
budget enforced by `verify-weapon-system.sh`): 2 axes (chop), 3 blades (slash),
2 spears (spear), 1 knuckle set (forced `unarmed`), 1 staff, 1 chakram.
Clamp `-1,50`, pool `random.Next(1,51)`. Styles assign automatically by grip.
Bake 128px previews via `WeaponIconProbe` (delete after use, as before).

### A3. New game mode: Rift Echoes (seeded challenge runs) — L
- Seeded daily/weekly run: fixed seed → deterministic routes/traps/enemy kinds
  (reuse `Random(stage*793)` pattern with a date seed), fixed loadout rules.
- Best-time + best-score per rift stored in `Progress` (new arrays, migrated).
- Reuses Trial UI patterns (`RealmTrials`) for the rift card + rewards.
- No backend: local-only leaderboard (best self). Online leaderboard explicitly
  out of scope (needs server + anti-cheat — see D4).

### A4. Sanctuary row: 2 new gem tracks — M
- **SWIFT** (+6% move speed/rank, cost 5+2r gems) — capped so `MaxMoveSpeed`
  CI guard (`4.8f`) still passes: implement as reduced ground friction feel,
  not a constant change (or bump guard + docs together — decide in design).
- **GREED** (+15% coin/gem pickup radius +10%/rank, cost 4+2r gems).
- Rows must stay inside the 1280×720 IMGUI layout; keep rects in sync per the
  Touch UI pass rules.

### A5. Bestiary / codex — S/M
Read-only gallery from existing data: enemy roles, weakness elements
(`ElementColors`), boss phases, trap list. Unlocks entries on first kill/seen
(new `Save.codex` bitmask, migrated). Zero gameplay risk, high completionist
value. Pairs with the 95%-star chase.

### A6. Boss rush arena (post-campaign) — M
Unlocks after level 20: back-to-back guardians (8/9/10/21/22) with heal
pickups between rounds, single-life, gem shower payout. Reuses guardian spawn
+ phase code; new small arena route (5 islands). Gives veterans a reason to
keep their built characters.

---

## PART B — GAMEPLAY IMPROVEMENTS

### B1. Combat readability (do first — cheap, high value) — S
- Telegraph audit: every Windup already glows red; standardize telegraph
  durations per realm (later realms telegraph *shorter* instead of only hitting
  harder — skill-based difficulty).
- Off-screen danger arrows already exist; extend to trap telegraphs
  (`CombatTelegraph` positions) on boss arenas.
- Hit-stop audit: keep dips ≤0.05s so pacing never stutters (see Combat feel).

### B2. Enemy AI variety pack — M
- Pack hunters (kind 16): shared-aggro pincer (reuse aggro-sharing + flank
  offsets, tighten to ±40° pairs).
- Ranged kiting (7/13/17) already kites under 3.4m; add lateral strafe while
  on token cooldown so backline feels alive.
- Bomber (12): fuse audio ramp (reuse `boss_warning` at low pitch) + red pulse
  0.5s before detonation — counterplay readability.
- Affix tells already exist; add affix combo names to `DamageTip` on first hit
  ("SHIELDED SWIFT!") for learnability.

### B3. Traversal feel, second pass — S/M
- Keep locked physics (`MaxMoveSpeed=4.8f`, fixed step). Improvements around it:
  grapple aim assist (snap to nearest route island within cone), ferry-riding
  camera lead (less motion sickness on Orbit movers), landing-dust tuning.
- Optional: glide feather pickup (slow-fall 3s, rare spawn on wide islands) —
  new pickup type through the existing `RealmPickup` magnet pipeline.

### B4. Onboarding (biggest new-player win) — M
- Chapter 1 is trap-free already; add contextual tips: first dart turret →
  strafe tip, first crusher → telegraph tip, first parryable enemy → parry tip.
  One-line `Tell()` triggers, once per save (new `Save.tipsSeen` bitmask).
- Sanctuary: one-line role descriptions already on rows; add a "recommended
  first buy" highlight (cheapest unmaxed track pulses).

### B5. UI/UX pass with ui-ux-pro-max skill (now installed globally) — M
- Menu composition already centered; apply the skill to: HUD information
  hierarchy (route compass vs minimap overlap check at 1280×720 AND notch
  phones), Sanctuary row scanning order, boss title-card timing, damage-number
  readability (outline + size curve already exist — tune).
- Accessibility: text scaling respect in IMGUI styles, colorblind-safe
  element colors (ember/frost/gale currently red/blue/green — add shapes).
- No TouchRouter rect changes without syncing `RealmGame.RoundButton` drawing.

### B6. Performance budget (mobile APK ~96MB) — M
- Per-realm texture audit (512² standard already; kill 2048² stragglers).
- Mesh audit: keep new families ≤26k tris, traps ≤40k (see Asset Audit skill).
- Particle cap audit on 4x-effect fights (Warden + heralds + bolts).
- Cold-start time: measure `LoadLevel(1)` wall time headless; budget <8s.

### B7. Audio round 2 (after the SFX diversification) — S
- Footstep alternation exists; add surface variation (stone vs bridge wood)
  via raycast tag check at step time.
- Boss phase stingers already per-kind pitch; add low-HP heartbeat under 25%
  on guardian fights (reuse `_Damage` vignette pulse clock).
- Mix audit: cap concurrent `enemy_warning` voices on 3+-enemy pulls.

---

## PART C — EXECUTION ORDER

| Phase | Contents | Gates |
|---|---|---|
| 1 | B1 readability + B4 onboarding + B7 audio | probes + playtest |
| 2 | A4 sanctuary tracks + A5 bestiary | save-migration tests |
| 3 | A2 weapons 41–50 + B2 AI variety | restyle-rollback rule: prove takes/bounds via probe before deleting |
| 4 | A1 Realm 5 (biggest) | full 20-chapter sweep + APK |
| 5 | A3 rifts + A6 boss rush | economy review (gem sinks vs payouts) |
| 6 | B3 traversal + B5 UI pass + B6 perf | device testing, APK size check |

Do NOT start A1 before the asset pipeline skills (`asset-spec`, `asset-audit`)
have signed off the model budgets. Do NOT touch locked physics, the Aster
baker, or token-gate rules without a design note first.

## Explicitly out of scope
Online multiplayer / leaderboards (server + anti-cheat), engine upgrade,
console ports, voice acting, full re-restyles without probe proof.

## Open decisions for you
1. Realm 5 theme lock: Stormveil (storm) vs Tidehollow (water) vs Ashfall (ash)?
2. Chapters 16–20, or a shorter 16–18 cap?
3. NG+ / prestige: yes as part of A3, or separate phase?
