# New Epic weapons and rigged enemies — 8 October 2026

The two weapons and three enemies come from the user's `Downloads/New 3d weapons and Enemies 2` folder. The supplied lava golem archive provides the rock monster's original colour and normal maps; its sampled UV coordinates match the rigged model.

## In-game content

- **Duskblade** (weapon 46): Epic sword, drawn slash grip, diagonal back sheath, charged gold piercing star lance.
- **Soulreaper** (weapon 47): Epic scythe, curved shaft grip, diagonal back sheath, charged violet/teal soul vortex.
- Both weapons enter the Epic chest's existing special-weapon pool. Standard and Golden chest pools retain their previous items and probabilities. Epic rewards use the existing purple reveal effect and duplicate refunds.
- **Iron Goblin** (enemy 22): Verdant chapters from chapter 2.
- **Ash Demon** (enemy 23): Burning Dunes and Emberfall from chapter 5.
- **Lava Golem** (enemy 24): Emberfall from chapter 11.
- **Lava Golem Guardian** (boss 9): enlarged desert guardian in chapter 7. The former guardian becomes **Sunscar Brute** (enemy 25), in chapters 5-7.
- Temporary weapon testing: Duskblade on chapter 1 second island and Soulreaper on chapter 2 second island. See [desert guardian notes](desert-lava-guardian.md).
- Bestiary expands to 26 entries and preserves existing discoveries and claimed achievements.

## Asset and animation pipeline

Original AccuRig FBX skeletons and bind poses stay in `Assets/Art/Enemies/NewEpic`. The `_Optimized.fbx` files supply lighter geometry only. `NewEpicIntegration.Prepare` remaps that geometry onto the original bone palette and creates the `_Mobile.asset` meshes and runtime prefabs. Do not replace these prefabs with the optimized FBX rigs directly.

Each new enemy has its own valid humanoid avatar and idle, walk, run, attack, hit and death clips retargeted from the existing humanoid animation library. The source FBX T-pose takes are excluded. `EnemyGroundPose` aligns the animated sole geometry to the existing enemy collision body's ground plane, including platform movement and falling; it does not move the collision body. Aster's movement tuning, source model, generated clips and animation baker are unchanged.

Weapons retain their colour textures and remain below the repository's 2 MB FBX limit. The new enemy geometry uses 18,000 triangles for the goblin, 32,000 for the repaired lava golem, and 14,292 for the demon. Original texture maps are explicitly bound to runtime materials. See [grip, charged attack and guardian repair validation](epic-weapon-boss-polish.md).

## Reproduce validation

With the interactive Unity Editor closed, run Unity 6000.6.0f1 with `-batchmode -force-d3d11 -executeMethod LostRealms.NewEpicIntegration.Validate -realmTest -newEpicFocused`, without `-quit`. The test exits itself and protects the player's saved progression. This prepares only the new models and clips; it does not invoke the Aster baker.

[Enemy pose review](NewEpic/enemies-review.jpg) · [Weapon pose review](NewEpic/weapons-review.jpg) · [Pose measurements](NewEpic/pose-motion.txt)

## Verification results

- Focused asset integration and polish: **153 assertions passed**, including all 18 animated states, sole alignment, shaft geometry, hand grips, chapter spawns, hit/death responses, ten grounded boss pose cycles, five attack patterns, actual arena approaches, charge collision, distinct charged attacks and save migration. See [the current guardian combat report](desert-guardian-patterns.md).
- Full gameplay suite: **2,233 runtime assertions passed**, with no runtime errors; all 15 chapters, all 26 enemy bodies, moving decks, edge falls, pause and first-guardian trap immunity included.
- Runtime and Editor compilation passed. All six repository verification scripts passed.
- Enemy and weapon pose montages were visually inspected. Aster's generated animation files and baker have no changes.

## Rollback

The original tracked files are backed up locally in `Temp/opencode/new-epic-20261008-backup`, against commit `c77751a`. The previous approved UI, character and physics work is preserved.

Run the local backup's restore.ps1 first without arguments to verify the file hashes. Use -Apply only when reverting is requested. It restores the scoped original files and removes the integration's added files; it stops if any file has changed since this checkpoint.
