# Expansion Stage 1 — implementation evidence

Baseline: `43acf98`. Target branch: `Game-expansion`.

Implemented the three chapter prototypes and local pacing foundation described in `EXPANSION_STAGES.md`. Main objectives replace the collection threshold only in chapters 2, 6 and 9. Other chapters, upgrade tracks, Aster clips, locomotion tuning and the six touch action buttons are preserved.

The optional elite clearings use textured, animated existing enemies and the regular enemy collision/fall behavior. Their islands are enlarged and stationary. Objective interactions require nearby supported, stationary Aster; guarded restoration waits for the designated enemies to be defeated. Crystal attunement checks both order and selected element. Optional rewards are awarded once per run and are banked with the chapter's ordinary rewards. Checkpoint retries preserve progress, while full restarts reset it.

Temple passage travel uses the existing Hero warp operation, lands on supported deck and is blocked by nearby living enemies. After arrival, Aster must step away before reactivating the passage. Pacing participation requires landing on the actual optional island, not merely standing near it on the main route.

## Verification

- Initial functional pass: `EXPANSION_STAGE1_TESTS_PASSED 88` in `Temp/opencode/expansion-focused.log`.
- Physical access pass: `EXPANSION_STAGE1_TESTS_PASSED 97` in `Temp/opencode/expansion-access.log`. Aster reaches and returns from all three optional clearings using normal double jumps, without changing movement constants.
- All six repository verification scripts passed after initial implementation.
- Final expanded checks also cover accurate detour participation and the passage arrival latch. Full 15-chapter regression passed **2,343 assertions** with no runtime exceptions or errors: `REALM_RUNTIME_TESTS_PASSED 2343` in `Temp/opencode/expansion-stage1-regression.log`.
- All six repository verification scripts passed again on the final implementation. Unity compiled runtime and Editor scripts cleanly; no Aster animation, baker or movement source was modified.

Visual evidence: `Validation/ExpansionStage1/chapter-*-optional.png` and `chapter-*-node-*.png`. Reviewed actual rendered islands, objective placement, restored state, textured enemy visuals and reused relic artwork. Review cameras differ from the gameplay camera, so world labels in these captures are angled toward the gameplay camera.

Pacing logging is local-only and retains the last 100 attempts. Automated validation is excluded from player save and pacing writes. The Editor export menu produces chapter summary and attempt CSVs for human playtests; no synthetic human duration measurements were produced.

## Remaining human review

Play chapters 2, 6 and 9, including the optional clearings. Assess interaction clarity, encounter enjoyment and pacing. Test on target Android hardware. Compare fresh-save runs with upgraded and Epic-equipped runs using the exported attempt rows. The proposed multi-hour campaign duration is a later-stage target, not a measured result of this prototype.
