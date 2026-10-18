# Aether Sovereign premium outfit

Baseline: `origin/astra-downsize`, commit `bde00b4` (the latest fetched release branch on October 18, 2026). Work is isolated on `legendary-skin`; the original dirty `astra-expanded` checkout was preserved.

## Integration

- Append-only skin ID 6, Aether Sovereign / The Eternal Crown. Existing outfit IDs and save keys are preserved.
- Legendary rarity, Epic Chest only: 0.5% per chest. Each of the five existing outfits is 2.9%; the overall outfit category remains 15%. Resource and weapon category probabilities are unchanged.
- Owned duplicates retain the existing 600 gold / 8 gem refund. Direct purchase cannot bypass the legendary chest unlock.
- Wardrobe has five outfits per page, a portrait, locked animated preview, and equip support. Vault collection count and probability disclosures include the new outfit; Legendary reveals use a gold border and label.
- Stats: +2 HP, +12% melee damage, +20% finisher damage, -8% incoming damage, +10% energy regeneration and +10 aether per kill. Base movement speed, air-dash count and parry timing are unchanged.

## Model preparation

The original supplied `Rigged fantasy armor 3d model.fbx` is preserved as the source FBX. Its embedded diffuse image was extracted and resized to 1024px. Blender re-export of this AccuRig rig distorted its bind poses, so the final asset is simplified inside Unity using the original imported bind poses and bone weights.

`Lost Realms > Skins > Build Aether Sovereign` builds the reduced mesh, standalone avatar, material, self-contained prefab and transparent 512px portrait. The existing Build All Aster Skins menu also builds this outfit. The mesh simplifier is editor-only, MIT licensed, and pinned in `Assets/Editor/ThirdParty/UnityMeshSimplifier/SOURCE.md`.

The 24,000-triangle runtime mesh replaces the 166,744-triangle source. The runtime prefab has no dependency on the high-detail source FBX. Existing Aster clips are generated only through `AsterPhase1.Prepare`; neither that baker nor Aster movement code was edited.

## Verification

Unity 6000.6.0f1, Direct3D 11:

- Valid humanoid avatar and right-hand weapon socket.
- Idle, walk, run, attack, roll, double jump and cast poses checked. Walk, run and attack additionally require measured hand/foot motion to reject static clips.
- Idle feet within 8cm of ground; all generated Aster RootT curves remain zero.
- Portrait contains 49,697 visible pixels and zero magenta pixels.
- Exhaustive 15,000 Epic Chest outcome combinations verify 0.5% legendary and 2.9% for each existing outfit, unchanged non-outfit categories, lower-tier exclusions, duplicate refunds, old-save migration and new-skin persistence.
- All six existing verification shell scripts pass.
- In-game UI probe passed all seven independently animated outfit previews, including the locked legendary outfit, the Legendary chest reveal, gameplay equipping with the expected health bonus, and preview cleanup on close. Ten 1280x720 screens were captured, with no text-overflow or shader-error reports.

The fresh editor session also reported an unrelated Unity Search startup indexing exception and the existing imported gate missing-script warnings. These did not prevent the outfit/UI checks from passing. This is focused outfit validation; an Android device playtest and a new release bundle were not produced in this task.

Detailed asset validation: `results.txt`. Wardrobe and reward captures are stored alongside this document after the in-game UI probe.

## Focused loot balance review

Sources: `SkinCatalog.cs`, `ChestSystem.cs`, `RealmGame.cs`, and the user's request for a rarer premium outfit. The new outfit is 5.8 times rarer than each existing outfit. At a 0.5% independent chance, expected acquisition is 200 Epic Chests (7,000 gems at the unchanged 35-gem price); this is an average, with no guarantee or new pity mechanic. The outfit trades away the movement, parry or specialized damage advantages of other outfits, so it does not strictly dominate them. No new resource faucet or purchase loop was introduced.
