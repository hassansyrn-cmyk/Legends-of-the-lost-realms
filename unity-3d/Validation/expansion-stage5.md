# Stage 5 — Astral Ruins and the campaign ending

Implemented October 9, 2026, locally on `Game-expansion`. Earlier local Stages 2–4 are preserved. No Stage 5 commit or push has been made.

## Playable scope

Chapters 21–25 add Starlit Threshold, The Broken Orrery, Moonshard Galleries, Sanctum of Echoes and Heart of the Fracture. Their authored main routes have 12, 14, 15, 16 and 10 circular terraces respectively, with two to four branching Sun/Moon mechanisms and two guarded side courts per mechanism. Physical proximity pads select the side bridge; standing beside a cleared court's focus reconnects it. Both sides of every mechanism are required for the gate. Occupied bridges cannot shift, including if a body enters during a pending warning.

Imported ivory, gold and amethyst temple decks, hollow arches and orreries provide a distinct celestial setting. Real mesh colliders preserve the circular deck outline, bridge lane and arch opening. Fracture floor pulses appear from chapter 22, warn for 1.5 seconds, then damage Aster and push ordinary enemies. Guardians remain immune to environmental trap damage. Pause freezes warnings and channels; checkpoint retries retain links while a full restart recreates them.

Astral Sentinels and Star Oracles have separate textured articulated rigs and nine in-place clips each. Sentinels pursue and attack with a lance; Oracles retreat, warn locked-target starfalls and periodically heal nearby living Sentinels. The Fracture Keeper has its own six-armed rig, grounded pursuit, warned charge lanes, starfall volleys and spreading sweeps. Ember, Frost and Gale wards close at successive phases; matching the existing elemental selector at the appropriate arena focus interrupts the guardian and exposes it for ten seconds. The existing boss HUD shows the current ward or exposure.

Each chapter's optional record pays 100 Gold and four Gems once per run, banking at normal chapter completion. The final record requires Keeper victory. Short discoveries connect Lyra's quest to the fracture. Completing chapter 25 offers an interactive epilogue with Realm Atlas and Sanctuary destinations.

The Atlas's New Realms page covers chapters 16–25. Saves expand to 25 score slots, preserving earlier progress and inventory; completed chapter-20 saves unlock chapter 21. Three bestiary entries and three achievement IDs are appended. The campaign has 75 possible stars. Stage 6 optional content and replay modes remain separate work.

## Verification and visual review

The full 25-chapter campaign regression passed **3,619 assertions with zero runtime errors** (`Temp/opencode/expansion-stage5-all25.log`, `REALM_RUNTIME_TESTS_PASSED 3619`). This includes all earlier stages, chapter physics, progression, rewards and the Astral ending. The final focused Astral run independently passed **458 assertions** with zero runtime errors (`Temp/opencode/expansion-stage5-final-458.log`). It includes closed-ward burn-relic protection, phase-transition damage-over-time cleanup and story-notice priority. All six repository verification scripts pass. No Aster model, prefab, generated animation clip or root-motion baker is modified. Locked movement tuning, the existing portrait, two-part analog and action hit regions are retained. No shader is edited. New asset rebuild sources are in `Tools/Astral/`.

Actual GPU rendering exposed coplanar bridge/deck surfaces and shallow inlays in the initial prototype. Bridges were shortened to span only the gap between terrace edges; the foundation was lowered below the ivory deck and the inlays raised clear of it. The large guardian arena is separated from its approach terrace, and the narrow arch was removed from the guardian's spawn area. Ordinary spawns sit clear of columns; later Moon courts pair the Oracle with a Sentinel so its healing has a normal encounter use. Focus labels mark Sun/Moon pads and the three arena elements. The ward's activation latch resets when entering a different focus, phase transitions close exposure, and burn relics cannot bypass a closed ward.

Ten placement/character/hazard renders and nine actual 1280×720 GUI captures were reviewed. Final focused placement evidence is in `Validation/ExpansionStage5/Runs/20261009-142616-10af14/`; GUI evidence is in `Validation/ExpansionStage5/Menus/`. GUI views cover the main menu, unlocked and locked Atlas cards, both bestiary pages containing Astral entries, normal HUD, guardian HUD, campaign completion and epilogue. Chapter labels, currencies, descriptions, ward text and ending buttons fit their frames. The independent placement camera checks geometry, support and clearance; it does not replace the normal gameplay camera. GUI captures use the settled follow camera. Automated assertions and visual captures establish behavior and appearance, not player enjoyment or a measured campaign duration.

Windows locked an older reference PNG during a full regression export. Runtime captures now write into a unique `Validation/ExpansionStageN/Runs/<timestamp-id>/` directory for each run, preserving earlier evidence and avoiding overwrites of open images. Unexpected runtime exceptions terminate validation rather than leaving a stalled coroutine running. Routine route hints also yield to an active story or interaction notice.

## Rollback and remaining review

Pre-Stage-5 files are backed up in `Temp/opencode/stage5-baseline/`. `Temp/opencode/restore_stage5.ps1` checks current-file and backup hashes before restoring Stage 5's 19 modified baseline files and removing its 125 added files. It refuses to overwrite later edits. Stages 2–4, player saves and rendered evidence are preserved. The `-ValidateOnly` safety check passes; restoration itself is not executed during validation.

Human pacing, combat difficulty/enjoyment and physical Android touch/performance review remain outstanding.
