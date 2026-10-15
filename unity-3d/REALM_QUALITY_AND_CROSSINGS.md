# Stormveil and Astral quality / traversal pass

Scope: chapters 16–25. This pass builds on the exploration overhaul without changing Aster's movement constants, baked animations, or the first four realms' route layouts.

## Platforms and gates

- Terrace models retain the original supplied platform topology and UV mapping. Decimation of the nearly flat deck collapsed many differently mapped triangles and caused the large smeared patches reported in playtesting. Do not run the old whole-mesh decimator on these assets.
- Platforms scale uniformly around their measured center deck height. Their supplied cliff keel replaces the second terrain mesh previously placed beneath the flattened terrace.
- Terrace color and normal textures import at up to 2048 pixels with anisotropic filtering.
- Stormveil uses the supplied **stone arch 3d model**, and Astral uses **ornate arch 3d model**. Each has its own `Gate_Storm` / `Gate_Astral` resource and texture. The snow gate fallback is removed.
- New gate colliders follow the supplied open arches; the original four gates retain their existing collision setup.

Source preparation: `Temp/opencode/rebuild_quality_assets.py`, with source/model measurements recorded in `Temp/opencode/quality-assets.json`. Source downloads remain untouched.

## Conductor courts

- The three courts use dogleg, triangular, and criss-cross layouts, with two to four relays.
- Each arrow points along the outgoing direction selected by that pad. Only relays reached by the incoming beam emit light.
- The final court has two successive receiver routes. The first receiver stays lit while the player redirects the network toward the second.
- Completion remains latched after falls/checkpoints. Keyboard, touch, and controller movement all use the same stand-on-pad interaction.
- Decorative corner gardens are omitted from puzzle islands to keep the entire control area clear.

## Moving crossings and wind

- Four out of every five crossing chains move. Three patterns combine broad sideways excursions, forward/back motion, vertical arcs, and figure-eight movement.
- Sideways excursion grows from ±3.6 to ±4.8 metres through each realm. Lift paths travel ±1.65 metres vertically. Routes face different world directions.
- Ferries within a chain share timing, preserving the gap between neighboring landings. Smooth starts/stops and short pauses at the authored docking position provide repeatable jumping windows.
- Rider detection rejects distant characters before raycasting. This prevents each platform from raycasting every enemy across the entire chapter.
- Wind has a green safe phase, an amber warning, and a red active phase with visible directional streaks. Sustained fixed-step force crosses the island instead of stopping at the grate's edge. Bosses remain immune to wind displacement.

## Verification and rollback

The `-realmQuality` exploration probe inspects all crossing motion envelopes, docking pauses, imported gates, and clear control pads. It also exercises green/red wind behavior with Aster's real controller. `-crossingProbe` traverses representative static, lift, orbit, and figure-eight chains using normal movement and jumping.

Rendered comparisons and logs are kept under `Validation/Exploration` and `Temp/opencode/quality-*.log`.

Verified on 14 October 2026: `quality-runtime-v4.log` reports **EXPLORATION_TESTS_PASSED 1577**, covering chapters 16–25, all conductor rounds, gate objectives, moving-platform riding and representative real-controller crossings in both realms. The red-wind check pushes Aster approximately 3.2–3.8 m and proves he leaves the deck; green wind leaves him stable. All six repository regression guard scripts pass. Rendered platform, gate, and puzzle images were visually inspected in `Validation/Exploration/20261014-114914`.

The additional rendered Stage 4 suite reports **EXPANSION_STAGE4_TESTS_PASSED 380** in `quality-stage4.log`: Aster physically stands on the pads to complete every court in chapters 16–20, including both rounds, and the guardian's four attack patterns remain grounded.

Final visual verification: `quality-final-preview.log` reports **EXPLORATION_TESTS_PASSED 266** after correcting the supplied arrow artwork's 22.5-degree offset. The rendered arrow in `Validation/Exploration/20261014-171330/arrow-direction.png` aligns with the white diagnostic beam-direction marker. That marker exists only during the test capture.

The pre-pass state is backed up at `Temp/opencode/realm-quality-baseline`. `Temp/opencode/restore_realm_quality.ps1 -CheckOnly` validates the rollback without changing anything. The actual restore refuses to overwrite files modified since the pass and requires Unity to be closed.
