# Legends of the Lost Realms

**Legends of the Lost Realms** is an original landscape Android 2D fantasy platformer. This illustrated build contains ten short handcrafted-style levels across the Verdant Kingdom, Burning Dunes, and Frozen Peaks; local save data; touch controls; collectible coins and realm gems; fire, ice, and wind powers; checkpoints; upgrades; and three multi-phase boss encounters.

## Illustrated Asset Refresh

The gameplay renderer now uses original illustrated assets rather than simplified Canvas shapes for Aster, four enemy varieties, three boss variants, world scenery, platforms, spikes, checkpoints, coins, and realm gems. The Android-ready resources are optimized copies under `app/src/main/res/drawable-nodpi/`; the corresponding original art is retained under `art/`. This revision validates genuine PNG alpha transparency before packaging, so the generated assets no longer render with checkerboard or white backing boxes.

## Motion and Sound Refresh

The game applies lightweight animation cycles to all available sprites: Aster gains idle breathing, run cadence, jump movement, attack lean, and power pulses; enemies hover, bob, and squash; bosses breathe and pulse; platforms sway; hazards pulse; collectible coins spin; realm gems shimmer; checkpoint banners sway; and upgrade tokens rotate or glow.

### Audio Expansion v2.1.0

The soundscape now includes **28 original low-latency effects**. In addition to menu actions, jumps, attacks, impacts, damage, collection, checkpoints, upgrades, and completion, the game now reacts audibly to footsteps, light and hard landings, enemy warnings, crawler dashes, moth swoops, power selection, insufficient energy, respawning, boss hazard warnings, and Ember/Frost/Gale casts. The music changes automatically for Burning Dunes, Frozen Peaks, and boss arenas, while Verdant Kingdom retains its exploration score.

## Controls

| Control | Action |
| --- | --- |
| Left and right arrows | Move Aster; double-tap an arrow while grounded to dash and briefly dodge incoming damage |
| Jump | Jump; tap again in air for a double jump; press while sliding on a wall to wall-jump or climb a ledge |
| Blade | Basic attack; press in the air for a downward dive slash that bounces Aster after a hit |
| Power | Cast the selected ability. Tap the power panel in the HUD to cycle Ember, Frost, and Gale. |
| Pause | Pause menu |

## Movement Phase 1

Aster can now dash through a quick double-tap on either directional button, then carries a brief slide at the end of the dash. Holding toward the side of a platform while falling starts a wall slide; pressing Jump performs a wall jump, or climbs the ledge when near its top. The first seconds of each level display a short on-screen control hint.

## Combat Phase 2

The dash now doubles as a short defensive dodge. Aster can also use a dive slash in the air, bouncing upward after connecting with an enemy. Legacy enemies now use distinct warning colours and attacks: Dune Skirmishers lunge and retreat, Frost Sentinels pulse in close range, and Wind Wisps burst toward the player.

## World Redesign Phase 3

Burning Dunes and Frozen Peaks now use their own handcrafted layouts rather than the former shared legacy template. Sunscorched Pass and Frostwind Climb introduce lower routes alongside higher gem routes. Temple of Keys and Crystal Hollow use alternating brittle platforms for timing challenges. Both boss arenas now contain purpose-built escape platforms that support the new movement and dodge mechanics.

## Progression and Rewards Phase 4

Every level now records up to three permanent Realm Stars: finish the level, collect at least one realm gem, and finish without taking damage. Each earned star adds bonus coins to that run, and the World Map displays the best score beneath each level. Realm Stars are never spent; reaching 6, 14, and 24 stars unlocks Realm Relic ranks in the Upgrades screen. Each rank grants one additional maximum heart, ten maximum energy, and a small power-cost reduction.

## Story and World Identity Phase 5

Short, skippable story cards now introduce each realm at levels 1, 5, and 8, then introduce each guardian at boss levels 4, 7, and 10. Boss victories receive distinct world conclusions that link the restored Heart fragment to the next destination. Story cards pause gameplay briefly, include a Heart fragment shimmer, and can be dismissed immediately with a tap.

## Quality of Life Phase 6

The Settings screen now stores independent music and effects switches. While in a level, a compact Trail bar shows route progress and the checkpoint marker; the marker changes colour after activation. The expanded pause screen shows the current trail percentage, coins, and gems, then lets the player resume, restart the level, or return to the World Map. Music pauses with the pause screen and resumes when play continues.

## Stability Fixes v2.7.1

Boss power damage now observes the same short hit lock used by normal enemies, preventing one active ability from dealing frame-by-frame damage. The victory cue is now triggered only by level completion, eliminating the duplicated boss-win sound. Returning to the app now respects the manual pause screen, keeping music paused until the player resumes or opens the World Map.

## App Icon and Resource Cleanup v2.7.2

The application now declares a dedicated original launcher icon based on the Heart of Realms crystal and Aster's sword. Only lint-confirmed unused legacy Aster art was removed after checking the current sprite-sheet references. This reduces raw resource weight while retaining all images used by the game engine.

## Refactor Phase 1 v2.8.0

The first architecture pass keeps gameplay behavior intact while introducing `InputHandler`, `GameRenderer`, `UIRenderer`, `PlayerController`, `CombatSystem`, `EnemyController`, and `BossController`. `GameView` now coordinates these layers, and the core combat methods are formatted for clearer maintenance. Level data and the remaining scene state intentionally stay in their existing behavior-preserving form until the next dedicated refactor phase.

## Data-Driven Levels Phase 2 v2.9.0

Each of the ten current stages now lives in `app/src/main/assets/levels/level_{id}.json`. `LevelLoader` parses platforms, pickups, foes, hazards, checkpoints, story type, and boss metadata into the same runtime entities used before the refactor. This makes future level tuning and new content additions possible without embedding coordinate lists in `GameView`.

## Game Feel Phase 3 v3.0.0

The game now includes a single `JUICE_ENABLED` switch in `GameView` for testing presentation changes. When enabled, it adds capped particles, short screen shake, stronger hit-stop, player squash/stretch, and a smooth directional camera look-ahead. These are presentation effects only; level data, damage, and power costs are unchanged.

## Save and Statistics Phase 4 v3.1.0

`SaveManager` now uses an additive schema version for safe local upgrades. Existing progression keys remain unchanged while per-level best times and lightweight career statistics are added. The completion screen reports the run time and best-time status, while Settings shows a compact career summary.

## Developer Tools Phase 5 v3.2.0

The main menu exposes an internal **DEV TOOLS** panel for fast validation. It can launch any of the ten stages regardless of unlock state, clear local test saves, and toggle compact runtime statistics or player/camera coordinates while a level is running. These tools are kept outside the normal play path.

## Automated QA Baseline v3.3.0

This build adds a repeatable verification suite without changing gameplay rules. JUnit tests cover the pure combat range and direction rules plus duration formatting. The suite also validates all ten level JSON files, checks the additive local-save contract, confirms the developer-tools routes, and performs an approximate headless platform-path check from spawn to the goal zone. Run `tools/run_phase6_checks.sh` to execute the complete suite, debug build, and lint together. This is a QA baseline for the future publishing-readiness phase; it does not replace device testing, store screenshots, production signing, or a privacy-policy review.

## Professional Level Design Foundation Phase 1 v3.4.0

Each level JSON asset now carries a verified design contract: curriculum role, primary mechanic, player brief, safe-route intent, risk/reward-route intent, and rest-beat intent. The ten stages follow an explicit INTRODUCE → DEVELOP → COMBINE → MASTERY curriculum adapted to the current three-world structure. `LevelLoader` reads this contract and the brief stage HUD identifies the curriculum role at level start. `tools/test_level_data.py` validates both the contract and its expected progression, while `tools/generate_level_json.py` preserves it during regeneration. The Phase 2 layout pass will use these contracts to make the route geometry more distinct without discarding existing levels.

## Level Audit and Route Readability Phase 2 v3.5.0

Every pickup now carries a `SAFE` or `RISK` route classification in the level JSON. Safe-route pickups preserve the straightforward ground progression, while elevated and higher-commitment pickups use `RISK`; the renderer gives these risk/reward pickups a subtle world-colour halo. The level-data test now verifies both classifications exist in every current level, and the JSON generator preserves them. This improves route readability without changing pickup values, player movement, enemy damage, level count, or save data.

## World Gameplay Identity Phase 3 v3.6.0

Burning Dunes and Frozen Peaks now use authored environment contracts from the level JSON rather than relying on background art alone. Dunes stages introduce sand platforms, visible wind corridors, heat zones, and horizontal moving platforms. Frozen stages use ice-specific traction on upper platforms, opposing wind corridors, and readable falling-icicle spawners. Platform material, motion, wind, heat, and icicle definitions are validated by the level-data tests and loaded into the runtime without replacing any existing stage.

## Enemy System Expansion Phase 4 v3.7.0

`EnemyController` now owns reusable archetype definitions for eight enemy roles. The current roster preserves crawler, swooper, skirmisher, sentinel, and wisp behavior while adding Aegis Guard (front shield), Stone Brute (slow heavy strike), and Rune Caster (telegraphed projectile). JSON level data now validates the archetype range and confirms the three new roles are present in authored stages. Enemy hits and defeats receive clearer feedback through warning halos, hurt flashes, color-matched defeat particles, and existing audio cues.

## Combat Depth Phase 5 v3.8.0

The original attack button now supports tap/release combat without adding control clutter: taps chain a four-stage combo, while a short hold charges a stronger attack. Later combo stages and charged attacks receive controlled damage and reach bonuses through pure `CombatSystem` rules. A precise early dash contact triggers Perfect Dodge, briefly slows the simulation, restores energy, and grants a short counter-damage window. The input handler now routes attack touch-down/up events safely, including cancellation handling for interrupted gestures.

## Distinct Power Systems Phase 6 v3.9.0

`PowerSystem` gives the three powers separate tactical identities without changing the existing POWER button or energy economy. Ember applies a timed burn to nearby foes and bosses, Frost freezes foes and slows boss movement/cooldowns, and Gale creates a knockback burst that clears active enemy projectiles. The rules for cost floors, durations, burn cadence, and knockback direction are covered by pure unit tests; runtime effects retain clear color and particle feedback.

## Three-Phase Boss Design Phase 7 v4.0.0

Each world boss now uses a reusable three-phase `BossController` profile. Thornwold escalates from single roots to a root wall, Akaros expands a targeted slam into alternating quake lanes, and Vyrn evolves from paired ice lanes into a whiteout pattern with stronger gusts. Phase transitions have profile names, visual pulses, audio warnings, and a controlled speed increase. Ember burn, Frost slow, and Gale displacement interact with bosses without making their arenas trivial.

## Exploration and Secret Caches Phase 8 v4.1.0

Three authored secret Realm Caches now sit on optional high-risk routes in levels 3, 6, and 9. Each has a distinct purple rune presentation, grants two bonus gems once, and disappears on later runs after its unique save key is recorded. Save schema v5 adds only `secret_{id}` flags and a secrets-found statistic; all earlier progress keys remain additive. Level-data validation checks cache placement, unique IDs, and the presence of SAFE/RISK routes alongside the new SECRET route.

## Visual Style System Phase 9 v4.2.0

`VisualStyle` centralizes the visual language for Verdant, Dunes, and Frost worlds. It supplies per-world panel, control, route, secret, and material accents, while preserving the existing sprites and backgrounds. SAFE/RISK/SECRET collectibles now use consistent world-aware readability cues, and platform ICE/SAND highlights plus HUD surfaces share the same palette source. The project includes a pure palette test to guard against accidental visual collapse between worlds.

## Parallax and Atmospheric Depth Phase 10 v4.3.0

The world renderer now adds multiple camera-relative depth layers without replacing shipped background art. Verdant reuses its far and mid images with separate speeds, Burning Dunes gains translucent dunes and drifting dust, and Frozen Peaks gains layered mountain silhouettes with front snow. These are canvas-only visual layers; collision, camera bounds, platform placement, and controls remain unchanged.

## Progress-Focused UI Phase 11 v4.4.0

The World Map now communicates state at a glance: each gate is NEW, CLEARED, or LOCKED; cleared gates show their best time and star record. The map footer and Settings career view surface coins, gems, realm stars, secret caches, defeated bosses, and other permanent statistics. The completion overlay also carries the player’s secret-cache progress, while touch targets and menu flow remain unchanged.

## Game Feel Polish Phase 12 v4.5.0

Combat feedback now distinguishes high-impact moments while retaining the lightweight juice system: charged strikes and fourth-hit finishers receive stronger limited hit-stop, shake, particles, squash, and floating callouts; counter strikes show a cyan confirmation and consume their short bonus window on the first successful hit. Boss impacts use an appropriately larger version of the same feedback. These presentation changes do not alter level content, input locations, collision, or baseline combat rules.

## Animated Enemy Atlas v4.6.0

A new transparent 1536×2304 shared atlas replaces the generic static rendering for six archetypes: Dune Skirmisher, Frost Sentinel, Wind Wisp, Aegis Guard, Stone Brute, and Rune Caster. Its six rows each hold four runtime poses (idle, travel, telegraph/cast, hit/recover), selected from the existing enemy state without changing AI or combat rules. The Moss Mask Crawler and Ember Moth keep their prior dedicated animated sheets. `tools/test_enemy_sprite_atlas.py` validates all 24 atlas cells and renderer routing.

## Remaining-Element Sprite Atlases v4.7.0

Three additional transparent motion atlases now cover remaining core visuals. A 3×4 boss atlas drives the three world guardians, a 6×4 world-interactive atlas drives checkpoints and active thorn/heat/ice hazards, and a 6×4 collectible atlas drives coins, gems, and secret caches. Unused rows for brittle/ice platforms and heart/shield/energy effects are included for future state expansion, while original art stays intact. The verification suite validates all 60 new cells and their renderer hookups.

## Action Effects Replacement v4.8.0

Running, dash, sword attack, confirmed hit, and coin-collection feedback now use a 3×4 transparent action-effects atlas. Its run-trail row animates during movement, its sword row follows the existing attack timer, and its gold-sparkle row follows the existing collection timer. This changes presentation only; movement, combat damage, reward values, and controls are unchanged. The project verification suite validates all 12 effect frames and their renderer hooks.

## Layout and Verdant Backdrop Fix v4.8.1

Active thorn, heat, and icicle sprites now have their image bases anchored to the bottom edge of their existing damage rectangles, so they visually sit on the supporting platform without changing collision behavior. Checkpoint banners now rest on the authored platform surface (620 in the current level layout), do not rotate or sway, and retain their distinct active-state sprite and glow after saving. The running and dash trail texture orientation is corrected for both facing directions. Verdant Kingdom now renders the supplied waterfall artwork as its primary background; only subtle particles remain above it, so the former Verdant parallax art no longer obscures the replacement image. `tools/test_layout_background_fix.py` guards these placements and background routing in the unified verification suite.

## Cleaned Sprite Asset Integration v4.9.0

The three new boss rows were cleaned to genuine transparency and packed into the existing 3×4 runtime boss atlas: Thornwold uses the forest guardian row, Akaros uses the stone guardian row, and Vyrn uses the ice guardian row. The checkpoint now uses the supplied flag artwork, stays anchored to the existing platform surface, and alternates between two active-state frames without any banner rotation. The supplied ice and golden platform sequences are rendered by current `ICE` and `SAND` material platforms respectively; the safe standing surface remains aligned with the existing collision surface, while crumble platforms advance through their supplied break frames. The special coin, shield, and energy-bolt graphics replace their HUD resources. The cleaned hanging ice-spike sheet is included as a prepared resource but is not forced into the current ground-anchored icicle hazard because its art represents a platform-mounted hanging hazard, which needs a dedicated level/physics placement in a later update. The verification suite includes `tools/test_uploaded_asset_integration.py` to validate dimensions, alpha, and renderer hooks.

## Boss Motion Sprite Refresh v4.10.0

The three world guardians now use newly prepared eight-frame motion sheets rather than the former four-frame boss atlas. Thornwold, Akaros, and Vyrn each receive idle/breathing, anticipation, stomp preparation, charge, strike, recovery, and hurt states in separate source cells. The Android atlas is 4352×1632 pixels, organized as three rows by eight 544×544 cells; the renderer routes frames 0–3 for idle, 4–5 for transition, 5 for attack/cooldown, and 6–7 for hit recovery. The atlas is validated for 24 populated cells, transparent margins, and correct renderer routing. This version passed the project’s static and unit verification suite, including the Android debug build and lint. It has not been run on a physical device or emulator.

## Checkpoint, Trap, and Boss Facing Fix v4.10.1

The supplied six-by-four checkpoint shrine sheet now replaces the former checkpoint renderer. The shrine remains fixed to the authored platform surface while its source frames communicate inactive and active states; the prior checkpoint artwork is no longer drawn in the level. The supplied six-by-four trap/platform sheet now replaces the former interactive-atlas hazard rendering. Both static floor hazards and telegraphed boss traps retain their current damage rectangles and surface anchors, while selecting compact armed or rising frames from the new sheet. The three boss sprites now mirror dynamically toward Aster's horizontal position, so their visible facing direction follows the hero rather than their movement direction. Static resource, renderer-routing, unit, lint, and debug-build verification passed; no device or emulator runtime test was performed.

## Trap Motion and Checkpoint Grid Correction v4.10.2

The supplied trap sheet was corrected to its actual six-column by three-row layout, and is now repacked into 256×342 cells. Static floor traps visibly cycle through the tall armed sequence (2 → 3 → 4 → 5 → 4 → 3), while telegraphed boss traps progress from the clear platform to the raised state. The checkpoint sheet was likewise corrected to its actual eight-column by three-row layout with 192×342 cells. This prevents adjacent checkpoint cells from being drawn together, leaving one clean fixed shrine at the configured checkpoint position. Static resource, routing, unit, lint, and debug-build verification passed. No physical-device or emulator runtime test was performed.

## Restored Checkpoint Flag v4.10.3

The checkpoint shrine experiment has been removed from runtime rendering at the player's request. The previous compact checkpoint flag from the world-interactives atlas is restored, remains fixed to the authored platform surface, and keeps its active-state source-frame change and glow. The newly corrected animated trap sheet and its visible rising/armed loop remain unchanged. Static resource, renderer-routing, unit, lint, and debug-build verification passed. No physical-device or emulator runtime test was performed.

## Run Trail and Boss HUD Placement v4.10.4

The run-trail effect now renders in a narrow band directly below Aster's feet rather than over the lower body. The boss name and HP bar now use a dedicated HUD coordinate above the tallest boss sprite frame, preventing the label and health bar from obscuring the boss's head. The verification suite includes dedicated checks for both placements, alongside the existing source, unit, lint, and debug-build tests. No physical-device or emulator runtime test was performed.

## Precise Ground Contact v4.10.5

The run trail now uses the measured visible-pixel bounds of the Aster run frames and the trail frames. Its visible ink is anchored to the platform contact line directly beneath the feet. Boss sprite frames leave a lower transparent margin; the boss visual is offset down by 23 virtual pixels so the visible feet meet the arena floor. The elevated boss health bar remains independently positioned above the full sprite. Static source, renderer-routing, contact-placement, unit, lint, and debug-build verification passed. No physical-device or emulator runtime test was performed.

## Procedural Coin and Gem Drops v4.10.6

Collectible coins and gems now use **procedural vector rendering** on Android Canvas rather than the gameplay collectible sprite sheet. Coins use a smooth side-spin transform, radial gold gradient, rim, highlight, and a readable plus sign. Gems use a breathing pulse, a four-point crystal path, blue or secret-purple gradients, outlined facets, and a specular highlight. Their pickup coordinates, collision behavior, route rings, and reward logic are unchanged. Static source, renderer-routing, unit, lint, and debug-build verification passed. No physical-device or emulator runtime test was performed.

## Skeletal Boss Animation Pilot v4.11.0

Bosses now use a **2D skeletal-animation pilot** instead of swapping complete boss pose frames at runtime. Each forest, stone, and ice boss is assembled from six independent transparent painted parts: head, torso, left arm, right arm, left leg, and right leg. Canvas joint transforms animate the torso breathing, head response, alternating legs, and independent arm swings; charge and hit states add stronger arm and torso poses. This is cutout-based skeletal animation, not a full inverse-kinematics or mesh-deformation system. Static alpha, source, renderer-routing, unit, lint, and debug-build verification passed. No physical-device or emulator runtime test was performed.

## Build

The source project uses a standard Gradle Android application configuration. Ensure Android SDK Platform 35 and Build Tools 35.0.0 are installed, then run:

```bash
./gradlew assembleDebug
```

The generated APK is placed under `app/build/outputs/apk/debug/`.

## Notes

All characters, environments, artwork, names, and gameplay content are original. Progress is saved locally on the device through Android SharedPreferences.

## Claude Boss Rig Review and Integration v4.11.4

The supplied Claude package was reviewed and its stone and ice boss rig sheets were accepted after a full-size visual composition check. The project keeps the shared six-part anchor-based cutout rig for forest, stone, and ice bosses, including connected upper bodies, covered shoulder and hip attachment points, conservative neutral/charge/hurt rotations, and horizontal facing toward Aster. The final preview covered neutral, charge, and hurt states in both facing directions for all three bosses. No detached limbs, exposed hip gaps, white backgrounds, or visible guide rails were found in the reviewed composition. The stable procedural Canvas coin and gem rendering and the corrected trap/checkpoint behavior were retained rather than regressed to the older sprite-drop path. This remains cutout-based skeletal animation without inverse kinematics or mesh deformation. Static resource, source-routing, visual-composition, lint, and signed debug-build verification were performed; no physical-device or emulator runtime test was performed.
