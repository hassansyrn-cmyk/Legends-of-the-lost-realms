# Legends of the Lost Realms — Unity 3D

A third-person 3D rebuild of Aster's adventure for Unity 6000.6.0f1. The source game reference is `hassansyrn-cmyk/Legends-of-the-lost-realms`, branch `feature/boss-and-enemy-ai-refinement`, commit `395d3540511f97bc9b52d3a577d9830a1ab80ed8`.

## Open and play

1. In Unity Hub, choose Add project from disk and select this folder.
2. Open `Assets/Scenes/Main.unity` and press Play.
3. Choose Continue Journey. The Realm Map shows completed stages and unlocked chapters.

The runtime builds the 3D world from the route definitions in `Assets/Scripts/RealmWorld.cs`. An empty Main scene is intentional: `RealmGame` starts the game automatically.

Desktop controls: WASD or arrow keys move; Space jumps and double-jumps; Shift dodges; J attacks (hold for a charged strike); K casts; Q switches Ember/Frost/Gale; right mouse drag orbits the camera; Escape pauses. Android uses the left touch region as a movement stick and the labelled action regions on the right. Drag above the action controls to orbit.

## This edition

Ten named stages span Verdant Kingdom, Burning Dunes and Frozen Peaks, with distinct routes and elevation profiles, optional gem islands, moving platforms, hazards, checkpoints, local progression, upgrades, three elemental powers and three guardian fights. Bosses have three health phases, attack warnings and recovery windows. Enemies inflict damage through committed attacks rather than passive contact. The original game's audio is reused.

This is the first 3D edition. Its level geometry, camera and combat have been redesigned for 3D. The original 2D game's wall climbing, story cards, full statistics and secret-cache systems have not all been ported. The new save schema and Android application ID are separate; existing Android progress is not automatically imported.

## Your FBX assets

All 36 FBX files found under Downloads were imported and inspected in a separate audit project. `FBX_SOURCE_INVENTORY.json` identifies the original files and their hashes.

Seven source character models are integrated into nine gameplay prefabs:

- Aster: `character_rig_final.fbx`, with the matching `Start Walking` and `Dying` clips.
- Goblin, Elemental and Demon: the three fitted rigs from `Enemy_Rigs_Package`.
- Heartwood: the Forest Elemental rig, scaled and animated for the guardian encounter.
- Sunscar: the stone creature from `Mutant Idle.fbx`.
- Whiteout: the ice creature from `Mutant Walking.fbx`, with a humanoid Mixamo rig.

The compatible humanoid animation exports are shared and retargeted through Unity's animation system. Aster has its own walking and death assets, plus a breathing idle pose derived from the shared idle animation. The Frost enemy uses a smaller version of the ice creature. The golden mage from `converted_models/model_03.fbx` is the Rune Caster. It has no skeleton and uses procedural hovering; articulated limb animation requires a dedicated rig.

Dense selected models were reduced from approximately 480,000–500,000 triangles to 60,000. The three supplied enemy rigs retain their 58,000-triangle meshes. `MESH_OPTIMIZATION.json` records the exact reductions. Original files in Downloads remain intact. Base-colour and normal textures were recovered from the embedded FBX textures and companion GLB files and assigned to Unity materials. Unrigged alternatives remain candidates for future character or scenery work; they are not all shipped in the game.

## Build and validate

Use the Lost Realms menu in the Unity editor:

- Prepare project recreates Main and updates player/build settings.
- Build Windows writes `Builds/Windows/LostRealms3D.exe` and its required companion files.
- Build Android APK requires Android Build Support, SDK, NDK and OpenJDK installed for this Unity version. These modules are installed on the build computer. The project targets Android API 26 or newer, landscape, with package ID `com.manus.lostrealms3d`.

For repeatable runtime verification, launch Unity with `-batchmode -force-d3d11 -projectPath <folder> -realmTest -executeMethod RealmBuild.PlayTest -logFile <log>`. Do not add `-quit`; the test runner exits when finished. It uses a temporary in-memory save, tests movement and double jumping, checks all ten routes and guardian/model presence, and exercises enemy contact, defeat, pause and checkpoint respawn. Results and scene renders are written to `Validation/`.

An Android device performance and touch-control pass is still required before treating this as a mobile release. Automatic mesh reduction and humanoid retargeting also merit close art review, particularly on Aster's cape and the creatures' unusual anatomy.


