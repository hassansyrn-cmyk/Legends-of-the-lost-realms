import bpy
import math
import os
import sys
from mathutils import Vector

blend, out_sheet, out_dir = sys.argv[-3:]
bpy.ops.wm.open_mainfile(filepath=blend)
scene=bpy.context.scene
scene.render.engine='BLENDER_WORKBENCH'
scene.display.shading.light='STUDIO'
scene.display.shading.studio_light='paint.sl'
scene.display.shading.color_type='TEXTURE'
scene.display.shading.show_shadows=True
scene.display.shading.show_cavity=True
scene.display.shading.cavity_type='WORLD'
scene.render.resolution_x=256; scene.render.resolution_y=256; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=True
scene.render.image_settings.color_mode='RGBA'
scene.render.resolution_percentage=100
mesh=next(o for o in scene.objects if o.type=='MESH')
arm=next(o for o in scene.objects if o.type=='ARMATURE')
# Hide non-character helpers and make the mesh renderable.
for o in scene.objects:
    if o.type in {'CAMERA','LIGHT','EMPTY','PLANE'}: o.hide_render=True
mesh.hide_render=False
# Bounds from the source character.
mins=Vector((1e9,1e9,1e9)); maxs=Vector((-1e9,-1e9,-1e9))
for c in mesh.bound_box:
    p=mesh.matrix_world@Vector(c)
    mins=Vector((min(mins[i],p[i]) for i in range(3)))
    maxs=Vector((max(maxs[i],p[i]) for i in range(3)))
center=(mins+maxs)/2
# Camera / lights; use a consistent 3/4 gameplay view.
bpy.ops.object.camera_add(location=(1.35,-3.4,center.z+0.02))
cam=bpy.context.object
cam.data.type='ORTHO'; cam.data.ortho_scale=1.28
cam.rotation_euler=(Vector(center)-cam.location).to_track_quat('-Z','Y').to_euler()
scene.camera=cam
bpy.ops.object.light_add(type='AREA', location=(1.5,-2.3,2.2)); key=bpy.context.object; key.data.energy=900; key.data.size=2.0; key.rotation_euler=(Vector(center)-key.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.light_add(type='AREA', location=(-1.4,-1.3,1.0)); fill=bpy.context.object; fill.data.energy=450; fill.data.size=1.5; fill.rotation_euler=(Vector(center)-fill.location).to_track_quat('-Z','Y').to_euler()
# Remove old action influence; create deterministic pose keys per cell.
if arm.animation_data: arm.animation_data_clear()
pose=arm.pose.bones
for p in pose: p.rotation_mode='XYZ'; p.rotation_euler=(0,0,0)

def reset():
    for p in pose:
        p.rotation_euler=(0,0,0)
        p.scale=(1,1,1)

def setr(name, axis, value):
    if name in pose: pose[name].rotation_euler[axis]=value

def setpose(row, f):
    reset(); t=f/7.0; s=math.sin(t*math.tau)
    if row==0: # idle breathing / subtle cape and sword movement
        setr('spine',2,0.018*s); setr('chest',2,0.022*s); setr('neck',2,-0.012*s)
        setr('upperarm_L',1,0.025*s); setr('upperarm_R',1,-0.025*s)
        setr('thigh_L',1,-0.015*s); setr('thigh_R',1,0.015*s)
    elif row==1: # run cycle
        setr('upperarm_L',1,0.38*s); setr('forearm_L',1,-0.12*s)
        setr('upperarm_R',1,-0.38*s); setr('forearm_R',1,0.12*s)
        setr('thigh_L',1,-0.42*s); setr('shin_L',1,0.18*max(0,s))
        setr('thigh_R',1,0.42*s); setr('shin_R',1,-0.18*max(0,s))
        setr('spine',2,0.045*s)
    elif row==2: # attack arc
        a=math.sin(t*math.pi)
        setr('spine',2,-0.12+0.15*t); setr('upperarm_R',1,-0.45+0.95*t); setr('forearm_R',1,-0.35+0.55*t)
        setr('upperarm_L',1,0.18*a); setr('forearm_L',1,0.08*a); setr('thigh_L',1,-0.08*a); setr('thigh_R',1,0.08*a)
    else: # hurt / defeat recoil
        setr('spine',2,0.14*math.sin(t*math.pi)); setr('neck',2,-0.1*math.sin(t*math.pi))
        setr('upperarm_L',1,-0.2*math.sin(t*math.pi)); setr('upperarm_R',1,0.2*math.sin(t*math.pi))
        setr('thigh_L',1,0.12*math.sin(t*math.pi)); setr('thigh_R',1,-0.12*math.sin(t*math.pi))

os.makedirs(out_dir, exist_ok=True)
frames=[]
for row in range(4):
    for f in range(8):
        setpose(row,f)
        scene.frame_set(1)
        path=os.path.join(out_dir,f'cell_{row}_{f}.png')
        scene.render.filepath=path
        bpy.ops.render.render(write_still=True)
        frames.append((row,f,path))
# The system Python packs the rendered cells after Blender exits.
print('ASTER_CELLS', out_dir, 'COUNT', len(frames), 'TARGET', out_sheet)
