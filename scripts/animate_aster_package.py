import bpy
import math
import os
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "art" / "blender" / "renders" / "aster_package_v1"
OUTPUT.mkdir(parents=True, exist_ok=True)
ANIMATED_BLEND = ROOT / "art" / "blender" / "aster_rig_source" / "Aster_Rig_Animated.blend"
ANIMATED_BLEND.parent.mkdir(parents=True, exist_ok=True)


rig = bpy.data.objects["Aster_Rig"]
high_mesh = bpy.data.objects.get("Aster_Body")
game_mesh = bpy.data.objects.get("Aster_GameMesh")


def material(name, color, metallic=0.0, roughness=0.45, emission=None):
    found = bpy.data.materials.get(name)
    if found:
        return found
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 1.8
    return mat


def attach_to_socket(obj, bone_name):
    bpy.context.view_layer.update()
    world = obj.matrix_world.copy()
    obj.parent = rig
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    obj.matrix_world = world
    return obj


def cube(name, location, scale, mat, bevel=0.008):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    modifier = obj.modifiers.new("Sword edge bevel", "BEVEL")
    modifier.width = bevel
    modifier.segments = 2
    return obj


def blade_mesh(name, location, width, depth, length, mat):
    half_w = width * .5
    half_d = depth * .5
    top = length * .5
    shoulder = -length * .38
    tip = -length * .56
    profile = [(-half_w, top), (half_w, top), (half_w, shoulder),
               (0, tip), (-half_w, shoulder)]
    vertices = [(x, -half_d, z) for x, z in profile]
    vertices += [(x, half_d, z) for x, z in profile]
    faces = [(0, 1, 2, 3, 4), (9, 8, 7, 6, 5)]
    for index in range(5):
        nxt = (index + 1) % 5
        faces.append((index, nxt, nxt + 5, index + 5))
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Sword blade bevel", "BEVEL")
    bevel.width = .006
    bevel.segments = 2
    return obj


def build_sword():
    for name in ("Aster Sword Blade", "Aster Sword Glow", "Aster Sword Guard",
                 "Aster Sword Grip", "Aster Sword Pommel"):
        old = bpy.data.objects.get(name)
        if old:
            bpy.data.objects.remove(old, do_unlink=True)

    steel = material("Aster Sword Steel", (.18, .34, .40), metallic=.85, roughness=.18)
    gold = material("Aster Sword Gold", (.52, .28, .055), metallic=.72, roughness=.24)
    leather = material("Aster Sword Leather", (.11, .038, .012), roughness=.68)
    glow = material("Aster Sword Aether", (.00, .32, .40), metallic=.15,
                    roughness=.16, emission=(.00, .62, .76))

    parts = [
        blade_mesh("Aster Sword Blade", (.50, -.08, .50), .095, .026, .69, steel),
        cube("Aster Sword Glow", (.50, -.097, .53), (.008, .006, .275), glow, .003),
        cube("Aster Sword Guard", (.50, -.08, .835), (.15, .035, .022), gold, .012),
        cube("Aster Sword Grip", (.50, -.08, .93), (.026, .026, .095), leather, .012),
        cube("Aster Sword Pommel", (.50, -.08, 1.035), (.045, .035, .028), gold, .014),
    ]
    for obj in parts:
        attach_to_socket(obj, "weapon_socket.L")


def reset_pose():
    for bone in rig.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0, 0, 0)
        bone.location = (0, 0, 0)
        bone.scale = (1, 1, 1)


def apply_pose(pose):
    reset_pose()
    for name, values in pose.items():
        bone = rig.pose.bones[name]
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


BASE = {}
IDLE_IN = {
    "pelvis": {"l": (0, 0, .006)}, "chest": {"r": (0, 0.6, 0), "s": (1.0, 1.0, 1.012)},
    "head": {"r": (0, -.8, -.5)}, "upper_arm.L": {"r": (-1.2, 0, 0)},
    "upper_arm.R": {"r": (1.2, 0, 0)}, "cloth_front": {"r": (-1.5, 0, 0)},
    "cloth_back": {"r": (1.2, 0, 0)},
}
IDLE_OUT = {
    "pelvis": {"l": (0, 0, -.004)}, "chest": {"r": (0, -.4, 0), "s": (1.0, 1.0, .994)},
    "head": {"r": (0, .7, .6)}, "upper_arm.L": {"r": (.8, 0, 0)},
    "upper_arm.R": {"r": (-.8, 0, 0)}, "cloth_front": {"r": (1.0, 0, 0)},
    "cloth_back": {"r": (-1.5, 0, 0)},
}
RUN_A = {
    "root": {"l": (0, 0, -.018)}, "pelvis": {"r": (0, 0, 3)},
    "spine": {"r": (0, 4, -2)}, "chest": {"r": (0, 5, -2)},
    "head": {"r": (0, -4, 1.5)},
    "upper_arm.L": {"r": (26, 0, -4)}, "forearm.L": {"r": (-24, 0, 0)},
    "upper_arm.R": {"r": (-28, 0, 4)}, "forearm.R": {"r": (-34, 0, 0)},
    "thigh.L": {"r": (-34, 0, 0)}, "shin.L": {"r": (50, 0, 0)},
    "foot.L": {"r": (-10, 0, 0)}, "thigh.R": {"r": (29, 0, 0)},
    "shin.R": {"r": (8, 0, 0)}, "foot.R": {"r": (8, 0, 0)},
    "cloth_front": {"r": (-17, 0, 0)}, "cloth_front_tip": {"r": (-10, 0, 0)},
    "cloth_back": {"r": (12, 0, 0)},
}
RUN_B = {
    "root": {"l": (0, 0, -.018)}, "pelvis": {"r": (0, 0, -3)},
    "spine": {"r": (0, 4, 2)}, "chest": {"r": (0, 5, 2)},
    "head": {"r": (0, -4, -1.5)},
    "upper_arm.L": {"r": (-28, 0, -4)}, "forearm.L": {"r": (-34, 0, 0)},
    "upper_arm.R": {"r": (26, 0, 4)}, "forearm.R": {"r": (-24, 0, 0)},
    "thigh.L": {"r": (29, 0, 0)}, "shin.L": {"r": (8, 0, 0)},
    "foot.L": {"r": (8, 0, 0)}, "thigh.R": {"r": (-34, 0, 0)},
    "shin.R": {"r": (50, 0, 0)}, "foot.R": {"r": (-10, 0, 0)},
    "cloth_front": {"r": (14, 0, 0)}, "cloth_back": {"r": (-16, 0, 0)},
    "cloth_back_tip": {"r": (-10, 0, 0)},
}
JUMP = {
    "root": {"l": (0, 0, .035)}, "pelvis": {"r": (-3, 0, 0)},
    "spine": {"r": (0, 5, 0)}, "chest": {"r": (0, 7, 0)},
    "head": {"r": (0, -5, 0)},
    "upper_arm.L": {"r": (-18, 0, -7)}, "forearm.L": {"r": (-38, 0, 0)},
    "upper_arm.R": {"r": (15, 0, 7)}, "forearm.R": {"r": (-32, 0, 0)},
    "thigh.L": {"r": (-24, 0, 0)}, "shin.L": {"r": (48, 0, 0)},
    "thigh.R": {"r": (-12, 0, 0)}, "shin.R": {"r": (38, 0, 0)},
    "cloth_front": {"r": (-25, 0, 0)}, "cloth_front_tip": {"r": (-15, 0, 0)},
    "cloth_back": {"r": (-18, 0, 0)},
}
ATTACK_READY = {
    "pelvis": {"r": (0, -5, -4)}, "spine": {"r": (0, -9, -5)},
    "chest": {"r": (0, -12, -7)}, "head": {"r": (0, 8, 5)},
    "upper_arm.L": {"r": (-38, -8, -24)}, "forearm.L": {"r": (-76, 0, 0)},
    "hand.L": {"r": (0, 0, -12)}, "upper_arm.R": {"r": (18, 0, 8)},
    "forearm.R": {"r": (-30, 0, 0)}, "thigh.L": {"r": (10, 0, 0)},
    "thigh.R": {"r": (-12, 0, 0)}, "cloth_front": {"r": (9, 0, 0)},
}
ATTACK_STRIKE = {
    "pelvis": {"r": (0, 8, 5)}, "spine": {"r": (0, 14, 8)},
    "chest": {"r": (0, 18, 11)}, "head": {"r": (0, -11, -7)},
    "upper_arm.L": {"r": (44, 4, 30)}, "forearm.L": {"r": (-18, 0, 0)},
    "hand.L": {"r": (0, 0, 20)}, "upper_arm.R": {"r": (-22, 0, -10)},
    "forearm.R": {"r": (-38, 0, 0)}, "thigh.L": {"r": (-18, 0, 0)},
    "shin.L": {"r": (24, 0, 0)}, "thigh.R": {"r": (15, 0, 0)},
    "cloth_front": {"r": (-22, 0, 0)}, "cloth_back": {"r": (-14, 0, 0)},
}


def create_actions():
    sequences = {
        "idle": [(1, BASE), (7, IDLE_IN), (13, BASE), (19, IDLE_OUT), (25, BASE)],
        "run": [(1, RUN_A), (5, BASE), (9, RUN_B), (13, BASE), (17, RUN_A)],
        "jump": [(1, {"root": {"l": (0, 0, -.02)}, "thigh.L": {"r": (12, 0, 0)},
                       "thigh.R": {"r": (12, 0, 0)}}), (4, JUMP),
                 (8, {**JUMP, "root": {"l": (0, 0, .07)}}),
                 (12, {**JUMP, "root": {"l": (0, 0, .02)}})],
        # The source model faces screen-left. Runtime mirrors it for normal
        # rightward movement, so this order becomes wind-up -> outward strike.
        "attack": [(1, BASE), (3, ATTACK_STRIKE), (5, ATTACK_READY),
                   (7, {**ATTACK_READY, "upper_arm.L": {"r": (-44, -8, -31)},
                        "forearm.L": {"r": (-58, 0, 0)},
                        "hand.L": {"r": (0, 0, -18)}}),
                   (9, ATTACK_STRIKE), (11, BASE)],
    }
    rig.animation_data_create()
    actions = {}
    for label, sequence in sequences.items():
        old = bpy.data.actions.get(f"AsterPhone_{label.title()}")
        if old:
            bpy.data.actions.remove(old)
        action = bpy.data.actions.new(f"AsterPhone_{label.title()}")
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
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.film_transparent = True
    scene.render.fps = 24
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.camera.data.type = "ORTHO"
    scene.camera.data.ortho_scale = 2.24
    if high_mesh:
        high_mesh.hide_render = True
        high_mesh.hide_viewport = True
    if game_mesh:
        game_mesh.hide_render = False
        game_mesh.hide_viewport = False


def render(actions):
    samples = {
        "idle": (1, 4, 7, 10, 13, 16, 19, 22),
        "run": (1, 3, 5, 7, 9, 11, 13, 15),
        "jump": (1, 2, 3, 4, 6, 8, 10, 12),
        "attack": (1, 2, 3, 4, 5, 7, 9, 11),
    }
    if os.environ.get("ASTER_PACKAGE_QUICK") == "1":
        samples = {"idle": (1,), "run": (1, 9), "attack": (3, 5)}
    scene = bpy.context.scene
    for label, frames in samples.items():
        rig.animation_data.action = actions[label]
        bpy.context.view_layer.update()
        destination = OUTPUT / label
        destination.mkdir(parents=True, exist_ok=True)
        for index, frame in enumerate(frames):
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            scene.render.filepath = str(destination / f"{label}_{index:02d}.png")
            bpy.ops.render.render(write_still=True)


build_sword()
actions = create_actions()
configure_scene()
render(actions)
rig.animation_data.action = actions["idle"]
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ANIMATED_BLEND))
print(f"Saved animated Aster rig: {ANIMATED_BLEND}")
print(f"Rendered phone frames: {OUTPUT}")
