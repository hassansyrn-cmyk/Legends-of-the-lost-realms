# Stormveil asset sources

Run these scripts from the repository root. They create only the new Stormveil assets; they do not bake or modify Aster.

1. With Blender 5.2, run `build_storm_assets.py` using `--background --python`. It authors the Storm Scribe, Tempest Regent, conductor and watchtower, rigs the two constructs, creates nine in-place actions per construct, and copies the supplied Gecko Ranger and its textures as Storm Stalker sources.
2. Run `build_storm_floor.py` with Blender after the first script. This replaces the initial terrace with the final joined weathered flagstone deck and floating rock keel.
3. Run `build_storm_audio.py` with Python 3 to produce the two original scores and five effects.
4. In Unity 6000.6.0f1, run **Lost Realms → Expansion → Prepare Stormveil Assets**. This imports readable meshes, builds the three prefabs and bakes only their new animation clips. The Stalker's humanoid motion uses the existing Iron Goblin clips.

The first script expects the owner's `Downloads/Gecko Ranger.fbx` and `Downloads/Gecko Ranger.fbm/` textures. The imported FBX, textures, prefabs and baked clips are included in the project, so this source download is needed only for a full rebuild. Preserve the FBX's imported root rotation and centimeter conversion when placing instances.

Validate actual rendered frames after rebuilding. Animation curves and collision assertions alone cannot establish that a skinned mesh remains visible in every pose. Runtime chapter, conductor, combat, save migration and grounding checks live in `Assets/Scripts/ExpansionStage4Checks.cs`; menu captures use `Assets/Editor/Stage4MenuProbe.cs`.
