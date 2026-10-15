import bpy,math,random
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
root=Path.cwd()/'unity-3d/Assets/Resources/Stormveil'
mat=bpy.data.materials.new('Stormveil weathered stone');mat.use_nodes=True
im=bpy.data.images.load(str(root/'StormPalette.png'));n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;mat.node_tree.links.new(n.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
def piece(name,pos,size,index,ico=False):
 if ico:bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.5,location=pos)
 else:bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
 o=bpy.context.object;o.name=name;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
 uv=o.data.uv_layers.active or o.data.uv_layers.new()
 for p in uv.data:p.uv=(index/8+.05,.4)
 if not ico:
  mod=o.modifiers.new('Worn stone edges','BEVEL');mod.width=.008;mod.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
piece('Masonry foundation',(0,0,-.18),(1,1,.28),0)
for x in range(5):
 for y in range(5):piece('Weathered flagstone',(-.4+x*.2,-.4+y*.2,-.035),(.194,.194,.07),[1,7,1,2,7][(x*3+y)%5])
for i in range(8):
 a=i*math.pi/4;piece('Broken floating keel',(.28*math.sin(a),.28*math.cos(a),-.43),(.55,.55,.9),0,True)
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=[o for o in bpy.context.scene.objects if o.type=='MESH'][0];bpy.ops.object.join()
bpy.ops.export_scene.fbx(filepath=str(root/'Terrace.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
print('STORM_STONE_DECK_REBUILT')
