# Realm exploration overhaul — 13 October 2026

Implemented locally on `Game-expansion`, preserving the pre-existing expansion and asset integration work.

## Delivered
- Chapters 16–25 use twelve named island districts with loops, branches, return routes and chapter-specific layout variants.
- Separated jumping platforms, shuttles and elevators replace the continuous forward runway. Real imported terrain supplies collision and visible cliff foundations.
- Three guarded keys and three puzzle districts awaken the exit. Optional caches and lore reward exploration. A field map records visited districts and marks the awakened gate.
- Mixed melee, ranged and creature encounters; imported trap models; full-size landmarks, corner gardens, distant islands, panoramic skies and cloud atmosphere.
- Astral's shifting courts have separate cliff foundations and diagonal approaches, clear of the main crossing routes.
- Aster's existing motion, animation assets and movement tuning are unchanged by this overhaul.

## Verified
| Rendered Unity suite | Result | Log |
| --- | --- | --- |
| Exploration, chapters 16–25 | 414 assertions passed; exit 0 | `Temp/opencode/exploration-runtime-v2.log` |
| Stormveil combat and progression | 370 assertions passed; exit 0 | `Temp/opencode/exploration-stage4-v2.log` |
| Astral combat and progression | 537 assertions passed; exit 0 | `Temp/opencode/exploration-stage5-v2.log` |
| Final ten-chapter layout and landmark/trap separation | 289 assertions passed; exit 0 | `Temp/opencode/exploration-layout-final.log` |

The exploration suite checks all chapter graphs and objectives, plus physical representative fixed/moving crossings, platform carrying and pause behavior. Combat is disabled during those navigation checks. The separate realm suites verify real proximity puzzle controls, guardian pursuit/attack patterns/phases, trap warnings and damage, rewards, save migration and enemy falls.

Issues found and corrected during validation: an obstructed imported ferry approach; a side court overlapping a main crossing; an arch blocking the diagonal bridge approach; landmarks sharing trap positions. An older footman model lacked its skeletal animator, so it was excluded from the new encounters in favor of the animated Briar Goblin. Existing spider and goblin bone-motion controllers remain supported by the checks.

All project regression guards passed. Standalone runtime compilation passed with existing obsolete-API/unused-field warnings. These checks do not constitute a full manual playthrough of every branch or a target-device performance benchmark.

Final HUD capture completed with exit 0 (`Temp/opencode/exploration-hud-settled.log`). Both gameplay images were visually inspected: objective counters, field-map panel/legend and action-button labels fit their frames. The capture waits for at least two seconds of actual gameplay so Aster's initial loading pose is not mistaken for the settled idle pose.

## Evidence and rollback
- Final world overview captures: `Validation/Exploration/20261012-191938/`.
- Final Astral court and guardian captures: `Validation/ExpansionStage5/Runs/20261012-190608-723046/`.
- Playable-camera HUD captures: `Validation/ExplorationHUD/`.
- Pre-overhaul snapshot: `Temp/opencode/realm-exploration-baseline/`.
- Targeted restore: `Temp/opencode/restore_realm_exploration.ps1`. Close Unity first. It refuses to overwrite files changed after the final manifest and saves the current files before restoring the baseline.

No commit or push was performed for this overhaul.
