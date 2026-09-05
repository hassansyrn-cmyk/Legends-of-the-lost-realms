# Aster character rig — Legends of the Lost Realms

## Files
- Aster_Rig.blend: editable Blender 5.2 file with the original detailed character, packed textures, a hidden reduced mesh, studio camera, lighting, and a short deformation-check action.
- Aster_Rig.fbx: reduced skinned character with embedded textures, in its rest pose.
- Aster_Rig.glb: reduced skinned character with embedded textures, in its rest pose.
- Aster_Rig_Preview.png: rendered pose from the actual rig.

## Using the Blender rig
Select Aster_Rig, enter Pose Mode, and rotate bones to pose the character. This is an FK rig: limbs are posed by rotating their bones; there are no IK target controls.

The skeleton has 34 bones, including 31 deform bones. It provides a root, pelvis, spine, chest, neck, head, paired arm and leg chains, foot/toe controls, front/back cloth chains, and two weapon sockets. The fingers are grouped under fingers/fingertips controls with a separate thumb on each hand. There are no individually articulated fingers or facial controls.

Scrub frames 1–46 to inspect the Rig_Deformation_Check action. Frame 1 is the neutral rest pose. The action is a posing test, not a finished idle, run, or combat animation. Clear the action before creating new animation.

Aster_Body preserves the supplied 483,822 triangles for offline rendering. Aster_GameMesh is the hidden reduced version used by both exports: 58,058 triangles, at most four normalized bone influences per vertex. Show only one mesh at a time; they occupy the same position. The model is approximately 1.8 metres tall.

## Verification
Both exported files were reopened in Blender. Each retained 34 bones, textures, zero unweighted vertices, and working elbow-driven deformation. Three poses were visually checked on the detailed source. These checks are not target-engine or physical-device testing. The reduced mesh is an automatic simplification, not manually retopologized animation topology; extreme poses may need weight or mesh refinement.

## Using this in the existing game
The inspected Android project renders 2D character artwork. Use the Blender source to animate and render transparent sprite sequences, then integrate those sequences into the game. The FBX and GLB are also available for a future 3D pipeline. This delivery adds the rig only; gameplay animation sets, sprite sheets, and APK integration have not been performed.
