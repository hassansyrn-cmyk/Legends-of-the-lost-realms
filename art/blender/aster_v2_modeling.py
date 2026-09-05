import bpy
import math
import os
from pathlib import Path
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
ART_DIR = ROOT / "art" / "blender"
CONCEPT = ART_DIR / "concepts" / "aster_v2_turnaround.png"
OUTPUT = ROOT / "art" / "blender" / "renders" / "aster_v2"
OUTPUT.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def mat(name, color, metallic=0.0, roughness=0.5, emission=None):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 3.0
    return material


def soften(obj, width=0.035):
    if obj.type != "MESH":
        return obj
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    if width:
        bevel = obj.modifiers.new("Production bevel", "BEVEL")
        bevel.width = width
        bevel.segments = 2
    return obj


def attach(obj, rig, bone):
    world = obj.matrix_world.copy()
    obj.parent = rig
    obj.parent_type = "BONE"
    obj.parent_bone = bone
    obj.matrix_world = world
    return obj


def sphere(name, xyz, scale, material, rig, bone, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20, location=xyz,
                                        rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    soften(obj, min(scale) * 0.08)
    return attach(obj, rig, bone)


def box(name, xyz, scale, material, rig, bone, rotation=(0, 0, 0), bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(location=xyz, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    soften(obj, bevel)
    return attach(obj, rig, bone)


def tapered_box(name, xyz, bottom_width, top_width, depth, height, material,
                rig, bone, rotation=(0, 0, 0), bevel=0.035):
    """A tailored, flat-backed volume for clothing and weapon silhouettes."""
    bw = bottom_width * 0.5
    tw = top_width * 0.5
    d = depth * 0.5
    h = height * 0.5
    vertices = [
        (-bw, -d, -h), (bw, -d, -h), (bw, d, -h), (-bw, d, -h),
        (-tw, -d, h), (tw, -d, h), (tw, d, h), (-tw, d, h),
    ]
    faces = [
        (0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1),
        (1, 5, 6, 2), (2, 6, 7, 3), (4, 0, 3, 7),
    ]
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = xyz
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    soften(obj, bevel)
    return attach(obj, rig, bone)


def sword_blade(name, xyz, width, depth, length, material, rig, bone,
                rotation=(0, 0, 0)):
    """A single watertight blade with an integrated point."""
    w = width * .5
    d = depth * .5
    top = length * .5
    shoulder = -length * .38
    tip = -length * .66
    profile = [(-w, top), (w, top), (w, shoulder), (0, tip), (-w, shoulder)]
    vertices = [(x, -d, z) for x, z in profile] + [(x, d, z) for x, z in profile]
    faces = [(0, 1, 2, 3, 4), (9, 8, 7, 6, 5)]
    for index in range(5):
        next_index = (index + 1) % 5
        faces.append((index, next_index, next_index + 5, index + 5))
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = xyz
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    soften(obj, .012)
    return attach(obj, rig, bone)


def pointed_panel(name, xyz, top_width, lower_width, depth, length, material,
                  rig, bone, rotation=(0, 0, 0)):
    """A split coat panel with a tapered, animated lower point."""
    tw = top_width * .5
    lw = lower_width * .5
    d = depth * .5
    h = length * .5
    profile = [(-tw, h), (tw, h), (lw, -h * .52), (0, -h), (-lw, -h * .52)]
    vertices = [(x, -d, z) for x, z in profile] + [(x, d, z) for x, z in profile]
    faces = [(0, 1, 2, 3, 4), (9, 8, 7, 6, 5)]
    for index in range(5):
        next_index = (index + 1) % 5
        faces.append((index, next_index, next_index + 5, index + 5))
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = xyz
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    soften(obj, .025)
    return attach(obj, rig, bone)


def cone(name, xyz, radius1, radius2, depth, material, rig, bone, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=radius1, radius2=radius2,
                                    depth=depth, location=xyz, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    soften(obj, 0.025)
    return attach(obj, rig, bone)


def add_bone(bones, name, head, tail, parent=None):
    bone = bones.new(name)
    bone.head = head
    bone.tail = tail
    if parent:
        bone.parent = bones[parent]
    return bone


def build_rig():
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    rig = bpy.context.object
    rig.name = "ASTER_V2_PRODUCTION_RIG"
    rig.data.name = "ASTER_V2_SKELETON"
    bones = rig.data.edit_bones
    bones.remove(bones[0])
    add_bone(bones, "Root", (0, 0, 0.05), (0, 0, 0.55))
    add_bone(bones, "Hips", (0, 0, 0.55), (0, 0, 1.15), "Root")
    add_bone(bones, "Spine", (0, 0, 1.05), (0, 0, 1.85), "Hips")
    add_bone(bones, "Chest", (0, 0, 1.75), (0, 0, 2.45), "Spine")
    add_bone(bones, "Neck", (0, 0, 2.40), (0, 0, 2.66), "Chest")
    add_bone(bones, "Head", (0, 0, 2.61), (0, 0, 3.25), "Neck")
    add_bone(bones, "CoatTail.L", (-0.20, 0.12, 1.10), (-0.32, 0.14, 0.25), "Hips")
    add_bone(bones, "CoatTail.R", (0.20, 0.12, 1.10), (0.32, 0.14, 0.25), "Hips")
    for side, sign in (("L", -1), ("R", 1)):
        add_bone(bones, f"UpperArm.{side}", (0.42 * sign, 0, 2.28),
                 (0.90 * sign, 0, 1.98), "Chest")
        add_bone(bones, f"Forearm.{side}", (0.90 * sign, 0, 1.98),
                 (1.20 * sign, 0, 1.54), f"UpperArm.{side}")
        add_bone(bones, f"Hand.{side}", (1.20 * sign, 0, 1.54),
                 (1.31 * sign, 0, 1.30), f"Forearm.{side}")
        add_bone(bones, f"Thigh.{side}", (0.23 * sign, 0, 1.02),
                 (0.28 * sign, 0, 0.30), "Hips")
        add_bone(bones, f"Shin.{side}", (0.28 * sign, 0, 0.30),
                 (0.28 * sign, 0, -0.40), f"Thigh.{side}")
        add_bone(bones, f"Foot.{side}", (0.28 * sign, 0, -0.40),
                 (0.28 * sign, -0.34, -0.50), f"Shin.{side}")
    bpy.ops.object.mode_set(mode="POSE")
    for pose_bone in rig.pose.bones:
        pose_bone.rotation_mode = "XYZ"
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.show_in_front = True
    return rig


def build_model(rig):
    skin = mat("Warm skin shadow", (0.39, 0.16, 0.08), roughness=0.72)
    skin_light = mat("Warm skin", (0.72, 0.40, 0.24), roughness=0.66)
    hair = mat("Dark teal hair", (0.012, 0.065, 0.075), roughness=0.76)
    teal = mat("Forest teal cloth", (0.012, 0.16, 0.15), roughness=0.58)
    teal_dark = mat("Deep teal cloth", (0.008, 0.07, 0.075), roughness=0.68)
    leather = mat("Brown leather", (0.105, 0.041, 0.018), roughness=0.67)
    leather_hi = mat("Warm leather", (0.25, 0.105, 0.035), roughness=0.58)
    gold = mat("Antique gold", (0.46, 0.24, 0.045), metallic=0.70, roughness=0.27)
    steel = mat("Sword steel", (0.43, 0.62, 0.67), metallic=0.88, roughness=0.16)
    blade_glow = mat("Sword aether edge", (0.00, 0.34, 0.42), metallic=0.32,
                     roughness=0.16, emission=(0.00, .62, .78))
    cyan = mat("Aster core", (0.00, 0.42, 0.52), metallic=0.15,
               roughness=0.18, emission=(0.02, 0.70, 0.86))
    eye = mat("Eyes", (0.02, 0.20, 0.23), roughness=0.20)

    # Overlapping volumes form a continuous silhouette at every playable pose.
    # The underbody prevents gaps; the tailored shells provide the visible design.
    sphere("Torso underbody", (0, 0, 1.84), (0.31, 0.19, 0.58), teal_dark, rig, "Chest")
    tapered_box("Tailored coat", (0, -0.17, 1.84), .50, .68, .18, 1.10,
                teal, rig, "Chest", bevel=.055)
    tapered_box("Dark waistcoat", (0, -.275, 1.84), .37, .48, .045, .83,
                teal_dark, rig, "Chest", bevel=.025)
    box("Left lapel", (-.13, -.315, 2.10), (.075, .025, .31), leather_hi,
        rig, "Chest", rotation=(0, 0, math.radians(-22)), bevel=.025)
    box("Right lapel", (.13, -.315, 2.10), (.075, .025, .31), leather_hi,
        rig, "Chest", rotation=(0, 0, math.radians(22)), bevel=.025)
    box("Chest strap", (0, -.326, 1.83), (.34, .022, .045), leather,
        rig, "Chest", rotation=(0, 0, math.radians(-12)), bevel=.018)
    sphere("Waist bridge", (0, 0, 1.14), (0.27, 0.18, 0.23), teal_dark, rig, "Hips")
    box("Belt", (0, -0.225, 1.20), (0.34, 0.05, 0.065), leather_hi, rig, "Hips", bevel=0.025)
    box("Buckle", (0, -0.292, 1.20), (0.085, 0.028, 0.085), gold, rig, "Hips", rotation=(0, math.radians(45), 0), bevel=0.018)
    box("Magic core bezel", (0, -0.352, 2.05), (.115, .022, .115), gold,
        rig, "Chest", rotation=(0, math.radians(45), 0), bevel=.018)
    box("Magic core", (0, -0.382, 2.05), (.075, .018, .075), cyan,
        rig, "Chest", rotation=(0, math.radians(45), 0), bevel=.015)
    cone("High collar", (0, -.015, 2.43), .30, .24, .23, teal_dark,
         rig, "Chest")
    for side, sign in (("L", -1), ("R", 1)):
        pointed_panel(f"Coat tail.{side}", (0.12 * sign, .10 + .025 * sign, .84),
                      .20, .13, .08, .70, teal, rig, f"CoatTail.{side}",
                      rotation=(math.radians(-4), 0, math.radians(-4 * sign)))

    sphere("Neck", (0, 0, 2.49), (.13, .13, .22), skin, rig, "Neck")
    sphere("Head", (0, -0.03, 2.89), (0.29, 0.25, 0.38), skin_light, rig, "Head")
    sphere("Jaw", (0, -0.08, 2.72), (0.23, 0.22, 0.22), skin_light, rig, "Head")
    sphere("Hair crown", (-.015, 0.02, 3.11), (0.32, 0.27, 0.23), hair, rig, "Head")
    cone("Nose", (0, -0.315, 2.88), 0.045, 0.008, 0.16, skin, rig, "Head",
         rotation=(math.radians(90), 0, 0))
    for index, (x, z, angle) in enumerate(((-.28, 3.12, -38), (-.15, 3.25, -25),
                                           (.00, 3.29, -5), (.16, 3.25, 22), (.29, 3.14, 38))):
        cone(f"Hair lock {index}", (x, 0.01, z), .085, .012, .35, hair, rig, "Head",
             rotation=(0, math.radians(angle), 0))
    for index, (x, z, angle) in enumerate(((-.19, 3.11, -45), (-.06, 3.14, -22),
                                           (.09, 3.14, 18), (.22, 3.09, 42))):
        cone(f"Swept fringe {index}", (x, -.245, z), .06, .008, .22, hair,
             rig, "Head", rotation=(0, math.radians(angle), 0))
    box("Mouth", (0, -.316, 2.73), (.055, .009, .009), skin, rig, "Head", bevel=.006)
    for side, sign in (("L", -1), ("R", 1)):
        sphere(f"Eye.{side}", (0.115 * sign, -0.286, 2.93), (.042, .018, .052), eye, rig, "Head")
        box(f"Brow.{side}", (0.115 * sign, -0.305, 3.01), (.075, .012, .014), hair, rig, "Head",
            rotation=(0, 0, math.radians(-5 * sign)), bevel=.008)

        sphere(f"Ear.{side}", (.27 * sign, -.03, 2.91), (.045, .035, .075), skin, rig, "Head")
        sphere(f"Shoulder cloth.{side}", (.44 * sign, 0, 2.24), (.19, .21, .18), teal, rig, "Chest")
        sphere(f"Pauldron.{side}", (.46 * sign, -.05, 2.28), (.20, .20, .10), leather_hi, rig, "Chest")
        box(f"Pauldron trim.{side}", (.48 * sign, -.23, 2.27), (.15, .025, .025), gold,
            rig, "Chest", bevel=.012)
        sphere(f"Upper arm.{side}", (.68 * sign, 0, 2.13), (.13, .14, .31), teal, rig,
               f"UpperArm.{side}", rotation=(0, math.radians(122 * sign), 0))
        sphere(f"Elbow.{side}", (.92 * sign, -.01, 1.83), (.125, .14, .125), leather_hi, rig, f"Forearm.{side}")
        sphere(f"Forearm.{side}", (1.055 * sign, 0, 1.76), (.12, .13, .27), leather, rig,
               f"Forearm.{side}", rotation=(0, math.radians(146 * sign), 0))
        box(f"Bracer.{side}", (1.02 * sign, -.13, 1.80), (.105, .025, .16), gold,
            rig, f"Forearm.{side}", rotation=(0, 0, math.radians(-34 * sign)), bevel=.018)
        sphere(f"Hand.{side}", (1.255 * sign, -.02, 1.42), (.115, .105, .15), skin_light, rig,
               f"Hand.{side}", rotation=(0, math.radians(155 * sign), 0))

        sphere(f"Hip bridge.{side}", (.20 * sign, 0, 1.02), (.18, .18, .17), teal_dark, rig, "Hips")
        sphere(f"Thigh.{side}", (.265 * sign, 0, .65), (.15, .17, .46), teal_dark, rig, f"Thigh.{side}")
        sphere(f"Knee guard.{side}", (.28 * sign, -.04, .28), (.15, .16, .125), leather_hi, rig, f"Shin.{side}")
        sphere(f"Shin.{side}", (.28 * sign, 0, -.03), (.15, .17, .42), leather, rig, f"Shin.{side}")
        box(f"Boot.{side}", (.28 * sign, -.17, -.43), (.17, .30, .14), leather, rig, f"Foot.{side}", bevel=.075)
        box(f"Boot cuff.{side}", (.28 * sign, -.03, -.21), (.175, .17, .07), leather_hi, rig, f"Shin.{side}", bevel=.04)
        box(f"Boot trim.{side}", (.28 * sign, -.245, -.34), (.18, .21, .025), gold, rig, f"Foot.{side}", bevel=.015)

    # The sword is a rigid child of the hand, so every keyed arm pose carries it correctly.
    box("Sword grip", (1.31, -.02, 1.18), (.045, .045, .22), leather, rig, "Hand.R",
        rotation=(0, 0, math.radians(-8)), bevel=.025)
    box("Sword guard", (1.34, -.02, .99), (.24, .055, .045), gold, rig, "Hand.R",
        rotation=(0, 0, math.radians(-8)), bevel=.025)
    sword_blade("Sword blade", (1.43, -.02, .43), .21, .06, 1.22,
                steel, rig, "Hand.R", rotation=(0, 0, math.radians(-8)))
    box("Sword aether channel", (1.435, -.057, .45), (.018, .008, .49),
        blade_glow, rig, "Hand.R", rotation=(0, 0, math.radians(-8)), bevel=.006)


def reset_pose(rig):
    for bone in rig.pose.bones:
        bone.rotation_euler = (0, 0, 0)
        bone.location = (0, 0, 0)
        bone.scale = (1, 1, 1)


def apply_pose(rig, pose):
    reset_pose(rig)
    for name, values in pose.items():
        bone = rig.pose.bones[name]
        if "r" in values:
            bone.rotation_euler = tuple(math.radians(value) for value in values["r"])
        if "l" in values:
            bone.location = values["l"]
        if "s" in values:
            bone.scale = values["s"]


def keyed(rig, frame, pose):
    bpy.context.scene.frame_set(frame)
    apply_pose(rig, pose)
    for bone in rig.pose.bones:
        bone.keyframe_insert("rotation_euler", frame=frame)
        bone.keyframe_insert("location", frame=frame)
        bone.keyframe_insert("scale", frame=frame)


BASE = {}
RUN_A = {"Root": {"l": (0, 0, -.025)}, "Hips": {"r": (0, 0, 4)},
         "Chest": {"r": (0, 0, -3)}, "Head": {"r": (0, 0, 2)},
         "UpperArm.L": {"r": (0, 0, -42)}, "UpperArm.R": {"r": (0, 0, 44)},
         "Forearm.L": {"r": (0, 0, 28)}, "Forearm.R": {"r": (0, 0, -30)},
         "Thigh.L": {"r": (0, 0, 38)}, "Shin.L": {"r": (0, 0, -28)},
         "Thigh.R": {"r": (0, 0, -36)}, "Shin.R": {"r": (0, 0, 45)},
         "CoatTail.L": {"r": (0, 0, 18)}, "CoatTail.R": {"r": (0, 0, 24)}}
RUN_B = {"Root": {"l": (0, 0, -.025)}, "Hips": {"r": (0, 0, -4)},
         "Chest": {"r": (0, 0, 3)}, "Head": {"r": (0, 0, -2)},
         "UpperArm.L": {"r": (0, 0, 44)}, "UpperArm.R": {"r": (0, 0, -42)},
         "Forearm.L": {"r": (0, 0, -30)}, "Forearm.R": {"r": (0, 0, 28)},
         "Thigh.L": {"r": (0, 0, -36)}, "Shin.L": {"r": (0, 0, 45)},
         "Thigh.R": {"r": (0, 0, 38)}, "Shin.R": {"r": (0, 0, -28)},
         "CoatTail.L": {"r": (0, 0, 24)}, "CoatTail.R": {"r": (0, 0, 18)}}
JUMP = {"Root": {"l": (0, 0, .26)},
        "Chest": {"r": (0, 0, -5)}, "Head": {"r": (0, 0, 4)},
        "UpperArm.L": {"r": (0, 0, -28)}, "UpperArm.R": {"r": (0, 0, 34)},
        "Thigh.L": {"r": (0, 0, 22)}, "Shin.L": {"r": (0, 0, -48)},
        "Thigh.R": {"r": (0, 0, -18)}, "Shin.R": {"r": (0, 0, 54)},
        "CoatTail.L": {"r": (0, 0, 28)}, "CoatTail.R": {"r": (0, 0, 35)}}
ATTACK_READY = {**BASE, "Hips": {"r": (0, 0, 4)}, "Chest": {"r": (0, 0, -8)},
                "Head": {"r": (0, 0, 5)}, "UpperArm.R": {"r": (0, 0, -85)},
                "Forearm.R": {"r": (0, 0, -25)}, "Thigh.L": {"r": (0, 0, 12)},
                "Thigh.R": {"r": (0, 0, -15)}}
ATTACK_STRIKE = {"Hips": {"r": (0, 0, -6)}, "Chest": {"r": (0, 0, 12)},
                 "Head": {"r": (0, 0, -7)}, "UpperArm.R": {"r": (0, 0, 62)},
                 "Forearm.R": {"r": (0, 0, -18)}, "Hand.R": {"r": (0, 0, -12)},
                 "UpperArm.L": {"r": (0, 0, 30)}, "Thigh.L": {"r": (0, 0, -22)},
                 "Thigh.R": {"r": (0, 0, 24)}, "CoatTail.L": {"r": (0, 0, 30)},
                 "CoatTail.R": {"r": (0, 0, 38)}}


def create_actions(rig):
    actions = {}
    sequences = {
        "idle": [(1, BASE), (12, {**BASE, "Head": {"r": (0, 0, 2)}}), (24, BASE)],
        "run": [(1, RUN_A), (5, BASE), (9, RUN_B), (13, BASE), (17, RUN_A)],
        "jump": [(1, {**BASE, "Root": {"l": (0, 0, -.08)}, "Thigh.L": {"r": (0, 0, 18)}, "Thigh.R": {"r": (0, 0, 18)}}),
                 (4, JUMP), (8, {**JUMP, "Root": {"l": (0, 0, .38)}}),
                 (12, {**JUMP, "Root": {"l": (0, 0, .12)}})],
        "attack": [(1, BASE), (3, ATTACK_READY), (5, ATTACK_STRIKE),
                   (7, {**ATTACK_STRIKE, "UpperArm.R": {"r": (0, 0, 88)}}),
                   (9, ATTACK_READY), (11, BASE)],
    }
    rig.animation_data_create()
    for name, sequence in sequences.items():
        action = bpy.data.actions.new(f"AsterV2_{name.title()}")
        action.use_fake_user = True
        rig.animation_data.action = action
        for frame, pose in sequence:
            keyed(rig, frame, pose)
        actions[name] = action
    rig.animation_data.action = None
    reset_pose(rig)
    return actions


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def setup_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = True
    scene.render.image_settings.color_depth = "8"
    scene.render.fps = 24
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.world.color = (.008, .012, .016)

    bpy.ops.object.camera_add(location=(4.4, -12.8, 3.9))
    camera = bpy.context.object
    camera.name = "Platformer Sprite Camera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 5.15
    look_at(camera, (0, 0, 1.30))
    scene.camera = camera

    for name, kind, location, energy, color, size in (
        ("Key", "AREA", (-4, -6, 7), 1050, (1.0, .76, .58), 5.0),
        ("Teal fill", "AREA", (4, -3, 4), 760, (.05, .55, .72), 4.0),
        ("Warm rim", "AREA", (0, 4, 5), 900, (1.0, .28, .08), 3.0),
    ):
        bpy.ops.object.light_add(type=kind, location=location)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.color = color
        light.data.shape = "DISK"
        light.data.size = size
        look_at(light, (0, 0, 1.3))


def render_animation_proofs(rig, actions):
    scene = bpy.context.scene
    samples = {
        "idle": (1, 4, 7, 10, 12, 15, 18, 22),
        "run": (1, 3, 5, 7, 9, 11, 13, 15),
        "jump": (1, 2, 3, 4, 6, 8, 10, 12),
        "attack": (1, 2, 3, 4, 5, 7, 9, 11),
    }
    if os.environ.get("ASTER_V2_QUICK") == "1":
        samples = {"run": (1, 9), "attack": (3, 5)}
    for name, frames in samples.items():
        rig.animation_data.action = actions[name]
        bpy.context.view_layer.update()
        destination = OUTPUT / name
        destination.mkdir(parents=True, exist_ok=True)
        for index, frame in enumerate(frames):
            scene.frame_set(frame)
            bpy.context.view_layer.update()
            scene.render.filepath = str(destination / f"{name}_{index:02d}.png")
            bpy.ops.render.render(write_still=True)
    rig.animation_data.action = actions["idle"]
    scene.frame_set(1)


clear_scene()
rig = build_rig()
build_model(rig)
actions = create_actions(rig)
setup_scene()
if CONCEPT.exists():
    reference = bpy.data.images.load(str(CONCEPT), check_existing=True)
    reference.pack()
    rig["concept_reference"] = "art/blender/concepts/aster_v2_turnaround.png"
render_animation_proofs(rig, actions)
bpy.ops.wm.save_as_mainfile(filepath=str(ART_DIR / "aster_v2_production.blend"))
print(f"Saved Aster V2 production rig to {ART_DIR / 'aster_v2_production.blend'}")
print(f"Rendered genuine animation frames to {OUTPUT}")
