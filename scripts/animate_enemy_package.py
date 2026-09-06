import bpy
import math
import os
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RENDER_ROOT = ROOT / "art" / "blender" / "renders" / "enemy_package_v1"
SOURCE_ROOT = ROOT / "art" / "blender" / "enemy_rig_source"
RENDER_ROOT.mkdir(parents=True, exist_ok=True)
SOURCE_ROOT.mkdir(parents=True, exist_ok=True)


def rotations(**bones):
    return {
        name.replace("_L", ".L").replace("_R", ".R"): {"r": values}
        for name, values in bones.items()
    }


def merged(*poses):
    result = {}
    for pose in poses:
        for bone, channels in pose.items():
            result.setdefault(bone, {}).update(channels)
    return result


def mirrored_stride(left_forward=True, amount=34, knee=48, lean=5):
    lead = -amount if left_forward else amount
    trail = amount if left_forward else -amount
    return merged(
        {
            "root": {"l": (0, 0, -0.018)},
            "pelvis": {"r": (0, 0, lean if left_forward else -lean)},
        },
        rotations(
            spine=(0, 5, -2 if left_forward else 2),
            chest=(0, 6, -3 if left_forward else 3),
            head=(0, -4, 2 if left_forward else -2),
            thigh_L=(lead, 0, 0),
            shin_L=(knee if left_forward else 8, 0, 0),
            foot_L=(-11 if left_forward else 8, 0, 0),
            thigh_R=(trail, 0, 0),
            shin_R=(8 if left_forward else knee, 0, 0),
            foot_R=(8 if left_forward else -11, 0, 0),
        ),
    )


def remap_pose_names(pose):
    return {name.replace("_L", ".L").replace("_R", ".R"): value
            for name, value in pose.items()}


BASE = {}

GOBLIN_IDLE_A = merged(
    {"pelvis": {"l": (0, 0, 0.012)}},
    rotations(
        pelvis=(2, 0, -1.5), spine=(0, 2, -1), chest=(0, 3, -1.5),
        head=(0, -2, 1), upper_arm_L=(-2, 0, -2), forearm_L=(-8, 0, 0),
        upper_arm_R=(2, 0, 2), forearm_R=(-6, 0, 0),
        thigh_L=(-3, 0, 0), thigh_R=(3, 0, 0), cloth_front=(-4, 0, 0),
        cloth_back=(3, 0, 0),
    ),
)
GOBLIN_IDLE_B = merged(
    {"pelvis": {"l": (0, 0, -0.007)}},
    rotations(
        pelvis=(-1, 0, 1), spine=(0, -1.5, 1), chest=(0, -2, 1),
        head=(0, 2.5, -1.5), upper_arm_L=(4, 0, -1), forearm_L=(-3, 0, 0),
        upper_arm_R=(-4, 0, 1), forearm_R=(-2, 0, 0),
        cloth_front=(3, 0, 0), cloth_back=(-4, 0, 0),
    ),
)
GOBLIN_CONTACT_L = merged(
    remap_pose_names(mirrored_stride(True, 37, 52, 5)),
    rotations(
        upper_arm_L=(10, 0, -5), forearm_L=(-20, 0, 0),
        upper_arm_R=(-30, 0, 5), forearm_R=(-34, 0, 0),
        cloth_front=(-18, 0, 0), cloth_front_tip=(-9, 0, 0), cloth_back=(14, 0, 0),
    ),
)
GOBLIN_CONTACT_R = merged(
    remap_pose_names(mirrored_stride(False, 37, 52, 5)),
    rotations(
        upper_arm_L=(-8, 0, -5), forearm_L=(-26, 0, 0),
        upper_arm_R=(25, 0, 5), forearm_R=(-25, 0, 0),
        cloth_front=(15, 0, 0), cloth_back=(-18, 0, 0), cloth_back_tip=(-9, 0, 0),
    ),
)
GOBLIN_DOWN_L = merged(
    {"root": {"l": (0, 0, -0.045)}},
    rotations(
        pelvis=(3, 0, 2), spine=(0, 7, -2), chest=(0, 8, -3),
        thigh_L=(-23, 0, 0), shin_L=(40, 0, 0), thigh_R=(18, 0, 0), shin_R=(22, 0, 0),
        upper_arm_L=(15, 0, -4), upper_arm_R=(-18, 0, 4),
    ),
)
GOBLIN_DOWN_R = merged(
    {"root": {"l": (0, 0, -0.045)}},
    rotations(
        pelvis=(3, 0, -2), spine=(0, 7, 2), chest=(0, 8, 3),
        thigh_L=(18, 0, 0), shin_L=(22, 0, 0), thigh_R=(-23, 0, 0), shin_R=(40, 0, 0),
        upper_arm_L=(-18, 0, -4), upper_arm_R=(15, 0, 4),
    ),
)
GOBLIN_PASS_L = merged(
    {"root": {"l": (0, 0, 0.012)}},
    rotations(
        spine=(0, 6, 0), chest=(0, 7, 0), thigh_L=(5, 0, 0), shin_L=(12, 0, 0),
        thigh_R=(-10, 0, 0), shin_R=(58, 0, 0), foot_R=(-18, 0, 0),
        upper_arm_L=(-4, 0, -4), upper_arm_R=(7, 0, 4), cloth_front=(-10, 0, 0),
    ),
)
GOBLIN_PASS_R = merged(
    {"root": {"l": (0, 0, 0.012)}},
    rotations(
        spine=(0, 6, 0), chest=(0, 7, 0), thigh_R=(5, 0, 0), shin_R=(12, 0, 0),
        thigh_L=(-10, 0, 0), shin_L=(58, 0, 0), foot_L=(-18, 0, 0),
        upper_arm_L=(7, 0, -4), upper_arm_R=(-4, 0, 4), cloth_back=(-10, 0, 0),
    ),
)
GOBLIN_WINDUP = rotations(
    pelvis=(-5, -4, -6), spine=(0, -10, -5), chest=(0, -16, -9), head=(0, 10, 7),
    upper_arm_L=(-54, -8, -28), forearm_L=(-78, 0, 0), hand_L=(0, 0, -15),
    upper_arm_R=(18, 0, 11), forearm_R=(-35, 0, 0),
    thigh_L=(13, 0, 0), thigh_R=(-16, 0, 0), shin_R=(22, 0, 0),
    cloth_front=(12, 0, 0),
)
GOBLIN_STRIKE = merged(
    {"root": {"l": (0, 0, -0.025)}},
    rotations(
        pelvis=(5, 9, 7), spine=(0, 15, 9), chest=(0, 22, 14), head=(0, -13, -8),
        upper_arm_L=(55, 5, 32), forearm_L=(-10, 0, 0), hand_L=(0, 0, 24),
        upper_arm_R=(-27, 0, -13), forearm_R=(-42, 0, 0),
        thigh_L=(-19, 0, 0), shin_L=(28, 0, 0), thigh_R=(17, 0, 0),
        cloth_front=(-25, 0, 0), cloth_back=(-18, 0, 0),
    ),
)
GOBLIN_HURT = merged(
    {"root": {"l": (0, 0, -0.03)}},
    rotations(
        pelvis=(-8, -7, 9), spine=(-4, -15, 12), chest=(-7, -22, 17), head=(8, 18, -15),
        upper_arm_L=(33, 0, -18), forearm_L=(-42, 0, 0),
        upper_arm_R=(-38, 0, 20), forearm_R=(-48, 0, 0),
        thigh_L=(-15, 0, 0), shin_L=(30, 0, 0), thigh_R=(12, 0, 0),
    ),
)

ELEMENTAL_IDLE_A = merged(
    {"pelvis": {"l": (0, 0, 0.012)}, "chest": {"s": (1.0, 1.0, 1.018)}},
    rotations(
        pelvis=(1, 0, -1), spine=(0, 1.5, -1), chest=(0, 2.5, -1), head=(0, -1.5, 1),
        upper_arm_L=(-3, 0, -1), upper_arm_R=(3, 0, 1),
        cloth_front=(-3, 0, 0), cloth_back=(2, 0, 0),
    ),
)
ELEMENTAL_IDLE_B = merged(
    {"pelvis": {"l": (0, 0, -0.008)}, "chest": {"s": (1.0, 1.0, 0.99)}},
    rotations(
        pelvis=(-1, 0, 1), spine=(0, -1, 1), chest=(0, -2, 1), head=(0, 1.5, -1),
        upper_arm_L=(2, 0, -1), upper_arm_R=(-2, 0, 1),
        cloth_front=(2, 0, 0), cloth_back=(-3, 0, 0),
    ),
)
ELEMENTAL_CONTACT_L = merged(
    remap_pose_names(mirrored_stride(True, 24, 36, 3)),
    rotations(
        upper_arm_L=(18, 0, -4), forearm_L=(-17, 0, 0),
        upper_arm_R=(-20, 0, 4), forearm_R=(-18, 0, 0),
        cloth_front=(-12, 0, 0), cloth_back=(9, 0, 0),
    ),
)
ELEMENTAL_CONTACT_R = merged(
    remap_pose_names(mirrored_stride(False, 24, 36, 3)),
    rotations(
        upper_arm_L=(-20, 0, -4), forearm_L=(-18, 0, 0),
        upper_arm_R=(18, 0, 4), forearm_R=(-17, 0, 0),
        cloth_front=(9, 0, 0), cloth_back=(-12, 0, 0),
    ),
)
ELEMENTAL_DOWN_L = merged(
    {"root": {"l": (0, 0, -0.055)}},
    rotations(
        pelvis=(4, 0, 2), spine=(0, 4, -1), chest=(0, 5, -2),
        thigh_L=(-16, 0, 0), shin_L=(30, 0, 0), thigh_R=(13, 0, 0), shin_R=(16, 0, 0),
        upper_arm_L=(10, 0, -3), upper_arm_R=(-12, 0, 3),
    ),
)
ELEMENTAL_DOWN_R = merged(
    {"root": {"l": (0, 0, -0.055)}},
    rotations(
        pelvis=(4, 0, -2), spine=(0, 4, 1), chest=(0, 5, 2),
        thigh_L=(13, 0, 0), shin_L=(16, 0, 0), thigh_R=(-16, 0, 0), shin_R=(30, 0, 0),
        upper_arm_L=(-12, 0, -3), upper_arm_R=(10, 0, 3),
    ),
)
ELEMENTAL_PASS_L = merged(
    {"root": {"l": (0, 0, 0.006)}},
    rotations(
        spine=(0, 4, 0), chest=(0, 4, 0), thigh_L=(4, 0, 0), shin_L=(9, 0, 0),
        thigh_R=(-7, 0, 0), shin_R=(42, 0, 0), foot_R=(-12, 0, 0),
    ),
)
ELEMENTAL_PASS_R = merged(
    {"root": {"l": (0, 0, 0.006)}},
    rotations(
        spine=(0, 4, 0), chest=(0, 4, 0), thigh_R=(4, 0, 0), shin_R=(9, 0, 0),
        thigh_L=(-7, 0, 0), shin_L=(42, 0, 0), foot_L=(-12, 0, 0),
    ),
)
ELEMENTAL_WINDUP = merged(
    {"root": {"l": (0, 0, 0.03)}},
    rotations(
        pelvis=(-5, 0, 0), spine=(0, -8, 0), chest=(0, -11, 0), head=(0, 8, 0),
        upper_arm_L=(-78, -4, -12), forearm_L=(-52, 0, 0),
        upper_arm_R=(78, 4, 12), forearm_R=(-52, 0, 0),
        thigh_L=(9, 0, 0), thigh_R=(9, 0, 0), shin_L=(20, 0, 0), shin_R=(20, 0, 0),
        cloth_front=(14, 0, 0), cloth_back=(10, 0, 0),
    ),
)
ELEMENTAL_STRIKE = merged(
    {"root": {"l": (0, 0, -0.075)}},
    rotations(
        pelvis=(9, 0, 0), spine=(0, 18, 0), chest=(0, 25, 0), head=(0, -18, 0),
        upper_arm_L=(58, 4, 16), forearm_L=(-10, 0, 0),
        upper_arm_R=(-58, -4, -16), forearm_R=(-10, 0, 0),
        thigh_L=(-16, 0, 0), shin_L=(34, 0, 0), thigh_R=(-16, 0, 0), shin_R=(34, 0, 0),
        cloth_front=(-25, 0, 0), cloth_back=(-20, 0, 0),
    ),
)
ELEMENTAL_HURT = merged(
    {"root": {"l": (0, 0, -0.035)}},
    rotations(
        pelvis=(-6, -8, 8), spine=(-4, -11, 10), chest=(-7, -17, 14), head=(7, 14, -12),
        upper_arm_L=(28, 0, -18), forearm_L=(-28, 0, 0),
        upper_arm_R=(-32, 0, 18), forearm_R=(-28, 0, 0),
        thigh_L=(-11, 0, 0), shin_L=(24, 0, 0), thigh_R=(9, 0, 0),
    ),
)

DEMON_WINGS_UP = rotations(
    wing_upper_L=(31, 0, 0), wing_upper_tip_L=(18, 0, 0),
    wing_upper_R=(31, 0, 0), wing_upper_tip_R=(18, 0, 0),
    wing_lower_L=(23, 0, 0), wing_lower_tip_L=(14, 0, 0),
    wing_lower_R=(23, 0, 0), wing_lower_tip_R=(14, 0, 0),
)
DEMON_WINGS_DOWN = rotations(
    wing_upper_L=(-25, 0, 0), wing_upper_tip_L=(-16, 0, 0),
    wing_upper_R=(-25, 0, 0), wing_upper_tip_R=(-16, 0, 0),
    wing_lower_L=(-18, 0, 0), wing_lower_tip_L=(-11, 0, 0),
    wing_lower_R=(-18, 0, 0), wing_lower_tip_R=(-11, 0, 0),
)
DEMON_IDLE_UP = merged(
    {"root": {"l": (0, 0, 0.025)}}, remap_pose_names(DEMON_WINGS_UP),
    rotations(
        chest=(0, 2, 0), head=(0, -2, 0), forearm_L=(-8, 0, 0), forearm_R=(-8, 0, 0),
        thigh_L=(-7, 0, 0), shin_L=(18, 0, 0), thigh_R=(-7, 0, 0), shin_R=(18, 0, 0),
        antenna_L=(7, 0, 0), antenna_R=(7, 0, 0), abdomen=(8, 0, 0),
    ),
)
DEMON_IDLE_DOWN = merged(
    {"root": {"l": (0, 0, -0.02)}}, remap_pose_names(DEMON_WINGS_DOWN),
    rotations(
        chest=(0, -2, 0), head=(0, 2, 0), forearm_L=(6, 0, 0), forearm_R=(6, 0, 0),
        thigh_L=(-12, 0, 0), shin_L=(25, 0, 0), thigh_R=(-12, 0, 0), shin_R=(25, 0, 0),
        antenna_L=(-6, 0, 0), antenna_R=(-6, 0, 0), abdomen=(-7, 0, 0),
    ),
)
DEMON_FLY_A = merged(
    {"root": {"l": (0, 0, 0.035)}}, remap_pose_names(DEMON_WINGS_UP),
    rotations(
        pelvis=(-5, 0, -3), spine=(0, 8, -3), chest=(0, 12, -5), head=(0, -9, 4),
        upper_arm_L=(-20, 0, -10), forearm_L=(-35, 0, 0),
        upper_arm_R=(22, 0, 10), forearm_R=(-35, 0, 0),
        thigh_L=(-19, 0, 0), shin_L=(43, 0, 0), thigh_R=(-10, 0, 0), shin_R=(34, 0, 0),
        antenna_L=(10, 0, 0), antenna_R=(10, 0, 0), abdomen=(12, 0, 0),
    ),
)
DEMON_FLY_B = merged(
    {"root": {"l": (0, 0, -0.025)}}, remap_pose_names(DEMON_WINGS_DOWN),
    rotations(
        pelvis=(-3, 0, 3), spine=(0, 7, 3), chest=(0, 10, 5), head=(0, -8, -4),
        upper_arm_L=(22, 0, -10), forearm_L=(-30, 0, 0),
        upper_arm_R=(-20, 0, 10), forearm_R=(-30, 0, 0),
        thigh_L=(-10, 0, 0), shin_L=(34, 0, 0), thigh_R=(-19, 0, 0), shin_R=(43, 0, 0),
        antenna_L=(-8, 0, 0), antenna_R=(-8, 0, 0), abdomen=(-10, 0, 0),
    ),
)
DEMON_WINDUP = merged(
    {"root": {"l": (0, 0, 0.02)}}, remap_pose_names(DEMON_WINGS_UP),
    rotations(
        pelvis=(-7, -5, -5), spine=(0, -10, -6), chest=(0, -16, -9), head=(0, 12, 7),
        upper_arm_L=(-42, -8, -22), forearm_L=(-65, 0, 0),
        upper_arm_R=(39, 8, 22), forearm_R=(-62, 0, 0),
        thigh_L=(-20, 0, 0), shin_L=(44, 0, 0), thigh_R=(-13, 0, 0), shin_R=(38, 0, 0),
        abdomen=(16, 0, 0), antenna_L=(12, 0, 0), antenna_R=(12, 0, 0),
    ),
)
DEMON_STRIKE = merged(
    {"root": {"l": (0, 0, -0.045)}}, remap_pose_names(DEMON_WINGS_DOWN),
    rotations(
        pelvis=(7, 10, 8), spine=(0, 17, 10), chest=(0, 25, 15), head=(0, -16, -10),
        upper_arm_L=(58, 5, 32), forearm_L=(-8, 0, 0),
        upper_arm_R=(-54, -5, -30), forearm_R=(-10, 0, 0),
        thigh_L=(-30, 0, 0), shin_L=(52, 0, 0), thigh_R=(-18, 0, 0), shin_R=(42, 0, 0),
        abdomen=(-19, 0, 0), antenna_L=(-15, 0, 0), antenna_R=(-15, 0, 0),
    ),
)
DEMON_HURT = merged(
    {"root": {"l": (0, 0, -0.04)}}, remap_pose_names(DEMON_WINGS_UP),
    rotations(
        pelvis=(-10, -9, 10), spine=(-5, -14, 12), chest=(-8, -22, 18), head=(10, 20, -16),
        upper_arm_L=(40, 0, -24), forearm_L=(-45, 0, 0),
        upper_arm_R=(-42, 0, 24), forearm_R=(-45, 0, 0),
        thigh_L=(-26, 0, 0), shin_L=(48, 0, 0), thigh_R=(-16, 0, 0), shin_R=(40, 0, 0),
        abdomen=(21, 0, 0), antenna_L=(18, 0, 0), antenna_R=(18, 0, 0),
    ),
)


def sequences_for(character):
    if character == "Forest_Goblin":
        return {
            "idle": [(1, GOBLIN_IDLE_A), (7, BASE), (13, GOBLIN_IDLE_B), (19, BASE), (25, GOBLIN_IDLE_A)],
            "move": [(1, GOBLIN_CONTACT_L), (4, GOBLIN_DOWN_L), (7, GOBLIN_PASS_L),
                     (10, BASE), (13, GOBLIN_CONTACT_R), (16, GOBLIN_DOWN_R),
                     (19, GOBLIN_PASS_R), (22, BASE), (25, GOBLIN_CONTACT_L)],
            "attack": [(1, BASE), (3, GOBLIN_WINDUP), (6, GOBLIN_WINDUP),
                       (8, GOBLIN_STRIKE), (11, GOBLIN_STRIKE), (14, GOBLIN_IDLE_A), (17, BASE)],
            "hurt": [(1, BASE), (2, GOBLIN_HURT), (5, GOBLIN_HURT),
                     (8, GOBLIN_IDLE_B), (12, BASE)],
        }
    if character == "Forest_Elemental":
        return {
            "idle": [(1, ELEMENTAL_IDLE_A), (7, BASE), (13, ELEMENTAL_IDLE_B),
                     (19, BASE), (25, ELEMENTAL_IDLE_A)],
            "move": [(1, ELEMENTAL_CONTACT_L), (4, ELEMENTAL_DOWN_L), (7, ELEMENTAL_PASS_L),
                     (10, BASE), (13, ELEMENTAL_CONTACT_R), (16, ELEMENTAL_DOWN_R),
                     (19, ELEMENTAL_PASS_R), (22, BASE), (25, ELEMENTAL_CONTACT_L)],
            "attack": [(1, BASE), (4, ELEMENTAL_WINDUP), (8, ELEMENTAL_WINDUP),
                       (10, ELEMENTAL_STRIKE), (13, ELEMENTAL_STRIKE),
                       (16, ELEMENTAL_IDLE_B), (20, BASE)],
            "hurt": [(1, BASE), (2, ELEMENTAL_HURT), (6, ELEMENTAL_HURT),
                     (9, ELEMENTAL_IDLE_B), (13, BASE)],
        }
    return {
        "idle": [(1, DEMON_IDLE_UP), (7, BASE), (13, DEMON_IDLE_DOWN),
                 (19, BASE), (25, DEMON_IDLE_UP)],
        "move": [(1, DEMON_FLY_A), (7, BASE), (13, DEMON_FLY_B),
                 (19, BASE), (25, DEMON_FLY_A)],
        "attack": [(1, DEMON_IDLE_UP), (3, DEMON_WINDUP), (6, DEMON_WINDUP),
                   (8, DEMON_STRIKE), (11, DEMON_STRIKE), (14, DEMON_IDLE_DOWN), (17, DEMON_IDLE_UP)],
        "hurt": [(1, DEMON_IDLE_UP), (2, DEMON_HURT), (6, DEMON_HURT),
                 (9, DEMON_IDLE_DOWN), (13, DEMON_IDLE_UP)],
    }


rig = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE" and obj.name in {
    "Forest_Goblin_Rig", "Forest_Elemental_Rig", "Ember_Demon_Rig"
})
character = rig.name.removesuffix("_Rig")
slug = character.lower()
detail_mesh = bpy.data.objects.get(f"{character}_Detail")
game_mesh = bpy.data.objects.get(f"{character}_GameMesh")


def reset_pose():
    for bone in rig.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0, 0, 0)
        bone.location = (0, 0, 0)
        bone.scale = (1, 1, 1)


def apply_pose(pose):
    reset_pose()
    for name, values in pose.items():
        bone = rig.pose.bones.get(name)
        if bone is None:
            continue
        if "r" in values:
            bone.rotation_euler = tuple(math.radians(value) for value in values["r"])
        if "l" in values:
            bone.location = values["l"]
        if "s" in values:
            bone.scale = values["s"]


def key_pose(frame, pose):
    bpy.context.scene.frame_set(frame)
    apply_pose(pose)
    for bone in rig.pose.bones:
        bone.keyframe_insert("rotation_euler", frame=frame)
        bone.keyframe_insert("location", frame=frame)
        bone.keyframe_insert("scale", frame=frame)


def create_actions():
    rig.animation_data_create()
    actions = {}
    for label, sequence in sequences_for(character).items():
        action_name = f"{character}_{label.title()}"
        old = bpy.data.actions.get(action_name)
        if old:
            bpy.data.actions.remove(old)
        action = bpy.data.actions.new(action_name)
        action.use_fake_user = True
        rig.animation_data.action = action
        for frame, pose in sequence:
            key_pose(frame, pose)
        actions[label] = action
    rig.animation_data.action = None
    reset_pose()
    return actions


def configure_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 384
    scene.render.resolution_y = 384
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.film_transparent = True
    scene.render.fps = 24
    scene.view_settings.look = "AgX - Medium High Contrast"
    if detail_mesh:
        detail_mesh.hide_render = True
        detail_mesh.hide_viewport = True
    if game_mesh:
        game_mesh.hide_render = False
        game_mesh.hide_viewport = False


def render(actions):
    samples = {
        "idle": tuple(range(1, 24, 2)),
        "move": tuple(range(1, 24, 2)),
        "attack": (1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 14, 17),
        "hurt": (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
    }
    if os.environ.get("ENEMY_PACKAGE_QUICK") == "1":
        samples = {"idle": (1,), "move": (1, 7, 13), "attack": (3, 8), "hurt": (2,)}
    scene = bpy.context.scene
    for label, frames in samples.items():
        rig.animation_data.action = actions[label]
        destination = RENDER_ROOT / slug / label
        destination.mkdir(parents=True, exist_ok=True)
        for index, frame in enumerate(frames):
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            scene.render.filepath = str(destination / f"{label}_{index:02d}.png")
            bpy.ops.render.render(write_still=True)


actions = create_actions()
configure_scene()
render(actions)
rig.animation_data.action = actions["idle"]
bpy.context.scene.frame_set(1)
animated_blend = SOURCE_ROOT / f"{character}_Animated.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(animated_blend))
print(f"Saved animated enemy rig: {animated_blend}")
print(f"Rendered enemy frames: {RENDER_ROOT / slug}")
