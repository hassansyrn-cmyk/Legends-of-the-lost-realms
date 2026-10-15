# Expansion assets for Tripo — reference pack v1

This pack contains 47 individual AI-generated reference images for the new content in expansion Stages 1–5. Images were generated with the built-in imagegen tool. These are model-generation references, not integrated game replacements.

Stormveil uses weathered slate, copper, silver and blue stormglass. Astral uses ivory stone, antique gold and amethyst. Shared chapter and crypt pieces use materials suited to the existing four realms. The focus is modeled detail, readable silhouettes and believable worn materials.

## Generate and return the models

Use one individual image for each model. Keep the image's filename on the exported model, and keep its category folder where possible. A textured FBX with its texture folder or a GLB with embedded textures is suitable. Send the original textured export, not a screenshot of the result.

For humanoids, retain the illustrated T-pose and separate arms, hands and legs. Rigged versions are welcome; unrigged textured models are also useful. The Tempest Regent is a quadruped with wings spread in a neutral rigging pose. The Fracture Keeper has six arms and needs six separate arm chains. Its rig may require custom work; send the textured unrigged model if needed.

The Storm Scribe staff and Astral Sentinel lance/buckler have separate references. Keep them separate from the character models so they can attach to hands correctly. Do not bake magical particles, lightning beams, warning rings or glow halos into the geometry.

For platforms and bridges, keep walking surfaces level and entrances unobstructed. Arches must have genuine open holes. Low pressure pads and hazard inserts should stay low enough to walk over. The conductor base and directional arm are separate to allow rotation. The Sun/Moon pads share a scale and border design. The arena ward focus is reused with three runtime crystal colors.

Target sizes in the manifest are approximate integration references, not something you need to force in Tripo. I will normalize scale, pivots, textures, collision and animations when integrating your selected models.

## Coverage and priorities

Start with the six enemies/guardians, then both realms' terraces, bridges, towers/arches and puzzle mechanisms. Continue with chapter objectives and crypt pieces. The eight relic models are optional physical representations: the current relic system chiefly uses menus, so these can also provide future item/icon art.

Named elites and crypt defenders currently reuse existing enemy families. This pack replaces the six genuinely new family/guardian models; it does not request remaking every older enemy. Existing first-four-realm Tripo traps and props are retained. Wind and fracture attacks remain game-controlled effects; their new physical trap markers are included.

Moving sky, clouds, weather, attack particles, HUD graphics and labels are not solid Tripo models. Color/state variants of one mechanism can share its mesh. The same terrace/bridge asset is scaled and reused across multiple chapters.

## Files

- `asset-manifest.json` contains every full generation prompt, intended game role and approximate target size.
- `CATALOG.md` presents the individual images and their roles.
- `01-enemies/` contains six characters and three separate accessories.
- `02-stormveil/` contains eight environment, mechanism and hazard assets.
- `03-astral/` contains nine environment, mechanism and hazard assets.
- `04-chapter-puzzles/` contains nine shared chapter objective/reward assets.
- `05-crypt/` contains four dungeon modules.
- `06-relics/` contains eight individual relic references.

No game code or existing game asset is changed by preparing this pack. References were visually reviewed for complete framing, pose, material consistency and model-friendly construction. Platform edges were refined to keep bridge connections open.

