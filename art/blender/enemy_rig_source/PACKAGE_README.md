# Enemy rigs — Legends of the Lost Realms

Three independently fitted FK rigs, with the original textures preserved.

## Source mapping
- Forest_Elemental: f45aad0e_2110_4303_851b_3730d3b84678_1a33f5b653d82db5d80d1bf6f26c516a.fbx
- Forest_Goblin: 754a6ee7_cb33_4ea1_95d1_1bb14d96b158_eaa12669c85d8ed4fd746822767cd728.fbx
- Ember_Demon: 3f87f1b4_36d4_443f_9a6c_c21c0c0fa11f_edb4af0f3193501f29053064e0f6a61d1.fbx

These are descriptive asset names. The supplied files were not overwritten.

## What each folder contains
- *_Rig.blend: editable Blender 5.2 source with the detailed mesh, packed textures, hidden reduced mesh, fitted skeleton, studio camera/lights, and a deformation-check action.
- *_Rig.fbx and *_Rig.glb: reduced skinned character in its rest pose with embedded textures, no test animation or studio objects.
- *_Pose_1.png, *_Preview.png, *_Pose_31.png: three rendered poses from the actual detailed rig.

## Controls
Select the *_Rig armature and enter Pose Mode. Rotate the bones to pose the model. These are FK rigs; there are no IK targets or facial controls.

Forest_Elemental: 34 bones, including body/limbs, grouped finger chains, separate thumbs, front/back foliage skirt controls, and two attachment bones. The antlers follow the head.

Forest_Goblin: 34 bones, including body/limbs, grouped finger chains, thumbs, skirt controls, and attachment bones. The integrated sickle follows hand.L rigidly. It is still part of the supplied mesh, not a separate equip/unequip prop. The large mask and headdress follow the head.

Ember_Demon: 42 bones, including body/limbs, claw chains, four independent two-bone wing chains, two two-bone antenna chains, a two-bone abdomen, and attachment bones.

## Working in Blender
Frames 1–46 contain Rig_Deformation_Check. Frame 1 is neutral. This action demonstrates posing; it is not a finished game animation. Clear it before making a new action.

The *_Detail object preserves the supplied geometry for offline sprite rendering. The *_GameMesh object is hidden initially and provides the lighter export version. Show one version at a time, as both occupy the same position.

Export meshes target approximately 58,000 triangles and use at most four normalized bone weights per vertex. Reduction preserves UVs and textures, but is automatic simplification rather than manual retopology.

Suggested working heights were set to 2.8 m for the elemental, 1.3 m for the goblin, and 1.8 m for the demon. These are editable authoring choices, not established game requirements.

## Game pipeline and limits
The existing Android game uses a 2D renderer. Animate these Blender rigs, render transparent sprite sequences, and then integrate those sequences into the game. Gameplay animation sets, sprite sheets, and APK integration are not included in this rigging delivery.

Detailed poses and reduced-mesh poses are visually reviewed; FBX and GLB are reopened in Blender to check bones, textures, skin weights, scale, and movement. These are Blender checks, not target-engine or device testing. Extreme poses can require further weight refinement, especially where the original single mesh joins foliage, clothing, wings, and limbs.
