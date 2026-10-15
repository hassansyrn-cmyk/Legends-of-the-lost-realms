# Astral asset sources

Run these scripts from the repository root. They create only Astral assets and never rebake Aster.

1. Run `build_astral_assets.py` with Blender 5.2 using `--background --python`. It authors the terrace, bridge, hollow arch and orrery meshes, and three textured generic rigs: Astral Sentinel, Star Oracle and Fracture Keeper. Each rig has nine original in-place actions. Skinned exports use unit transforms (`FBX_SCALE_ALL`) so posed grounding measurements use the same scale as the collision body.
2. Run `build_astral_audio.py` with Python 3. It produces two original scores and four effects as mono 22,050 Hz WAVs.
3. In Unity 6000.6.0f1, run **Lost Realms → Expansion → Prepare Astral Assets**. It imports readable meshes, creates the three prefabs and bakes only their new animation clips. Imported model rotations are retained; runtime placement uses an identity wrapper.

Generated assets are included, so rebuilding is optional. No external model downloads are required. The static bridge spans the gap between circular terraces rather than extending across their floors. Mesh colliders retain the real platform silhouette and hollow arch opening.

Run `LostRealms.QualityValidation.Run` with `-realmTest -stage5Focused` for the Astral runtime checks, or omit the focused flag for the full campaign regression. Runtime frames are saved in a unique `Validation/ExpansionStage5/Runs/<timestamp-id>/` directory. Actual 1280×720 HUD, Atlas, bestiary and ending captures are produced by `LostRealms.Stage5MenuProbe.Run` with `-menuVisualProbe`. Review rendered poses and frames after any asset rebuild; script compilation alone cannot verify material appearance or animated deck contact.
