# Stage 3 validation — October 9, 2026

Branch: `Game-expansion`. Stage 2 and Stage 3 are local, uncommitted changes. Stage 3's pre-change Stage 2 files are preserved in `Temp/opencode/stage3-baseline/`.

## Implemented scope

Four weapon mastery paths, two choices per path and affordable respecs; eight guaranteed relic unlocks with two distinct equip slots; Lyra's ordered Wayfinder commission; and the five-room Oathkeeper's Crypt off chapter 3's optional clearing. The crypt has authored guard/caster and bruiser/archer fights, an ordered elemental cipher, a warned Oathkeeper attack, return shrines, per-room checkpoints and a one-time first-clear reward.

Build selection uses existing gold/blue artwork and fantasy fonts. Existing buttons select perks, equip/clear relic slots, change collection pages, accept the commission and claim its reward. Existing power selection operates the crypt cipher. No mobile action button is added.

Aster movement constants, fixed-step locomotion, animation clips, root-motion baker, weapon assets and grip/orientation data are unchanged. New combat hooks supply conditional damage, defense rewards, burn, stagger and spell-cost effects.

## Automated evidence

- Full campaign: **REALM_RUNTIME_TESTS_PASSED 2787**, no runtime errors. This covers the 15 chapter routes, character/weapon models, guardian combat, movement/double jumps, collisions, edge falls, traps, trials, gates, checkpoint respawns, the Stage 1 prototypes, Stage 2 objectives/elite signatures, and Stage 3 progression/dungeon behavior.
- Final focused Stage 3 run: **EXPANSION_STAGE3_TESTS_PASSED 107**, zero runtime errors. This additionally exercises blocked charged strikes, Rupture, and all crypt arches' physical support and interaction clearance after the final burn-on-block fix.
- All six `.github/scripts/verify-*.sh` scripts pass. No changes to the locked Aster assets or touch-routing file.
- Unity compiled runtime/editor scripts successfully. Existing imported missing-script warnings remain; the full regression logged no runtime exceptions or errors.

Stage 3 tests cover old-save migration, perk choices and respec affordability, distinct owned relic slots, conditional damage/spell costs, actual perfect-defense refunds/healing/cooldowns, actual charged-hit burn, kill energy, third-combo Flow, Meditation, Resolve, Conduit, ordered quest milestones, guarded dungeon entry, supported arrivals/altars, normal double-jump crossings, live-room locks, wrong/out-of-order cipher rejection, proximity interactions, pause, leave/resume, first-clear and quest rewards, duplicate reward prevention, save roundtrip and replay.

The final focused test uses a real shield defender to require a blocked frontal strike to deal no damage and apply no burn, then a real heavy charged hit to require Rupture recovery. Runtime arch checks require a MeshCollider, visible deck support, feet at ground level, and clear restoration space in every room.

## Visual evidence

Seven actual 1280×720 GUI captures in `Validation/ExpansionStage3/Menus/`: Sanctuary, mastery, locked mastery, both relic pages, active quest and claim-ready quest. Reviewed text wrapping, card spacing, button fitting, disabled states and equipped indicators; the new views match existing gold/blue menu artwork.

Five independent review-camera renders in `Validation/ExpansionStage3/room-0.png` through `room-4.png`. Reviewed animated/textured characters, stone arches, crystals, encounter spacing, cipher placement and crossing gaps. The overhead review camera is for placement inspection; billboard captions normally face the gameplay camera.

Automated validation uses test saves and never writes the player's saved progress or pacing history. Test-generated Stage 1 screenshots were restored so the earlier milestone's evidence stays intact.

## Review limits and rollback

Physical Android touch/legibility review, human difficulty feedback and measured playtime remain outstanding. Assertion counts and screenshots do not prove enjoyment, campaign duration, or target-device performance.

`Temp/opencode/restore_stage3.ps1` restores the backed-up Stage 2 files and removes only the newly added Stage 3 source/meta files. Its syntax is validated; it has not been executed. It preserves Stage 2, player saves and rendered evidence. It is intended for reverting this milestone before further edits to the affected files. No commit or push was made for Stage 3.