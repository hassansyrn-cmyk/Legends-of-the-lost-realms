# Stage 2 validation — October 9, 2026

Branch: `Game-expansion`. Reversible baseline: Stage 1 commit `4af4ce2ff7d2f3f8e3b323974ba80784553883e5`. Stage 2 is an uncommitted local change.

## Implemented scope

- 33 authored progression objectives across the existing 15 chapters: guarded restoration and keys, elemental sequences, sustained ritual channels, ambush outposts, and guardian approach seals.
- Six named optional elites in chapters 2, 3, 6, 9, 12 and 14, each with a different warned strike pattern. Existing textured models, animation clips, collision capsules and enemy movement supply presentation and physics.
- Nine optional cache/memory discoveries in the remaining chapters. Normal chapters use a stationary side clearing; guardian chapters use a supported approach-island slot.
- Authored encounter pairings at guarded objectives. Two reinforcement enemies per ambush outpost spawn on supported, clear deck positions. Reinforcements and optional elites do not increase the mandatory collection denominator.
- Explicit objectives unlock chapter gates; higher stars still use collection percentages. Existing chapter unlocks, earned stars, save schema, controls, Aster clips and locked movement tuning are preserved.

## Automated evidence

Unity 6000.6.0f1, Windows D3D11, real Play mode, `LostRealms.QualityValidation.Run`, with `-realmTest`; player saves and pacing history are not written by validation.

- Focused Stage 2: **EXPANSION_STAGE2_TESTS_PASSED 331**, log `Temp/opencode/expansion-stage2-focused-final.log`.
- Final full campaign, including the Stage 1 and Stage 2 suites: **REALM_RUNTIME_TESTS_PASSED 2700**, log `Temp/opencode/expansion-stage2-regression.log`.
- All six `.github/scripts/verify-*.sh` checks passed after the final gameplay edits. `git diff --check` passed.
- No changes to `Hero.cs`, `AsterPhase1.cs`, Aster animation clips or shaders.

The Stage 2 suite exercises all 15 chapters: supported and unobstructed objective sites, genuine proximity activation, wrong-power and out-of-order rejection, sustained ritual duration, pause, reinforcement waves, living-guardian gate rejection, chapter gate readiness, supported secrets, reward amount and one-time payment, delayed claim after corpse destruction, checkpoint preservation, and full restart reset. All six elite signatures are exercised in Play mode with pause, stun cancellation and death cancellation; execution remains grounded.

The full suite additionally checks normal movement/double jump, all chapter routes and spawns, weapon and character presentation, bosses, physical building collision, moving platforms, enemy edge falling and death accounting, trials, arsenal, checkpoint behavior, and the Stage 1 detour double jumps and return shortcut.

## Visual review

Reviewed all **36** images in `Validation/ExpansionStage2/`: one objective site and one optional site for every chapter, plus six elite windups. Sites are visibly supported and leave interaction space. Elite warning zones and the nova safe center are visible. Existing realm scenery and Cinzel labels are retained.

These images use an independent review camera; they are not a physical mobile-device playtest or a continuous manual campaign run.

## Fixes found during validation

- Optional elite rewards now use the authored encounter definition instead of a surviving corpse reference; late arrival still grants the promised elite reward.
- Chapter 11's cracked lava mesh rejected a nine-sample placement-style footprint despite Aster being grounded safely. Discovery participation now checks the actual deck beneath the grounded capsule, rather than treating every small crack as a failed interaction. Placement still requires the wider supported footprint.
- Reinforcement spawn slots are checked against real deck geometry, props, traps and other enemies.
- Named attacks cancel warnings during stun/freeze, falling, death and destruction; pauses stop their timers.

## Remaining human review

Enjoyment, clue clarity, chapter duration, fresh-save reward balance and physical Android performance remain unmeasured. The existing local pacing exporter is available for that review. This milestone does not claim the eventual 3.5–5 hour campaign target has been achieved. Existing imported teleport-gate missing-script warnings remain; the final run has no runtime errors or exceptions.

See `EXPANSION_STAGES.md` for the chapter map and suggested playtest route. Later stages are not implemented by this change.
