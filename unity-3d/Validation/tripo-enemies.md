# Tripo expansion enemy integration

Completed 2026-10-11 in the existing Game-expansion working tree. No commit or push was requested.

## Mapping and scope

The 49 files in Downloads/New 3d weapons and Enemies are ZIP packages with .fbx filenames. They contain 47 distinct models after excluding two byte-identical duplicate packages. Originals were retained. The real FBX and texture directories were extracted into Temp/opencode/tripo-extracted. Full mapping: Tripo-References/Incoming-2026-10-10/asset-map.json and ASSET_MAP.md. Three similar floor inserts are marked for motif verification before environment integration.

All six incoming character models had no armature or animation. They now use supplied textured meshes with custom weighted skeletons, optimized to approximately 26k triangles each and 1024-pixel basecolor/normal textures. The Storm Scribe staff and Astral Sentinel lance/buckler are separate, hand-bound meshes with independent textures. The old duplicate Storm Stalker JPG textures were removed after backup to avoid ambiguous Resources loading.

| Enemy | Incoming download | Test chapters |
|---|---|---|
| Storm Stalker, kind 26 | dragon warrior 3d model.fbx | 16–20 |
| Storm Scribe, kind 27 | fantasy knight 3d model.fbx | 16–20 |
| Tempest Regent, kind 28 | fantasy griffin 3d model.fbx | Guardian in 20 |
| Astral Sentinel, kind 29 | fantasy knight 3d model (1).fbx | 21–25 |
| Star Oracle, kind 30 | fantasy sorceress 3d model.fbx | 21–25 |
| Fracture Keeper, kind 31 | fantasy armored deity 3d model.fbx | Guardian in 25 |

## Animation and grounding

Nine distinct authored states per role: idle, walk, run, attack, attack_2, cast, gethit, victory and death; hit aliases support the existing generic hit path. Clips use densely sampled smooth skeletal motion and the existing CharacterVisual crossfade system. The griffin has four independent leg chains, articulated wings, neck/head and tail; the Keeper has six independent arm chains. Gaits, casting, attack preparation, reactions and death poses animate these native bones rather than stretching static meshes or applying humanoid animations to the griffin.

FBX exports use FBX_SCALE_ALL to avoid a hundredfold skinned-mesh transform scale that broke BakeMesh grounding. Model-object transform curves and unused bone translation/scale curves are removed during baking. Skeleton rotation clips animate in place; actor physics, gravity and knockback remain authoritative. The existing animated-sole grounding correction remains active. Aster generated clips, model and locked movement tuning were not changed. The only CharacterVisual addition evaluates the initial idle grip for the Scribe and Sentinel before attaching hand props.

The models use an approximate custom skinning pass, not a studio-authored facial/finger or cloth simulation rig. Gameplay playtesting can still guide refinements to individual attacks, robes and grip poses.

## Verification

- Unity import/bake succeeds; all role/state clips resolve, contain motion and have no missing bone bindings. Six-arm count is explicitly checked.
- 333 Stage 4 runtime checks pass across chapters 16–20, including posed-mesh bounds, sole grounding, boss pursuit/patterns, projectile encounters and conductor progression.
- 458 Stage 5 runtime checks pass across chapters 21–25, including route mechanisms, ward transitions, boss pursuit/patterns, hazards, saves and ending progression.
- Both focused runs exit 0 and require zero runtime errors.
- All six repository verification scripts pass, including Aster movement and weapon-system guards.
- 36 individual Unity pose renders and live chapter captures were reviewed. Stormveil live evidence: Validation/ExpansionStage4/Runs/20261010-200711-1e3d61. Astral live evidence: Validation/ExpansionStage5/Runs/20261010-201641-d8b5eb. Earlier failed intermediate renders and logs are not final validation evidence.
- No post-processing shader was changed; PostProbe was therefore not rerun.

## Remaining assets and handoff

The 38 non-enemy references have not been integrated in this task. Antigravity has no direct callable connection in this session. Its complete task prompt is Tripo-References/Incoming-2026-10-10/ANTIGRAVITY_HANDOFF.md. It includes the map, extracted sources, material/scale/placement requirements, interaction preservation, validation and strict ownership boundaries. Run that task after this enemy work is complete and Unity is closed.

## Rollback

Enemy-only baseline: Temp/opencode/tripo-enemy-baseline. Restore script: Temp/opencode/restore_tripo_enemies.py. Close Unity before restoring. Its --validate-only audit passed: 176 restores and 17 newly introduced file removals. The script validates all target paths remain within the project and does not remove scenery or Aster clips. It restores the pre-integration versions of the two enemy asset bakers, Enemy.cs and the pre-existing Hero.cs working copy, preserving earlier expansion work. Restart Unity afterward to reimport restored enemy assets. Do not use a repository-wide reset.
