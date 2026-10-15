import bpy,math,random,shutil,json
from pathlib import Path
from mathutils import Vector
ROOT=Path.cwd()/'unity-3d/Assets/Resources'
ART=Path.cwd()/'unity-3d/Assets/Art/Astral'
ART.mkdir(parents=True,exist_ok=True)
palette=[(.18,.12,.3,1),(.57,.55,.64,1),(.83,.81,.72,1),(.78,.6,.26,1),(.46,.22,.85,1),(.045,.03,.12,1),(.29,.79,.92,1),(.69,.68,.75,1)]
def reset():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=12
def texture():
 im=bpy.data.images.new('Astral metal and stone palette',width=128,height=128)
 pixels=[]
 for y in range(128):
  for x in range(128):
   c=palette[x//16];f=.92+.08*math.sin(x*.7)*math.sin(y*.5)
   pixels.extend([c[0]*f,c[1]*f,c[2]*f,1])
 im.pixels=pixels;im.filepath_raw=str(ROOT/'Astral/AstralPalette.png');im.file_format='PNG';im.save()
 mat=bpy.data.materials.new('Weathered silver, copper and stormglass');mat.use_nodes=True
 n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;mat.node_tree.links.new(n.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
 return mat
def mesh(name,loc,scale,color=1,shape='cube',bone=None):
 if shape=='cube':bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
 elif shape=='sphere':bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.5,location=loc)
 else:bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=.5,radius2=.5 if shape=='cylinder' else .38 if shape=='column' else 0,depth=1,location=loc)
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
 a=bpy.data.armatures.new('AstralRig');o=bpy.data.objects.new('AstralRig',a);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
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

(ROOT/'Astral').mkdir(parents=True,exist_ok=True)
def ring(name,pos,radius,width,color,bone=None):
 bpy.ops.mesh.primitive_torus_add(major_radius=radius,minor_radius=width,major_segments=24,minor_segments=6,location=pos)
 o=bpy.context.object;o.name=name;o.data.materials.append(MAT)
 uv=o.data.uv_layers.active or o.data.uv_layers.new()
 for v in uv.data:v.uv=(color/8+.05,.4)
 if bone:PARTS.append((o,bone))
 return o
reset();MAT=texture();PARTS=[]
mesh('Octagonal temple foundation',(0,0,-.24),(1,1,.38),1,'column')
mesh('Ivory celestial deck',(0,0,-.04),(1,1,.08),2,'cylinder')
ring('Inlaid gold star circuit',(0,0,.015),.39,.008,3)
for i in range(8):
 a=i*math.pi/4;o=mesh('Radial star inlay',(.22*math.sin(a),.22*math.cos(a),.016),(.012,.35,.003),4);o.rotation_euler.z=-a
mesh('Broken star keel',(0,0,-.6),(.7,.7,.8),0,'cone')
export(ROOT/'Astral/Terrace.fbx')
reset();MAT=texture();PARTS=[]
mesh('Route bridge',(0,0,-.08),(1,1,.16),2)
for s in [-1,1]:
 mesh('Gold edge rail',(s*.47,0,.12),(.045,1,.24),3)
export(ROOT/'Astral/Bridge.fbx')
reset();MAT=texture();PARTS=[]
for s in [-1,1]:
 mesh('Temple column',(s*1.7,0,1.65),(.65,.65,3.3),2,'column');mesh('Column capital',(s*1.7,0,3.2),(.85,.85,.3),3)
mesh('Broken celestial lintel',(0,0,3.55),(4.2,.85,.45),2)
ring('Observatory halo',(0,0,4),1,.065,3)
export(ROOT/'Astral/Arch.fbx');preview('astral-arch',aim=(0,0,2),distance=7)
reset();MAT=texture();PARTS=[]
mesh('Orrery plinth',(0,0,.22),(1.2,1.2,.44),1,'column')
ring('Stellar equator',(0,0,1.05),.7,.07,3)
o=ring('Stellar meridian',(0,0,1.05),.63,.06,2);o.rotation_euler.x=math.pi/2
mesh('Astral focus',(0,0,1.05),(.5,.5,.65),4,'sphere')
export(ROOT/'Astral/Orrery.fbx');preview('orrery',distance=4)
for role,oracle,keeper in [('AstralSentinel',False,False),('StarOracle',True,False),('FractureKeeper',False,True)]:
 reset();MAT=texture();PARTS=[]
 # Kept at human modeling scale; runtime applies a measured role height.
 mesh('Celestial cuirass',(0,0,1.3),(.7,.44,.9),2,'sphere','body')
 mesh('Gold abdominal armour',(0,0,.95),(.55,.42,.45),3,'column','body')
 mesh('Crystal heart',(0,-.25,1.4),(.26,.12,.4),4 if keeper else 6,'sphere','body')
 mesh('Helmed face',(0,0,1.95),(.38,.38,.48),2,'sphere','head')
 mesh('Dark visor',(0,-.2,1.95),(.28,.055,.12),5,'cube','head')
 mesh('Celestial crest',(0,.03,2.3),(.16,.4,.35),3,'cone','head')
 for s,side in [(-1,'L'),(1,'R')]:
  mesh('Leg armour',(s*.2,0,.45),(.27,.28,.85),2,'column','leg'+side)
  mesh('Gold boot',(s*.2,-.12,.1),(.3,.45,.2),3,'cube','leg'+side)
  mesh('Shoulder guard',(s*.46,0,1.6),(.38,.4,.34),3,'sphere','arm'+side)
  mesh('Articulated forearm',(s*.47,0,1.2),(.22,.25,.6),2,'column','arm'+side)
  mesh('Hand',(s*.47,0,.94),(.24,.24,.22),1,'sphere','arm'+side)
 spec=[('body',(0,0,.85),(0,0,1.6),None),('head',(0,0,1.7),(0,0,2.05),'body'),('armL',(-.45,0,1.65),(-.45,0,1),'body'),('armR',(.45,0,1.65),(.45,0,1),'body'),('legL',(-.2,0,.85),(-.2,0,.1),'body'),('legR',(.2,0,.85),(.2,0,.1),'body')]
 if oracle:
  mesh('Oracle star mantle',(0,.12,1.05),(1,.6,1.6),0,'cone','body')
  ring('Oracle crown',(0,0,2.1),.48,.035,3,'head')
  for s in [-1,1]:mesh('Orbiting temple tablet',(s*.64,.2,1.1),(.24,.08,.6),4,'cube','body')
  mesh('Oracle scepter',(-.55,0,1.35),(.065,.065,1.3),3,'column','armL')
  mesh('Scepter crystal',(-.55,0,2.1),(.25,.25,.4),6,'sphere','armL')
 elif keeper:
  ring('Fracture crown',(0,0,2.35),.65,.065,3,'head')
  for i in range(8):
   a=i*math.pi/4;mesh('Crown star',(.65*math.sin(a),.65*math.cos(a),2.35),(.12,.12,.3),4,'cone','head')
  for s,side in [(-1,'L'),(1,'R')]:
   for j in [1,2]:
    bone='arm'+str(j)+side;spec.append((bone,(s*.35,.2,1.5-j*.28),(s*.86,.2,.95-j*.15),'body'))
    mesh('Extra celestial arm',(s*.7,.2,1.25-j*.23),(.22,.22,.62),2,'column',bone)
    mesh('Guardian ritual blade',(s*.84,.16,.9-j*.21),(.11,.12,.7),4,'cone',bone)
   mesh('Split celestial mantle',(s*.4,.29,1),( .5,.24,1.6),0,'cone','body')
  mesh('Keeper greatblade',(.48,-.04,.55),(.19,.14,1.25),2,'cone','armR')
 else:
  mesh('Sentinel lance',(.52,0,1.3),(.07,.07,1.8),3,'column','armR')
  mesh('Lance star tip',(.52,0,2.35),(.25,.18,.6),6,'cone','armR')
  mesh('Buckler',(-.52,-.13,1.05),(.6,.16,.65),0,'sphere','armL')
  for s,side in [(-1,'L'),(1,'R')]:mesh('Scapular wing',(s*.5,.2,1.6),(.3,.22,.9),2,'cone','body')
 o=rig(spec);animate(o);export(ART/(role+'.fbx'),True);preview(role.lower(),distance=5,aim=(0,0,1.2))
 texdir=ROOT/'Characters/Textures';texdir.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/'Astral/AstralPalette.png' if (ROOT/'Astral/AstralPalette.png').exists() else ROOT/'Astral/AstralPalette.png',texdir/(role+'_basecolor.png'))
print('ASTRAL_ASSETS_AUTHORED')

