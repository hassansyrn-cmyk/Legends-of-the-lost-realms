import bpy
import math
from pathlib import Path
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
BLEND_DIR = ROOT / "work" / "src" / "Legends-of-the-lost-realms-main" / "art" / "blender"
RENDER_DIR = ROOT / "outputs" / "blender_aster_preview"
BLEND_DIR.mkdir(parents=True, exist_ok=True)
RENDER_DIR.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials):
        pass


def material(name, color, metallic=0.0, roughness=0.5):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def smooth(obj, bevel=0.08):
    if obj.type == "MESH":
        for poly in obj.data.polygons:
            poly.use_smooth = True
        if bevel:
            mod = obj.modifiers.new("Edge softness", "BEVEL")
            mod.width = bevel
            mod.segments = 2
    return obj


def parent_bone(obj, armature, bone):
    world_matrix = obj.matrix_world.copy()
    obj.parent = armature
    obj.parent_type = "BONE"
    obj.parent_bone = bone
    obj.matrix_world = world_matrix


def uv_part(name, location, scale, mat, armature, bone):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    smooth(obj, min(scale) * 0.12)
    parent_bone(obj, armature, bone)
    return obj


def cube_part(name, location, scale, mat, armature, bone, bevel=0.08, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    smooth(obj, bevel)
    parent_bone(obj, armature, bone)
    return obj


def cone_part(name, location, radius1, radius2, depth, mat, armature, bone, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=radius1, radius2=radius2, depth=depth,
                                    location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    smooth(obj, 0.05)
    parent_bone(obj, armature, bone)
    return obj


def add_bone(edit_bones, name, head, tail, parent=None):
    bone = edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    if parent:
        bone.parent = edit_bones[parent]
    return bone


def build_armature():
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm = bpy.context.object
    arm.name = "ASTER_RIG"
    arm.data.name = "ASTER_SKELETON"
    eb = arm.data.edit_bones
    eb.remove(eb[0])
    add_bone(eb, "Root", (0, 0, 0.2), (0, 0, 0.7))
    add_bone(eb, "Hips", (0, 0, 0.7), (0, 0, 1.15), "Root")
    add_bone(eb, "Spine", (0, 0, 1.05), (0, 0, 1.75), "Hips")
    add_bone(eb, "Chest", (0, 0, 1.65), (0, 0, 2.25), "Spine")
    add_bone(eb, "Neck", (0, 0, 2.2), (0, 0, 2.48), "Chest")
    add_bone(eb, "Head", (0, 0, 2.42), (0, 0, 3.02), "Neck")
    for side, sign in (("L", -1), ("R", 1)):
        add_bone(eb, f"UpperArm.{side}", (0.43 * sign, 0, 2.1), (0.82 * sign, 0, 1.72), "Chest")
        add_bone(eb, f"Forearm.{side}", (0.82 * sign, 0, 1.72), (0.92 * sign, 0, 1.2), f"UpperArm.{side}")
        add_bone(eb, f"Hand.{side}", (0.92 * sign, 0, 1.2), (0.96 * sign, 0, 0.94), f"Forearm.{side}")
        add_bone(eb, f"Thigh.{side}", (0.25 * sign, 0, 1.0), (0.30 * sign, 0, 0.35), "Hips")
        add_bone(eb, f"Shin.{side}", (0.30 * sign, 0, 0.35), (0.28 * sign, 0, -0.32), f"Thigh.{side}")
        add_bone(eb, f"Foot.{side}", (0.28 * sign, 0, -0.32), (0.28 * sign, -0.34, -0.46), f"Shin.{side}")
    bpy.ops.object.mode_set(mode="POSE")
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.show_in_front = True
    return arm


def build_character(arm):
    skin = material("Skin", (0.55, 0.27, 0.14), 0.0, 0.72)
    skin_light = material("Skin highlight", (0.82, 0.47, 0.27), 0.0, 0.62)
    hair = material("Chestnut hair", (0.09, 0.022, 0.012), 0.0, 0.82)
    cloth = material("Aster teal", (0.015, 0.16, 0.22), 0.15, 0.38)
    cloth_hi = material("Aster blue trim", (0.01, 0.42, 0.58), 0.25, 0.3)
    leather = material("Dark leather", (0.055, 0.025, 0.015), 0.05, 0.7)
    gold = material("Antique gold", (0.56, 0.30, 0.055), 0.72, 0.28)
    steel = material("Sword steel", (0.28, 0.55, 0.64), 0.85, 0.18)
    eye = material("Eye", (0.03, 0.5, 0.68), 0.1, 0.2)

    # Torso, waist and joint-covering armor deliberately overlap to prevent pose gaps.
    uv_part("Torso armor", (0, 0, 1.72), (0.48, 0.25, 0.66), cloth, arm, "Chest")
    cube_part("Chest plate", (0, -0.235, 1.83), (0.35, 0.055, 0.35), gold, arm, "Chest", 0.10)
    uv_part("Waist", (0, 0, 1.11), (0.34, 0.22, 0.28), leather, arm, "Hips")
    cube_part("Belt", (0, -0.235, 1.16), (0.38, 0.05, 0.09), gold, arm, "Hips", 0.05)
    uv_part("Belt buckle", (0, -0.31, 1.16), (0.11, 0.04, 0.11), cloth_hi, arm, "Hips")
    cube_part("Cape", (0, 0.20, 1.64), (0.42, 0.055, 0.78), cloth_hi, arm, "Chest", 0.10,
              rotation=(math.radians(-7), 0, 0))

    uv_part("Head", (0, -0.02, 2.67), (0.35, 0.29, 0.43), skin_light, arm, "Head")
    uv_part("Hair cap", (0, 0.03, 2.88), (0.38, 0.31, 0.29), hair, arm, "Head")
    for i, (x, z, rz) in enumerate(((-0.30, 2.91, -38), (-0.12, 3.10, -18), (0.12, 3.12, 12), (0.31, 2.98, 35))):
        cone_part(f"Hair spike {i}", (x, 0.02, z), 0.15, 0.018, 0.56, hair, arm, "Head",
                  rotation=(0, math.radians(rz), 0))
    for side, sign in (("L", -1), ("R", 1)):
        uv_part(f"Eye.{side}", (0.13 * sign, -0.294, 2.71), (0.055, 0.025, 0.065), eye, arm, "Head")

        uv_part(f"Pauldron.{side}", (0.49 * sign, 0, 2.05), (0.27, 0.29, 0.23), gold, arm, "Chest")
        uv_part(f"Upper arm.{side}", (0.68 * sign, 0, 1.83), (0.20, 0.20, 0.40), cloth, arm, f"UpperArm.{side}")
        uv_part(f"Elbow guard.{side}", (0.84 * sign, -0.02, 1.54), (0.19, 0.22, 0.18), gold, arm, f"Forearm.{side}")
        uv_part(f"Forearm.{side}", (0.88 * sign, 0, 1.34), (0.17, 0.18, 0.38), leather, arm, f"Forearm.{side}")
        uv_part(f"Hand.{side}", (0.94 * sign, -0.02, 1.04), (0.17, 0.16, 0.20), skin, arm, f"Hand.{side}")

        uv_part(f"Hip guard.{side}", (0.25 * sign, 0, 0.96), (0.26, 0.25, 0.25), gold, arm, "Hips")
        uv_part(f"Thigh.{side}", (0.28 * sign, 0, 0.62), (0.22, 0.23, 0.48), cloth, arm, f"Thigh.{side}")
        uv_part(f"Knee.{side}", (0.30 * sign, -0.03, 0.28), (0.21, 0.22, 0.19), gold, arm, f"Shin.{side}")
        uv_part(f"Shin.{side}", (0.29 * sign, 0, -0.02), (0.20, 0.22, 0.46), leather, arm, f"Shin.{side}")
        cube_part(f"Boot.{side}", (0.29 * sign, -0.17, -0.38), (0.22, 0.34, 0.16), leather, arm, f"Foot.{side}", 0.10)

    # Sword follows the right hand and remains independently poseable.
    cube_part("Sword grip", (1.0, -0.02, 0.91), (0.055, 0.055, 0.24), leather, arm, "Hand.R", 0.03,
              rotation=(0, 0, math.radians(-10)))
    cube_part("Sword guard", (1.0, -0.02, 0.70), (0.27, 0.07, 0.055), gold, arm, "Hand.R", 0.04,
              rotation=(0, 0, math.radians(-10)))
    blade = cube_part("Sword blade", (1.10, -0.02, 0.22), (0.085, 0.035, 0.53), steel, arm, "Hand.R", 0.045,
                      rotation=(0, 0, math.radians(-10)))
    blade.scale.x = 0.72


def reset_pose(arm):
    for pb in arm.pose.bones:
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)


def apply_pose(arm, pose):
    reset_pose(arm)
    for name, values in pose.items():
        pb = arm.pose.bones[name]
        if "r" in values:
            pb.rotation_euler = tuple(math.radians(v) for v in values["r"])
        if "l" in values:
            pb.location = values["l"]
        if "s" in values:
            pb.scale = values["s"]


POSES = {
    "idle": {
        "Chest": {"r": (0, 0, -2)}, "Head": {"r": (0, 0, 3)},
        "UpperArm.L": {"r": (0, 0, -7)}, "UpperArm.R": {"r": (0, 0, 8)},
        "Forearm.L": {"r": (0, 0, 8)}, "Forearm.R": {"r": (0, 0, -12)},
    },
    "run": {
        "Root": {"r": (0, 0, -8), "l": (0, 0, 0.08)}, "Chest": {"r": (0, 0, 9)},
        "UpperArm.L": {"r": (0, 0, -38)}, "Forearm.L": {"r": (0, 0, 28)},
        "UpperArm.R": {"r": (0, 0, 42)}, "Forearm.R": {"r": (0, 0, -35)},
        "Thigh.L": {"r": (0, 0, 32)}, "Shin.L": {"r": (0, 0, -22)},
        "Thigh.R": {"r": (0, 0, -34)}, "Shin.R": {"r": (0, 0, 38)},
    },
    "jump": {
        "Root": {"r": (0, 0, -5), "l": (0, 0, 0.32)}, "Chest": {"r": (0, 0, 6)},
        "UpperArm.L": {"r": (0, 0, -25)}, "UpperArm.R": {"r": (0, 0, 28)},
        "Thigh.L": {"r": (0, 0, 18)}, "Shin.L": {"r": (0, 0, -42)},
        "Thigh.R": {"r": (0, 0, -15)}, "Shin.R": {"r": (0, 0, 48)},
    },
    "attack": {
        "Root": {"r": (0, 0, -10)}, "Chest": {"r": (0, 0, 18)},
        "Head": {"r": (0, 0, -9)}, "UpperArm.R": {"r": (0, 0, -112)},
        "Forearm.R": {"r": (0, 0, -22)}, "Hand.R": {"r": (0, 0, -18)},
        "UpperArm.L": {"r": (0, 0, 25)}, "Thigh.L": {"r": (0, 0, 22)},
        "Thigh.R": {"r": (0, 0, -25)},
    },
    "hurt": {
        "Root": {"r": (0, 0, 12), "l": (0, 0, 0.04)}, "Chest": {"r": (0, 0, -24)},
        "Head": {"r": (0, 0, 20)}, "UpperArm.L": {"r": (0, 0, -55)},
        "UpperArm.R": {"r": (0, 0, 52)}, "Thigh.L": {"r": (0, 0, -18)},
        "Thigh.R": {"r": (0, 0, 16)},
    },
}


def make_actions(arm):
    arm.animation_data_create()
    frames = {"idle": (1, 25), "run": (1, 13), "jump": (1, 12), "attack": (1, 10), "hurt": (1, 8)}
    for name, (start, end) in frames.items():
        action = bpy.data.actions.new(f"Aster_{name.title()}")
        action.use_fake_user = True
        arm.animation_data.action = action
        apply_pose(arm, POSES[name])
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_euler", frame=start)
            pb.keyframe_insert("location", frame=start)
            pb.keyframe_insert("scale", frame=start)
        if name == "idle":
            arm.pose.bones["Chest"].scale = (1.025, 1.025, 1.035)
        elif name == "run":
            apply_pose(arm, {**POSES[name], "Thigh.L": {"r": (0, 0, -34)}, "Thigh.R": {"r": (0, 0, 32)},
                             "UpperArm.L": {"r": (0, 0, 42)}, "UpperArm.R": {"r": (0, 0, -38)}})
        elif name == "attack":
            arm.pose.bones["UpperArm.R"].rotation_euler.z = math.radians(58)
            arm.pose.bones["Forearm.R"].rotation_euler.z = math.radians(-18)
            arm.pose.bones["Chest"].rotation_euler.z = math.radians(-12)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_euler", frame=end)
            pb.keyframe_insert("location", frame=end)
            pb.keyframe_insert("scale", frame=end)
    arm.animation_data.action = None
    reset_pose(arm)


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = True
    scene.render.image_settings.color_depth = "8"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.world.color = (0.015, 0.02, 0.025)

    bpy.ops.object.camera_add(location=(4.7, -12.5, 4.0))
    cam = bpy.context.object
    cam.name = "Sprite Camera"
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 4.6
    look_at(cam, (0, 0, 1.3))
    scene.camera = cam

    bpy.ops.object.light_add(type="AREA", location=(-4, -6, 7))
    key = bpy.context.object
    key.name = "Key Light"
    key.data.energy = 950
    key.data.shape = "DISK"
    key.data.size = 5
    look_at(key, (0, 0, 1.2))
    bpy.ops.object.light_add(type="AREA", location=(4, -2, 4))
    fill = bpy.context.object
    fill.name = "Teal Rim"
    fill.data.energy = 700
    fill.data.color = (0.05, 0.55, 0.85)
    fill.data.size = 4
    look_at(fill, (0, 0, 1.4))
    bpy.ops.object.light_add(type="AREA", location=(0, 4, 5))
    rim = bpy.context.object
    rim.name = "Warm Rim"
    rim.data.energy = 850
    rim.data.color = (1.0, 0.35, 0.08)
    rim.data.size = 3
    look_at(rim, (0, 0, 1.5))


def render_previews(arm):
    scene = bpy.context.scene
    for name in ("idle", "run", "jump", "attack", "hurt"):
        apply_pose(arm, POSES[name])
        scene.render.filepath = str(RENDER_DIR / f"aster_{name}.png")
        bpy.ops.render.render(write_still=True)
    reset_pose(arm)


clear_scene()
armature = build_armature()
build_character(armature)
make_actions(armature)
setup_render()
render_previews(armature)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_DIR / "aster_production_rig_v1.blend"))
print(f"Saved rig to {BLEND_DIR / 'aster_production_rig_v1.blend'}")
print(f"Rendered previews to {RENDER_DIR}")
