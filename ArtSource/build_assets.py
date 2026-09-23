"""Original static game meshes. Blender -> FBX, meters; Unity colliders remain separate."""
import bpy, math, pathlib, json
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parent.parent
OUT=ROOT/'Assets/Resources/Visuals'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
COLORS={'Team':(.10,.70,.87,1),'Shell':(.72,.79,.79,1),'Metal':(.16,.23,.28,1),'Dark':(.025,.055,.075,1),'Glow':(.3,.95,1,1),'Amber':(1,.56,.12,1),'Wood':(.42,.22,.09,1),'Rubber':(.035,.04,.05,1),'Deck':(.12,.19,.23,1),'Paint':(.32,.42,.46,1),'Window':(.35,.61,.70,1)}
MATS={}
for k,c in COLORS.items():
 m=bpy.data.materials.new(k);m.diffuse_color=c;MATS[k]=m
parts=[];exports=[]
def xyz(p):return (p[0],-p[2],p[1])
def finish(obj,name,mat,bevel=0):
 obj.name=name;obj.data.materials.append(MATS[mat]);parts.append(obj)
 if bevel:
  mod=obj.modifiers.new('Machined edges','BEVEL');mod.width=bevel;mod.segments=3
  bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
  mod=obj.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True
  bpy.ops.object.modifier_apply(modifier=mod.name)
 return obj
def box(name,p,size,mat='Metal',bevel=.025):
 bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(p));o=bpy.context.object;o.dimensions=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,mat,bevel)
def cyl(name,p,r,h,mat='Metal',vertices=24):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=h,location=xyz(p));return finish(bpy.context.object,name,mat,.012)
def torus(name,p,major,minor,mat='Metal',front=False):
 bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=32,minor_segments=8,location=xyz(p));o=bpy.context.object
 if front:o.rotation_euler.x=math.pi/2
 return finish(o,name,mat)
def label(text,p,size,mat='Shell',floor=False):
 bpy.ops.object.text_add(location=xyz(p));o=bpy.context.object;o.data.body=text;o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.size=size;o.data.extrude=.0015
 if not floor:o.rotation_euler=(math.pi/2,0,0)
 bpy.ops.object.convert(target='MESH');return finish(bpy.context.object,'Marking_'+text,mat)
def export(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0]
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True)
 exports.append({'asset':name,'objects':len(parts),'vertices':sum(len(o.data.vertices) for o in parts)})
 # Keep a readable source gallery outside Assets, preventing Unity's .blend auto-import.
 for o in parts:o.location.x+=len(exports)*25
 parts.clear()
# Modular robot: body, hands and boots can animate independently.
box('Chassis',(0,.99,0),(.71,.75,.53),'Team',.13)
box('Chest armor',(0,1.10,.25),(.62,.44,.13),'Shell',.06)
box('Waist',(0,.60,0),(.51,.16,.42),'Dark',.04)
cyl('Neck',(0,1.44,0),.15,.15,'Dark')
box('Helmet',(0,1.63,0),(.73,.48,.61),'Shell',.13)
box('Visor',(0,1.66,.30),(.59,.21,.045),'Dark',.045)
for x in [-.17,.17]:box('Eye LED',(x,1.67,.332),(.13,.055,.025),'Glow',.01)
for x in [-.38,.38]:box('Helmet ear',(x,1.63,0),(.07,.23,.27),'Team',.025)
box('Brow',(0,1.84,.15),(.43,.065,.24),'Team',.025)
torus('Chest reactor',(0,1.1,.337),.105,.025,'Dark',True)
box('Reactor glow',(0,1.1,.345),(.13,.13,.02),'Glow',.04)
box('Back pack',(0,1.04,-.29),(.43,.46,.17),'Metal',.05)
for y in [.92,1.02,1.12]:box('Cooling slot',(0,y,-.381),(.3,.035,.018),'Dark',.005)
box('Back status',(0,1.24,-.385),(.28,.035,.02),'Glow',.005)
for x in [-.42,.42]:box('Shoulder',(x,1.20,0),(.16,.20,.29),'Team',.045)
label('PX',(0,.80,.283),.115,'Dark');export('robot_body')
box('Gauntlet',(0,0,0),(.28,.29,.32),'Team',.07);box('Knuckle',(0,.03,.14),(.26,.15,.10),'Shell',.04);box('Wrist',(0,.13,-.02),(.16,.13,.2),'Dark',.025);export('robot_glove')
box('Sole',(0,-.10,.05),(.30,.12,.48),'Rubber',.03);box('Boot',(0,.025,0),(.29,.24,.42),'Shell',.06);box('Toe stripe',(0,.04,.213),(.22,.07,.02),'Team',.01);export('robot_boot')
# Props match the existing invisible physical proxies exactly.
box('Crate core',(0,0,0),(.94,.94,.94),'Wood',.025)
for x in [-.43,.43]:box('Crate steel frame',(x,0,0),(.14,1,1),'Metal',.02)
for y in [-.43,.43]:box('Crate crossbar',(0,y,0),(1,.14,1),'Shell',.02)
for z in [-.487,.487]:
 for x in [-.25,0,.25]:box('Wood slat',(x,0,z),(.21,.74,.024),'Amber',.008)
 label('08',(0,0,z+(.015 if z>0 else -.015)),.23,'Dark')
export('crate')
cyl('Drum',(0,0,0),.418,1.26,'Amber',32)
for y in [-.60,-.37,.37,.60]:cyl('Drum rim',(0,y,0),.429,.055,'Metal',32)
cyl('Drum top',(0,.632,0),.38,.016,'Shell',32);cyl('Cap',(.17,.655,.12),.065,.025,'Dark',16)
for y in [-.15,.15]:cyl('Black safety band',(0,y,0),.421,.10,'Dark',32)
label('16',(0,0,.43),.22,'Shell');export('barrel')
box('Seat',(0,.12,0),(.78,.16,.77),'Team',.06)
box('Back rest',(0,.59,-.31),(.77,.68,.14),'Team',.055)
for x in [-.3,.3]:
 for z in [-.30,.3]:box('Chair leg',(x,-.085,z),(.08,.28,.08),'Metal',.016)
for x in [-.3,.3]:box('Back support',(x,.35,-.32),(.075,.56,.075),'Metal',.015)
box('Back inset',(0,.63,-.225),(.55,.2,.025),'Dark',.02);export('chair')
box('Cargo case',(0,0,0),(2.08,.98,1.28),'Metal',.09)
box('Cargo lid',(0,.44,0),(2.1,.12,1.3),'Shell',.04)
for x in [-.88,.88]:box('Cargo band',(x,0,0),(.12,1.01,1.31),'Amber',.02)
for z in [-.653,.653]:
 box('Cargo inset',(0,-.02,z),(1.50,.55,.025),'Dark',.01)
 for x in [-.48,0,.48]:box('Cargo rib',(x,-.02,z),(.08,.53,.045),'Paint',.01)
label('HEAVY',(0,.02,.684),.16,'Shell');export('cargo')
box('Vent housing',(0,0,0),(2.4,1.6,1.8),'Paint',.08)
box('Vent plinth',(0,-.73,0),(2.4,.14,1.8),'Dark',.03)
box('Vent lid',(0,.75,0),(2.38,.10,1.78),'Shell',.04)
for x in [-.55,.55]:
 cyl('Fan well',(x,.81,0),.43,.035,'Dark',32)
 torus('Fan ring',(x,.835,0),.40,.035,'Metal')
 cyl('Fan hub',(x,.84,0),.10,.04,'Metal',16)
 for j in range(6):
  angle=j*math.pi/3;o=box('Fan blade',(x+math.cos(angle)*.21,.84,math.sin(angle)*.21),(.30,.025,.09),'Paint',.015);o.rotation_euler.z=-angle
for y in [-.40,-.23,-.06,.11,.28]:box('Air intake',(0,y,.908),(1.80,.07,.025),'Dark',.008)
label('AIR / 01',(0,.56,.916),.16,'Dark');export('vent')
# Roof: surface stays at y=0, all safety markers lie within the existing 20 m square.
box('Roof structure',(0,-.59,0),(20,1.10,20),'Dark',.08)
for x in range(8):
 for z in range(8):box('Deck plate',(-8.75+x*2.5,-.045,-8.75+z*2.5),(2.475,.09,2.475),'Deck' if (x+z)%2 else 'Metal',.018)
for side in range(4):
 a=side*math.pi/2
 for i in range(-9,10):
  x,z=i,9.70;rx=x*math.cos(a)+z*math.sin(a);rz=-x*math.sin(a)+z*math.cos(a)
  o=box('Warning dash',(rx,.015,rz),(.57,.02,.35),'Amber',.004);o.rotation_euler.z=a+.45
 for i in range(-8,9,4):
  x,z=i,10.01;rx=x*math.cos(a)+z*math.sin(a);rz=-x*math.sin(a)+z*math.cos(a)
  o=box('Edge lamp',(rx,-.25,rz),(2,.07,.05),'Glow',.01);o.rotation_euler.z=a
  o=box('Structural beam',(rx,-1.30,rz),(.24,1.35,.24),'Paint',.02)
for z,mat in [(-5,'Team'),(5,'Amber')]:
 torus('Spawn ring',(0,.012,z),1.18,.022,mat)
label('IMPULSE',(0,.017,0),.72,'Paint',True)
label('ROOFTOP   01',(0,.018,1.0),.25,'Paint',True);export('rooftop')
# A modular city building with stepped roof and lit windows.
box('Tower',(0,0,0),(5,12,5),'Dark',.10);box('Roof tier',(0,6.35,0),(3.5,.7,3.5),'Metal',.05)
box('Roof mast',(1.1,7.3,0),(.09,1.2,.09),'Metal',.01)
for y in range(-5,6,2):
 for x in [-1.7,-.55,.55,1.7]:
  for z in [-2.51,2.51]:box('Window',(x,y,z),(.55,.67,.015),'Window',.002)
export('skyline')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/PhysicsPlayground.blend'))
(OUT/'asset-manifest.json').write_text(json.dumps({'units':'meters','source':'ArtSource/build_assets.py','assets':exports},indent=2)+'\n')
print('ART EXPORT COMPLETE',exports)
