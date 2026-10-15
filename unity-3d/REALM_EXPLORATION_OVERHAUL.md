# Stormveil and Astral exploration overhaul

## Player experience
Each of chapters 16–25 becomes an island network with loops and branches instead of a continuous forward causeway. Twelve named districts per chapter connect through separated stepping stones, ferries, and lifts. Layout variants expand different wings, change heights, rotate the approach, and relocate the guardian gate.

Three guarded keys and three puzzle districts are required to awaken the gate. Players can explore objectives in any order, return through circular routes, rest at checkpoints, and discover two optional supply caches and a lore record. The field map records visited districts and reveals the gate when the goals are complete. Death retains chapter-local key and puzzle progress; restarting resets the run.

Stormveil mixes Stalkers and Scribes with goblin brawlers, Briar Goblins, spiders, and a heavy brute. Astral mixes Sentinels and Oracles with Briar Goblins, knights, skeletons, and spiders. Encounters alternate with traversal and recovery islands. Imported turrets, crushers, elemental traps, wind windows, and fracture pulses add timing challenges.

The visual target is substantial floating rock under the imported masonry, readable full-sized landmarks, a populated horizon and cloud sea, and restrained completion effects. Existing Aster movement tuning, root-normalized animations, weapons, inventory, and first-four-realm layouts are protected.

## Validation required
- Every objective and exit connected to spawn through the authored network.
- Two-axis exploration, circular return paths, three keys, optional caches.
- Physical player jumps across representative stepping and moving crossings.
- Moving platforms carry Aster, pause correctly, and leave real void between islands.
- Key and cache collection by physical proximity; no repeat reward.
- Keys cannot bypass puzzles; finished objectives reveal and open the gate.
- Grounded textured enemies, mixed encounter roles, existing guardian mechanics.
- Inspect screenshots of chapter layouts, puzzle courts and the playable camera.
- Run the project regression guards and preserve a targeted rollback.

## Rollback baseline
`Temp/opencode/realm-exploration-baseline` contains the pre-overhaul Scripts and Editor folders. Restore only the files named in the final rollback manifest, not the entire folders over newer unrelated work.

`Temp/opencode/restore_realm_exploration.ps1` is the targeted restore entry point. Close Unity first. It checks current file hashes before any changes and refuses to overwrite later edits. It also saves a recovery copy of the overhaul before restoring the baseline. Imported models, previous expansion stages and unrelated work are retained.

## Test entry points
- `LostRealms.QualityValidation.Run` with `-realmTest -explorationFocused`: ten-chapter connectivity, representative real jumps, platform carrying/pause, physical key collection and court attunement, gate conditions and one-time caches. Combat is deliberately isolated in this navigation suite.
- Same entry point with `-stage4Focused` or `-stage5Focused`: combat, guardian phases, physical puzzle controls, save migration and rewards.
- `LostRealms.ExplorationVisualProbe.Run` with `-menuVisualProbe`: capture the playable camera and field map without writing the player's save.

Use rendered Unity runs (`-batchmode -force-d3d11`), without `-nographics`. Exploration captures are timestamped under `Validation/Exploration`; HUD captures live under `Validation/ExplorationHUD`.

## Testing scope
The navigation suite checks all ten chapter graphs and objectives. Physical movement tests cover representative fixed and moving crossings in both realms; they do not replace a full manual playthrough of every branch. The guardian suites separately exercise damage, pursuit, phases and telegraphed attacks.
