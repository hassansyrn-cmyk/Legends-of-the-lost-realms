import bpy,math,random,shutil,json
from pathlib import Path
from mathutils import Vector
ROOT=Path.cwd()/'unity-3d/Assets/Resources'
ART=Path.cwd()/'unity-3d/Assets/Art/Stormveil'
ART.mkdir(parents=True,exist_ok=True)
palette=[(.19,.24,.31,1),(.34,.41,.48,1),(.62,.70,.73,1),(.72,.51,.23,1),(.12,.55,.74,1),(.035,.065,.1,1),(.65,.84,.98,1),(.28,.34,.40,1)]
def reset():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=12
def texture():
 im=bpy.data.images.new('Stormveil metal and stone palette',width=128,height=128)
 pixels=[]
 for y in range(128):
  for x in range(128):
   c=palette[x//16];f=.92+.08*math.sin(x*.7)*math.sin(y*.5)
   pixels.extend([c[0]*f,c[1]*f,c[2]*f,1])
 im.pixels=pixels;im.filepath_raw=str(ROOT/'Stormveil/StormPalette.png');im.file_format='PNG';im.save()
 mat=bpy.data.materials.new('Weathered silver, copper and stormglass');mat.use_nodes=True
 n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;mat.node_tree.links.new(n.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
 return mat
def mesh(name,loc,scale,color=1,shape='cube',bone=None):
 if shape=='cube':bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
 elif shape=='sphere':bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.5,location=loc)
 else:bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=.5,radius2=.38 if shape=='column' else 0,depth=1,location=loc)
 o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.data.materials.append(MAT)
 uv=o.data.uv_layers.active or o.data.uv_layers.new()
 for p in uv.data:p.uv=(color/8+.05,.4)
 if bone:PARTS.append((o,bone))
 return o
def export(path,anim=False):
 if anim:
  meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
  bpy.ops.object.select_all(action='DESELECT')
  for o in meshes:o.select_set(True)
  bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join()
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=anim,bake_anim_use_all_actions=anim,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,apply_scale_options='FBX_SCALE_ALL' if anim else 'FBX_SCALE_NONE',axis_forward='-Z',axis_up='Y')
def rig(spec):
 a=bpy.data.armatures.new('StormRig');o=bpy.data.objects.new('StormRig',a);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for name,head,tail,parent in spec:
  b=a.edit_bones.new(name);b.head=head;b.tail=tail
  if parent:b.parent=a.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT')
 for m,bone in PARTS:
  g=m.vertex_groups.new(name=bone);g.add(list(range(len(m.data.vertices))),1,'REPLACE');mod=m.modifiers.new('Articulated storm armour','ARMATURE');mod.object=o
 return o
def animate(o,beast=False):
 o.animation_data_create();fps=30;bpy.context.scene.render.fps=fps
 for state,frames in [('idle',60),('walk',30),('attack',30),('gethit',20),('death',45),('cast',45),('attack_2',40),('victory',45),('run',24)]:
  action=bpy.data.actions.new(state);o.animation_data.action=action
  for f in range(1,frames+1):
   t=(f-1)/(frames-1);w=t*math.pi*2
   for b in o.pose.bones:
    b.rotation_mode='XYZ';b.rotation_euler=(0,0,0)
    if b.name=='body':
     if state=='idle':b.rotation_euler.x=.035*math.sin(w)
     if state=='gethit':b.rotation_euler.x=-.25*math.sin(t*math.pi)
     if state=='death':b.rotation_euler.y=1.48*min(1,t*1.6)
     if state=='attack_2':b.rotation_euler.x=.35*math.sin(t*math.pi)
    if b.name.startswith('leg') and state in ['walk','run']:
     b.rotation_euler.x=.38*math.sin(w+(0 if b.name.endswith('L') else math.pi))
    if b.name.startswith('arm') or b.name.startswith('wing'):
     s=1 if b.name.endswith('L') else -1
     if state in ['walk','run']:b.rotation_euler.x=-.3*math.sin(w+(0 if s>0 else math.pi))
     if state=='idle':b.rotation_euler.y=s*.08*math.sin(w)
     if state=='attack':b.rotation_euler.x=-1.4*math.sin(t*math.pi);b.rotation_euler.z=s*.4*math.sin(t*math.pi)
     if state in ['cast','victory']:b.rotation_euler.z=s*.7*math.sin(t*math.pi)
     if state=='attack_2':b.rotation_euler.x=1.2*math.sin(t*math.pi)
    if b.name=='head' and state in ['attack','cast']:b.rotation_euler.x=.4*math.sin(t*math.pi)
    b.keyframe_insert('rotation_euler',frame=f)
  action.use_fake_user=True
 o.animation_data.action=bpy.data.actions.get('idle');bpy.context.scene.frame_set(1)
def preview(name,aim=(0,0,1),distance=7):
 sc=bpy.context.scene;sc.world=bpy.data.worlds.new('Preview');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.18,.23,1)
 bpy.ops.object.camera_add(location=(distance,-distance,distance*.65));cam=bpy.context.object;cam.rotation_euler=(Vector(aim)-cam.location).to_track_quat('-Z','Y').to_euler();sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=distance*.9
 for loc,power in [((3,-4,6),1400),((-4,1,4),1000)]:
  bpy.ops.object.light_add(type='AREA',location=loc);bpy.context.object.data.energy=power;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=5
 sc.render.resolution_x=900;sc.render.resolution_y=800;sc.render.resolution_percentage=100;sc.render.film_transparent=True;sc.render.filepath=str(ART/f'{name}-preview.png');bpy.ops.render.render(write_still=True)
(ROOT/'Stormveil').mkdir(parents=True,exist_ok=True)
reset();MAT=texture();PARTS=[]
mesh('Carved terrace',(0,0,-.36),(1,1,.72),1)
for i in range(8):
 a=i*math.pi/4;mesh('Rock keel',(.3*math.sin(a),.3*math.cos(a),-.85),(.5,.5,1.2),0,'cone')
export(ROOT/'Stormveil/Terrace.fbx')
reset();MAT=texture();PARTS=[]
mesh('Bridge deck',(0,0,-.15),(1,1,.3),1)
for side in [-1,1]:
 for i in range(5):mesh('Broken brass rail',(side*.47,-.45+i*.22,.24),(.025,.035,.5),3)
export(ROOT/'Stormveil/Bridge.fbx')
reset();MAT=texture();PARTS=[]
mesh('Conductor pedestal',(0,0,.3),(.9,.9,.6),0,'column');mesh('Copper spine',(0,0,1.05),(.15,.15,1.5),3,'column');mesh('Stormglass focus',(0,0,1.9),(.4,.4,.7),4,'sphere')
for s in [-1,1]:mesh('Lightning fork',(s*.3,0,1.65),(.1,.12,.75),3)
export(ROOT/'Stormveil/Conductor.fbx');preview('conductor',distance=4)
reset();MAT=texture();PARTS=[]
for x,y in [(-.8,0),(.8,0),(0,.8)]:
 mesh('Tower buttress',(x,y,2),( .55,.55,4),0,'column');mesh('Copper spire',(x,y,4.5),(.65,.65,1.3),3,'cone')
mesh('Storm tower chamber',(0,0,2),(1.4,1.4,3.4),1,'column');mesh('Broken roof',(0,0,4),(1.8,1.8,.65),2,'cone')
export(ROOT/'Stormveil/Tower.fbx');preview('tower',distance=7)
reset();MAT=texture();PARTS=[]
mesh('Scribe bell mantle',(0,0,1.1),(.85,.55,1.2),0,'cone','body');mesh('Copper cuirass',(0,0,1.45),(.6,.45,.6),3,'sphere','body');mesh('Silver mask',(0,-.03,1.95),(.4,.34,.5),2,'sphere','head');mesh('Stormglass eye',(0,-.2,1.96),(.23,.06,.12),4,'cube','head')
for s,side in [(-1,'L'),(1,'R')]:
 mesh('Armour sleeve',(s*.46,0,1.43),(.24,.28,.65),1,'column','arm'+side);mesh('Copper hand',(s*.46,0,1.1),(.24,.26,.23),3,'sphere','arm'+side)
 mesh('Shin greave',(s*.22,0,.42),(.25,.28,.8),1,'column','leg'+side);mesh('Plated foot',(s*.22,-.11,.1),(.27,.45,.2),2,'cube','leg'+side)
mesh('Scribe staff',(-.66,0,1.1),(.09,.09,1.9),3,'column','armL');mesh('Staff glass',(-.66,0,2.1),(.27,.27,.4),4,'sphere','armL')
o=rig([('body',(0,0,.85),(0,0,1.6),None),('head',(0,0,1.7),(0,0,2.05),'body'),('armL',(-.45,0,1.65),(-.45,0,1),'body'),('armR',(.45,0,1.65),(.45,0,1),'body'),('legL',(-.22,0,.85),(-.22,0,.1),'body'),('legR',(.22,0,.85),(.22,0,.1),'body')]);animate(o);export(ART/'StormScribe.fbx',True);preview('storm-scribe',distance=4)
reset();MAT=texture();PARTS=[]
mesh('Regent armoured body',(0,0,1.4),(1.65,2.3,1.65),0,'sphere','body');mesh('Regent silver breast',(0,-.85,1.6),(1.25,.7,1.4),2,'sphere','body');mesh('Lightning heart',(0,-1.2,1.8),(.65,.2,.8),4,'sphere','body')
mesh('Regent hawk head',(0,-1.25,2.7),(1,.95,.8),2,'sphere','head');mesh('Copper beak',(0,-1.87,2.6),(.6,.65,.4),3,'cone','head');mesh('Crown',(0,-1.1,3.3),(.9,.55,.75),3,'cone','head')
for s,side in [(-1,'L'),(1,'R')]:
 for y in [-.7,.8]:
  bone='leg'+('Front' if y<0 else 'Back')+side
  mesh('Articulated talon',(s*.6,y,.5),(.45,.55,1.2),1,'column',bone);mesh('Copper claw',(s*.6,y-.22,.12),(.5,.8,.24),3,'cube',bone)
 mesh('Regent folded wing',(s*1.3,.3,2.05),(1.7,1.5,.6),1,'sphere','wing'+side)
 for j in range(4):mesh('Silver wing vane',(s*(1.1+j*.25),.5+j*.27,2.1-j*.12),(.4,1.6,.22),2 if j%2 else 3,'cone','wing'+side)
spec=[('body',(0,0,.8),(0,0,2),None),('head',(0,-.9,2.25),(0,-1.4,3),'body')]
for s,side in [(-1,'L'),(1,'R')]:
 for y in [-.7,.8]:spec.append(('leg'+('Front' if y<0 else 'Back')+side,(s*.6,y,1.1),(s*.6,y,.1),'body'))
 spec.append(('wing'+side,(s*.65,.2,2),(s*2,.7,2),'body'))
o=rig(spec);animate(o,True);export(ART/'TempestRegent.fbx',True);preview('tempest-regent',distance=6)
shutil.copy2(Path.home()/'Downloads/Gecko Ranger.fbx',ART/'StormStalker.fbx')
texdir=ROOT/'Characters/Textures';texdir.mkdir(parents=True,exist_ok=True)
for role in ['StormScribe','TempestRegent']:shutil.copy2(ROOT/'Stormveil/StormPalette.png',texdir/f'{role}_basecolor.png')
for src,dest in [('lizard_creature_3d_model_basecolor.JPEG','StormStalker_basecolor.jpg'),('lizard_creature_3d_model_normal.JPEG','StormStalker_normal.jpg')]:shutil.copy2(Path.home()/'Downloads/Gecko Ranger.fbm'/src,texdir/dest)
print('STORM_ASSETS_GENERATED')

