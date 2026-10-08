# Epic weapon grip, distinct charges and lava guardian repair

- Soulreaper now uses the lower curved shaft centre `(-0.008, -0.20, -0.145)` in imported mesh coordinates, away from the blade head. The measured nearest shaft surface is 0.0074 metres away. Its blade axis is upward, correcting the previous inverted alignment; its attack draw immediately seats the grip in the palm and clears the trail on that transition. Duskblade's grip and model are unchanged.
- Duskblade's charged attack is a gold **star lance** which pierces enemies and hits each once, stopping at scenery.
- Soulreaper's charged attack creates a violet/teal **soul vortex**, with pulsed damage and a short physical pull on regular enemies. Terrain blocks placement and damage; pause stops its animation. These no longer use the repeated crescent attack.
- The source golem's split vertices were welded before reduction, with consistent normals and skin weights preserved. The reduced mesh changed from 21,548 boundary edges to 182; the resulting 32,000 triangles / 23,655 imported vertices preserve the original avatar, UVs and textures.
- The guardian now has bespoke grounded muscle clips and five attack patterns; see [the current combat validation](desert-guardian-patterns.md). The whole rock body stays above the actor's deck plane during poses and crossfades. The new enemy animators keep updating off-camera so grounding never uses a stale culled skeleton. Its collision body retains gravity and fixed-step movement.
- Aster's generated animation assets and animation baker are unchanged.

Earlier grip/pose follow-up validation: **120 assertions passed**, including shaft geometry and hand placement, obstacle detouring, piercing damage, vortex damage and pause, and all basic enemy animations. Current dedicated-controller and pose validation is recorded in the combat report linked above.

Previous full regression before this grip/pose follow-up: **2,233 assertions passed** across all 15 chapters, all enemy bodies, moving decks, edge falls, pause and first-guardian trap immunity. The current follow-up passed Runtime and Editor compilation and all six repository verification scripts.

Preview files: `NewEpic/Soulreaper-attack-1.png`, `NewEpic/DesertGuardian-attack_2.png`, `NewEpic/Duskblade-star-lance.png`, `NewEpic/Soulreaper-soul-vortex.png`.

The previous implementation is saved in `Temp/opencode/epic-polish-backup`; the grip/slam follow-up snapshot is in `Temp/opencode/grip-slam-followup-backup`. The complete pre-integration backup remains in `Temp/opencode/new-epic-20261008-backup`.
