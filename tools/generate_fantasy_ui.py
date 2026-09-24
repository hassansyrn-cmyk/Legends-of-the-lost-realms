"""Deterministically draw the Legends of Lost Realms UI art kit.

Run from the workspace root: python tools/generate_fantasy_ui.py
The artwork deliberately uses layered silhouettes rather than flat recolours.
"""
from pathlib import Path
from hashlib import md5
from PIL import Image, ImageDraw, ImageFilter
import math, random

random.seed(179025)
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "unity-3d/Assets/Resources/FantasyUI"
PREVIEW = ROOT / ".local/previews"

NAVY=(10,22,42,238); DEEP=(5,13,28,245); SLATE=(22,44,67,245)
GOLD=(197,151,70,255); GOLD_L=(250,220,137,255); GOLD_D=(91,57,24,255)
CYAN=(84,225,246,255); CYAN_L=(191,252,255,255); IVORY=(244,235,205,255)
SILVER=(186,206,211,255); SILVER_D=(89,119,133,255)

def layer(size): return Image.new("RGBA", size, (0,0,0,0))
def poly(d, points, fill, outline=None, width=1): d.polygon(points, fill=fill); outline and d.line(points+[points[0]], fill=outline, width=width, joint="curve")
def inset(polypts, amount, cx, cy):
    return [(cx+(x-cx)*(1-amount),cy+(y-cy)*(1-amount)) for x,y in polypts]
def crystal(im, x, y, r=16, flip=False):
    d=ImageDraw.Draw(im); pts=[(x,y-r),(x+r*.73,y-r*.18),(x+r*.5,y+r*.7),(x,y+r),(x-r*.5,y+r*.7),(x-r*.73,y-r*.18)]
    if flip: pts=[(2*x-a,b) for a,b in pts]
    poly(d,pts,(21,107,144,255),GOLD_D,3)
    poly(d,[(x,y-r+3),(x+r*.65,y-r*.14),(x,y+r*.7)],CYAN)
    poly(d,[(x,y-r+3),(x,y+r*.7),(x-r*.62,y-r*.14)],(51,170,202,255))
    d.line([(x,y-r+4),(x,y+r*.7)],fill=CYAN_L,width=2)
def glowdot(im,x,y,r=7):
    g=layer(im.size); ImageDraw.Draw(g).ellipse((x-r*2,y-r*2,x+r*2,y+r*2),fill=(43,220,255,90)); g=g.filter(ImageFilter.GaussianBlur(r)); im.alpha_composite(g)
    ImageDraw.Draw(im).ellipse((x-r/2,y-r/2,x+r/2,y+r/2),fill=CYAN_L)
def texture(im, box, density=160):
    d=ImageDraw.Draw(im); x0,y0,x1,y1=box
    for _ in range(density):
        x=random.randint(x0,x1); y=random.randint(y0,y1); a=random.randint(7,21)
        d.line((x,y,x+random.randint(2,16),y+random.choice([-1,0,1])),fill=(126,166,183,a),width=1)
def panel(size=(512,512)):
    im=layer(size); d=ImageDraw.Draw(im); w,h=size
    # stepped octagonal silhouette / nested chiselled frames
    p=[(38,0),(w-38,0),(w,38),(w,h-38),(w-38,h),(38,h),(0,h-38),(0,38)]
    poly(d,p,GOLD_D); poly(d,inset(p,.025,w/2,h/2),GOLD); poly(d,inset(p,.055,w/2,h/2),GOLD_L)
    q=[(55,20),(w-55,20),(w-20,55),(w-20,h-55),(w-55,h-20),(55,h-20),(20,h-55),(20,55)]
    poly(d,q,DEEP,GOLD_D,4)
    r=[(70,37),(w-70,37),(w-37,70),(w-37,h-70),(w-70,h-37),(70,h-37),(37,h-70),(37,70)]
    poly(d,r,NAVY,(42,89,112,255),2); texture(im,(45,45,w-45,h-45),330)
    # corner filigree
    for sx,sy in ((1,1),(-1,1),(1,-1),(-1,-1)):
        cx=w/2+sx*(w/2-59); cy=h/2+sy*(h/2-59)
        d.arc((cx-37,cy-37,cx+37,cy+37), 0 if sx*sy>0 else 90, 90 if sx*sy>0 else 180,fill=GOLD_L,width=4)
        d.line((cx-sx*34,cy,cx,cy-sy*34),fill=GOLD,width=4)
        crystal(im,cx-sx*7,cy-sy*7,11)
    return im

def button(active=False):
    w,h=512,128; im=layer((w,h)); d=ImageDraw.Draw(im)
    p=[(28,0),(w-28,0),(w,28),(w,h-28),(w-28,h),(28,h),(0,h-28),(0,28)]
    poly(d,p,GOLD_D); poly(d,inset(p,.025,w/2,h/2),GOLD); poly(d,inset(p,.06,w/2,h/2),GOLD_L)
    q=[(43,17),(w-43,17),(w-17,43),(w-17,h-43),(w-43,h-17),(43,h-17),(17,h-43),(17,43)]
    poly(d,q,(18,60,82,255) if active else NAVY,GOLD_D,3); texture(im,(25,25,w-25,h-25),80)
    d.line((62,32,w-62,32),fill=(102,184,195,130),width=2)
    for x in (34,w-34):
        crystal(im,x,h/2,14); glowdot(im,x,h/2,10) if active else None
    if active:
        g=layer((w,h)); ImageDraw.Draw(g).line((62,46,w-62,46),fill=(92,232,250,180),width=5); im.alpha_composite(g.filter(ImageFilter.GaussianBlur(7)))
    return im

def round_button(primary=False):
    n=256; im=layer((n,n)); d=ImageDraw.Draw(im); c=n//2
    for r,col in [(123,GOLD_D),(118,GOLD),(112,GOLD_L),(107,SILVER_D),(103,SILVER),(99,DEEP),(91,(21,48,75,255) if primary else NAVY)]:
        d.ellipse((c-r,c-r,c+r,c+r),fill=col)
    texture(im,(39,39,217,217),110)
    # Four royal cardinal spear ornaments give this a deliberate HUD silhouette.
    for a in (0,90,180,270):
        rad=math.radians(a); px=c+math.cos(rad)*118; py=c+math.sin(rad)*118
        perp=(-math.sin(rad),math.cos(rad))
        tip=(c+math.cos(rad)*132,c+math.sin(rad)*132)
        pts=[tip,(px+perp[0]*10,py+perp[1]*10),(c+math.cos(rad)*103+perp[0]*5,c+math.sin(rad)*103+perp[1]*5),(c+math.cos(rad)*103-perp[0]*5,c+math.sin(rad)*103-perp[1]*5),(px-perp[0]*10,py-perp[1]*10)]
        poly(d,pts,GOLD_D); poly(d,inset(pts,.17,c,c),GOLD_L)
    crystal(im,c,13,11)
    if primary:
        # crisp inner aura: keep the outside genuinely transparent for HUD compositing
        d.ellipse((62,62,194,194),outline=(42,137,166,110),width=5)
        d.ellipse((79,79,177,177),outline=CYAN,width=3)
    return im

def transparent_frame(kind):
    n=256; im=layer((n,n)); d=ImageDraw.Draw(im)
    if kind=="portrait":
        # squared ornate opening
        p=[(31,0),(225,0),(256,31),(256,225),(225,256),(31,256),(0,225),(0,31)]
        poly(d,p,GOLD_D); poly(d,inset(p,.04,128,128),GOLD); poly(d,inset(p,.085,128,128),GOLD_L)
        inner=[(50,22),(206,22),(234,50),(234,206),(206,234),(50,234),(22,206),(22,50)]
        d.line(inner+[inner[0]],fill=DEEP,width=10,joint="curve")
        # Erase the portrait aperture after the bevels; only the surrounding frame remains.
        d.polygon([(51,27),(205,27),(229,51),(229,205),(205,229),(51,229),(27,205),(27,51)],fill=(0,0,0,0))
        for x,y in ((30,30),(226,30),(30,226),(226,226)): crystal(im,x,y,17)
    elif kind=="minimap":
        for r,col in [(126,GOLD_D),(121,GOLD),(112,GOLD_L),(103,DEEP)]:
            d.ellipse((128-r,128-r,128+r,128+r),outline=col,width=7)
        for a in range(0,360,45):
            rad=math.radians(a); x=128+math.cos(rad)*112; y=128+math.sin(rad)*112
            d.polygon([(x,y),(x+math.cos(rad+1.6)*14,y+math.sin(rad+1.6)*14),(x+math.cos(rad-1.6)*14,y+math.sin(rad-1.6)*14)],fill=GOLD_L)
        crystal(im,128,14,15)
    else:
        for r,col in [(122,GOLD_D),(116,GOLD),(104,GOLD_L),(95,DEEP)]:
            d.ellipse((128-r,128-r,128+r,128+r),outline=col,width=8)
        d.arc((33,33,223,223),40,165,fill=(50,113,137,255),width=5)
        crystal(im,128,11,14)
    if kind in ("minimap", "joystick"):
        # small cardinal spearheads turn a generic ring into a royal instrument.
        for a in (0,90,180,270):
            rad=math.radians(a); v=(math.cos(rad),math.sin(rad)); p=(-v[1],v[0])
            cx,cy=128+v[0]*114,128+v[1]*114
            pts=[(cx+v[0]*15,cy+v[1]*15),(cx+p[0]*9,cy+p[1]*9),
                 (128+v[0]*97+p[0]*4,128+v[1]*97+p[1]*4),(128+v[0]*97-p[0]*4,128+v[1]*97-p[1]*4),(cx-p[0]*9,cy-p[1]*9)]
            poly(d,pts,GOLD_D); poly(d,inset(pts,.16,128,128),GOLD_L)
    return im

def crest():
    im=layer((256,256)); d=ImageDraw.Draw(im)
    # Four matched compass spears: a royal insignia, not an abstract burst.
    for a in (0,90,180,270):
        rad=math.radians(a); v=(math.cos(rad),math.sin(rad)); p=(-v[1],v[0])
        pts=[(128+v[0]*22+p[0]*15,128+v[1]*22+p[1]*15),(128+v[0]*118,128+v[1]*118),
             (128+v[0]*77+p[0]*12,128+v[1]*77+p[1]*12),(128+v[0]*90,128+v[1]*90),
             (128+v[0]*77-p[0]*12,128+v[1]*77-p[1]*12),(128+v[0]*22-p[0]*15,128+v[1]*22-p[1]*15)]
        poly(d,pts,GOLD_D); poly(d,inset(pts,.12,128,128),GOLD)
        d.line([(128+v[0]*30,128+v[1]*30),(128+v[0]*107,128+v[1]*107)],fill=GOLD_L,width=3)
    d.ellipse((67,67,189,189),fill=DEEP,outline=SILVER,width=5)
    crystal(im,128,128,57)
    d.polygon([(128,84),(151,128),(128,172),(105,128)],outline=IVORY,width=4)
    return im

def divider():
    im=layer((512,32)); d=ImageDraw.Draw(im)
    d.line((0,16,512,16),fill=GOLD_D,width=8); d.line((0,14,512,14),fill=GOLD_L,width=2)
    for x in (24,488): d.polygon([(x-18,16),(x,3),(x+18,16),(x,29)],fill=GOLD); crystal(im,x,16,9)
    d.polygon([(256,1),(272,16),(256,31),(240,16)],fill=GOLD); d.polygon([(256,6),(267,16),(256,26),(245,16)],fill=CYAN)
    return im

def icon(name):
    im=layer((128,128)); d=ImageDraw.Draw(im); f=IVORY; c=CYAN; g=GOLD
    # all iconography is illustrated geometry, never glyphs/text
    if name=="blade":
        # Tilted forged sword, with unmistakable point, guard, and wrapped grip.
        poly(d,[(78,10),(87,18),(55,69),(45,70),(48,58)],SILVER_D,g,2)
        d.polygon([(80,15),(82,20),(54,62),(51,61)],fill=IVORY)
        d.line((35,73,70,102),fill=g,width=9); d.line((32,78,70,110),fill=GOLD_L,width=5)
        d.line((37,95,24,111),fill=(71,46,31,255),width=10); d.line((34,99,22,113),fill=GOLD_L,width=2)
    elif name=="jump":
        # two up-arrows with stepped shafts read at action-button scale
        for x,top,bottom in ((43,25,101),(78,9,88)):
            d.polygon([(x,top),(x-19,top+25),(x-9,top+25),(x-9,bottom),(x+9,bottom),(x+9,top+25),(x+19,top+25)],fill=c,outline=g)
            d.line((x,top+7,x,bottom-8),fill=CYAN_L,width=3)
    elif name=="dodge":
        # running humanoid: head, forward torso, opposing arms and legs
        d.ellipse((68,20,87,39),fill=f,outline=g,width=2)
        d.line((74,42,59,65,76,78),fill=IVORY,width=9)
        d.line((64,57,39,52),fill=c,width=7); d.line((67,58,88,48),fill=c,width=7)
        d.line((76,78,99,94),fill=f,width=9); d.line((76,77,54,103),fill=f,width=9)
        d.line((22,42,43,42),fill=CYAN,width=5); d.line((16,57,38,57),fill=CYAN,width=5)
    elif name=="power":
        # layered elemental flame with a hot ivory core
        poly(d,[(64,9),(82,43),(101,62),(89,103),(64,118),(35,103),(24,73),(45,49)],(191,87,44,255),g,3)
        poly(d,[(65,35),(78,63),(74,83),(64,102),(48,87),(51,65)],c,IVORY,2)
        d.polygon([(65,54),(70,74),(64,89),(58,75)],fill=CYAN_L)
    elif name=="parry":
        # faceted heraldic shield
        poly(d,[(64,14),(103,29),(96,78),(64,112),(32,78),(25,29)],(27,88,116,255),g,5)
        d.polygon([(64,22),(90,34),(64,97)],fill=c); d.polygon([(64,22),(38,34),(64,97)],fill=(44,137,165,255))
        d.line((64,24,64,97),fill=CYAN_L,width=3)
    elif name=="spell":
        # curling comet: spiral magic trail ending at a bright crystal core
        d.arc((18,20,107,110),40,330,fill=c,width=9)
        d.arc((39,40,89,91),40,330,fill=CYAN_L,width=6)
        d.arc((51,51,77,78),60,335,fill=IVORY,width=4)
        d.polygon([(91,19),(114,34),(92,43)],fill=g); d.line((90,26,107,35),fill=CYAN_L,width=4)
    elif name=="atlas":
        d.rounded_rectangle((19,25,109,103),radius=8,fill=(29,95,125,255),outline=g,width=5); d.line((64,27,64,101),fill=f,width=4); d.line((26,45,57,57),fill=c,width=3); d.line((71,76,103,55),fill=c,width=3)
    elif name=="sanctuary":
        d.polygon([(18,55),(64,18),(110,55)],fill=g); d.rectangle((27,55,101,102),fill=(27,83,107,255),outline=f,width=4); d.arc((45,67,83,104),180,360,fill=c,width=7); d.line((64,9,64,38),fill=f,width=4)
    elif name in ("play","restart","pause","new"):
        if name=="play": d.polygon([(44,28),(99,64),(44,100)],fill=c,outline=f)
        if name=="pause": d.rounded_rectangle((35,26,55,102),5,fill=f); d.rounded_rectangle((73,26,93,102),5,fill=f)
        if name=="restart": d.arc((22,22,106,106),35,315,fill=c,width=10); d.polygon([(94,17),(112,45),(83,42)],fill=f)
        if name=="new": d.polygon([(64,12),(75,51),(116,64),(75,77),(64,116),(52,77),(12,64),(52,51)],fill=c,outline=f)
    elif name in ("coin","gem","star"):
        if name=="coin": d.ellipse((20,20,108,108),fill=g,outline=GOLD_L,width=5); d.ellipse((34,34,94,94),outline=GOLD_D,width=4); d.polygon([(64,42),(76,64),(64,86),(52,64)],fill=f)
        if name=="gem": crystal(im,64,64,47)
        if name=="star":
            pts=[(64+math.cos(math.radians(-90+i*72))*51,64+math.sin(math.radians(-90+i*72))*51) if i%2==0 else (64+math.cos(math.radians(-90+i*72))*23,64+math.sin(math.radians(-90+i*72))*23) for i in range(10)]
            poly(d,pts,g,GOLD_L,3); d.polygon([(64,26),(72,60),(64,83),(56,60)],fill=f)
    return im

def save(name, im, border=None):
    p=OUT/(name+".png"); im.save(p)
    guid=md5(("LegendsLostRealms/"+name).encode()).hexdigest()
    b="0, 0, 0, 0" if border is None else border
    meta=f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps: {{enableMipMap: 0}}
  isReadable: 0
  sRGBTexture: 1
  textureType: 8
  spriteMode: 1
  spritePixelsToUnits: 100
  spriteBorder: {{x: {b.split(', ')[0]}, y: {b.split(', ')[1]}, z: {b.split(', ')[2]}, w: {b.split(', ')[3]}}}
  alphaUsage: 1
  alphaIsTransparency: 1
  wrapU: 1
  wrapV: 1
  filterMode: 1
"""
    p.with_suffix(".png.meta").write_text(meta)

def main():
    OUT.mkdir(parents=True,exist_ok=True); PREVIEW.mkdir(parents=True,exist_ok=True)
    assets=[
      ("panel",panel(),"64, 64, 64, 64"),("button",button(),"40, 24, 40, 24"),("button-active",button(True),"40, 24, 40, 24"),
      ("round",round_button(),None),("round-primary",round_button(True),None),("portrait-frame",transparent_frame("portrait"),None),
      ("minimap-frame",transparent_frame("minimap"),None),("joystick-base",transparent_frame("joystick"),None),
      ("joystick-handle",round_button().resize((128,128),Image.Resampling.LANCZOS),None),("crest",crest(),None),("divider",divider(),None)]
    for name,im,b in assets: save(name,im,b)
    names=["blade","jump","dodge","power","parry","spell","atlas","sanctuary","play","restart","pause","new","coin","gem","star"]
    for name in names: save("icon-"+name,icon(name),None)
    # art-directed contact sheet on navy field
    sheet=Image.new("RGBA",(1200,1050),(8,18,35,255)); d=ImageDraw.Draw(sheet)
    d.rectangle((0,0,1200,1050),outline=(197,151,70,255),width=8)
    d.text((38,25),"LEGENDS OF LOST REALMS  /  FANTASY UI KIT",fill=IVORY)
    x,y=35,85
    for name,im,_ in assets:
        thumb=im.copy(); thumb.thumbnail((240,180)); sheet.alpha_composite(thumb,(x+(240-thumb.width)//2,y))
        d.text((x,y+184),name,fill=(191,252,255,255)); x+=280
        if x>1000: x=35; y+=230
    y+=15; x=35
    for name in names:
        im=icon(name); sheet.alpha_composite(im,(x,y)); d.text((x,y+132),name,fill=IVORY); x+=150
        if x>1050: x=35; y+=172
    sheet.save(PREVIEW/"fantasy-kit.png")
if __name__ == "__main__": main()