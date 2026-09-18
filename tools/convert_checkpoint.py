import bpy
import os
import math

src_glb = r"C:\Users\X1 YOGA\Downloads\3d checkpoint.glb"
dst_props_dir = r"c:\Users\X1 YOGA\Documents\3D Lost Realms OpenCode\Legends-of-the-lost-realms\unity-3d\Assets\Resources\Props"
dst_tex_dir = os.path.join(dst_props_dir, "Textures")
os.makedirs(dst_props_dir, exist_ok=True)
os.makedirs(dst_tex_dir, exist_ok=True)

print("=== Converting Checkpoint 3D Model ===", flush=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

# Import GLB
bpy.ops.import_scene.gltf(filepath=src_glb)

meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
if not meshes:
    raise RuntimeError("No meshes found in GLB")

bpy.ops.object.select_all(action='DESELECT')
for m in meshes:
    m.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()

obj = bpy.context.view_layer.objects.active
obj.name = "Checkpoint"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Save basecolor texture as 512x512 JPG
tex_saved = False
mat = obj.active_material
if mat and mat.node_tree:
    for node in mat.node_tree.nodes:
        if node.type == 'TEX_IMAGE' and node.image:
            img = node.image
            img_path = os.path.join(dst_tex_dir, "Checkpoint_basecolor.jpg")
            img.scale(512, 512)
            img.filepath_raw = img_path
            img.file_format = 'JPEG'
            img.save()
            print(f"Saved texture: {img_path}", flush=True)
            tex_saved = True
            break

if not tex_saved and bpy.data.images:
    for img in bpy.data.images:
        if img.size[0] > 0 and img.size[1] > 0:
            img_path = os.path.join(dst_tex_dir, "Checkpoint_basecolor.jpg")
            img.scale(512, 512)
            img.filepath_raw = img_path
            img.file_format = 'JPEG'
            img.save()
            print(f"Saved fallback texture: {img_path}", flush=True)
            tex_saved = True
            break

# 1. Weld duplicate vertices
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=0.0005)
bpy.ops.object.mode_set(mode='OBJECT')

# 2. Decimate to ~24k polygons (ratio ~0.05)
total_faces = len(obj.data.polygons)
mod = obj.modifiers.new(name="Decimate", type='DECIMATE')
mod.ratio = 0.05
mod.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier="Decimate")
print(f"Decimated: {total_faces} -> {len(obj.data.polygons)} polygons ({len(obj.data.vertices)} vertices)", flush=True)

# 3. Check bounding box and ensure grounded at Z=0, centered in X and Y
verts = obj.data.vertices
min_x = min(v.co.x for v in verts); max_x = max(v.co.x for v in verts)
min_y = min(v.co.y for v in verts); max_y = max(v.co.y for v in verts)
min_z = min(v.co.z for v in verts); max_z = max(v.co.z for v in verts)

cx = (min_x + max_x) * 0.5
cy = (min_y + max_y) * 0.5

for v in verts:
    v.co.x -= cx
    v.co.y -= cy
    v.co.z -= min_z

obj.data.update()
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

verts = obj.data.vertices
print(f"Final bounds: X [{min(v.co.x for v in verts):.3f}, {max(v.co.x for v in verts):.3f}], Y [{min(v.co.y for v in verts):.3f}, {max(v.co.y for v in verts):.3f}], Z [{min(v.co.z for v in verts):.3f}, {max(v.co.z for v in verts):.3f}]")

# 4. Export FBX
fbx_out = os.path.join(dst_props_dir, "Checkpoint.fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx_out,
    use_selection=True,
    apply_unit_scale=True,
    bake_space_transform=True,
    axis_forward='-Z',
    axis_up='Y'
)
fbx_size = os.path.getsize(fbx_out)
print(f"Exported FBX: {fbx_out} ({fbx_size} bytes)", flush=True)

# 5. Render preview image
cam_data = bpy.data.cameras.new(name="Cam")
cam_obj = bpy.data.objects.new("Cam", cam_data)
bpy.context.scene.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj

cam_obj.location = (1.8, -1.8, 1.4)
cam_obj.rotation_euler = (math.radians(65), 0, math.radians(45))

# Sun light
light_data = bpy.data.lights.new(name="Sun", type='SUN')
light_data.energy = 3.5
light_obj = bpy.data.objects.new("Sun", light_data)
bpy.context.scene.collection.objects.link(light_obj)
light_obj.rotation_euler = (math.radians(45), math.radians(30), 0)

# Fill light
fill_data = bpy.data.lights.new(name="Fill", type='POINT')
fill_data.energy = 30.0
fill_obj = bpy.data.objects.new("Fill", fill_data)
bpy.context.scene.collection.objects.link(fill_obj)
fill_obj.location = (-1.2, 1.2, 1.5)

bpy.context.scene.render.resolution_x = 512
bpy.context.scene.render.resolution_y = 512
bpy.context.scene.render.image_settings.file_format = 'PNG'
preview_path = r"C:\Users\X1 YOGA\.gemini\antigravity\brain\8161ef4f-085a-40af-8fa6-849260c8d0c7\checkpoint_preview.png"
bpy.context.scene.render.filepath = preview_path

bpy.ops.render.render(write_still=True)
print(f"Rendered preview: {preview_path}", flush=True)
print("=== CHECKPOINT CONVERSION COMPLETE ===", flush=True)
