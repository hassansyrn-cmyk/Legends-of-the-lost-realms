# Stage 4 — Stormveil Expanse

Implemented October 9, 2026, locally on `Game-expansion`. No Stage 4 commit or push has been made. The earlier local Stage 2 and Stage 3 changes are preserved.

## Playable scope

Chapters 16–20 add Cloudbreak Approach, Thunderwatch Ruins, Galeweaver Causeway, Eye of the Tempest and Crown of Stormveil. The routes have 12, 14, 14, 14 and 9 terraces respectively, plus optional record courts. Conductors use physical proximity pads to redirect lightning; warning-colored bridge wind windows push bodies across exposed gaps. Clear nearby guards before operating a relay. Linked circuits latch for checkpoint retries; restarting a chapter resets the run.

Storm Stalkers are textured humanoid melee pursuers. Storm Scribes are rigged conductor keepers with retreating movement and locked-target lightning warnings. The Tempest Regent is an independently rigged gryphon construct with grounded pursuit, claw strikes, lightning volleys, charge lanes and a storm cross. Four arena conductors interrupt it and share a ten-second cooldown. Existing collision, knockback, gravity and abyss deaths remain in use. Environmental trap damage cannot kill the guardian.

One optional storm record per chapter grants 80 Gold and three Gems, banked at chapter completion. The final record requires Regent victory. Records tie Lyra's commission to the fracture beyond Emberfall and the future Astral stage. Astral chapters 21–25 are not implemented in this stage.

Old campaign saves expand to 20 slots without losing existing progress, currencies, equipment, upgrades, relics or achievement claims. Finished chapter-15 saves unlock chapter 16. The Atlas gains a Stormveil page, three bestiary entries and three appended achievements. Chapter 20 ends at the Atlas without unlocking chapter 21.

## Verification

The full campaign regression passed **3,130 runtime assertions** with zero runtime errors, covering the existing fifteen routes, Stages 1–3 and Stormveil progression/combat. Visual review then found a scale problem in the two new generic rigs: their 100× imported mesh transforms caused the animated grounding sample to be scaled twice. They were re-exported with FBX unit scaling, rebuilt with unit transforms and rebaked using **Lost Realms → Expansion → Prepare Stormveil Assets**. Their skinned renderers update offscreen to keep pose bounds current.

The final focused Stage 4 run adds geometry measurements for all three new families in idle, walk, attack, cast, hit and death poses. It checks actual posed mesh extents, proximity to the collision body and deck clearance, in addition to bone motion and body grounding. **EXPANSION_STAGE4_TESTS_PASSED 333**, with zero runtime errors, verifies the final assets and behavior. The full regression ran before the final Stormveil-only asset scale correction; the focused run verifies that correction and the expanded geometry checks. All six repository verification scripts pass.

Other coverage includes 15-slot save migration/idempotence, inventory preservation, required textures and readable meshes, all five chapter mappings, guarded relays, wrong orientations, real standing-on-pad interactions, latched completion, walking the first bridge with input, wind push and pause, enemy pursuit/kiting, four telegraphed guardian patterns, grounding interruption/cooldown, gate completion, saved rewards and enemy falls beyond the deck.

Aster's FBX, prefab, generated animation clips and root-motion baker are unchanged. Locked locomotion and touch button routing are retained. No shaders are edited. Asset authoring sources and rebuild steps are in `Tools/Stormveil/`.

## Visual review and rollback

Nine placement/character renders are in `Validation/ExpansionStage4/`; six actual 1280×720 GUI captures are in its `Menus/` folder. The independent placement camera is intended to inspect deck support, mesh positioning and encounter clearance; it does not replace the normal gameplay camera. GUI capture allows the actual follow camera to settle before freezing the frame. All six captures were reviewed for text fitting, chapter labels, locked cards, bestiary wrapping, preserved Aster portrait, two-part analog and HUD content. The final character/placement renders show the Scribe and Regent at the correct scale with solid materials and deck contact.

Pre-Stage-4 files are backed up in `Temp/opencode/stage4-baseline/`. The guarded `Temp/opencode/restore_stage4.ps1` restores only modified baseline files and removes only Stage 4 additions. It validates current and backup hashes before any mutation, refusing a rollback if later edits would be overwritten. It preserves Stages 2–3, player saves and rendered evidence. Validate-only mode passed for 14 baseline restores and 126 Stage 4 additions; restoration itself was not executed.

Human pacing, difficulty/enjoyment feedback and physical Android touch/performance review remain outstanding. Automated results do not establish the eventual campaign duration target.
