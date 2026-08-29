from pathlib import Path
from PIL import Image, ImageDraw
import math

ROOT = Path('/home/ubuntu/lost-realms-v4114-staging')
RES = ROOT / 'app/src/main/res/drawable-nodpi'
OUT = ROOT / 'v4114_boss_rig_motion_previews.png'
RIGS = {
    'FOREST': {'sheet':'boss_forest_rig_parts.png','torso':((577,49,925,479),131.429,162.398,-65.715,-282.396),'head':((110,7,482,400),140.493,148.424,.470,.970,-3.943,-271.028),'armR':((1092,40,1326,480),88.375,166.175,.550,.060,52.572,-261.285),'armL':((219,523,439,947),83.087,160.132,.420,.060,-52.572,-261.285),'legA':((550,520,795,955),92.529,164.286,.500,.030,-30.229,-144.358),'legB':((1083,532,1331,950),93.662,157.866,.240,.040,30.229,-144.358)},
    'STONE': {'sheet':'boss_stone_rig_parts.png','torso':((592,62,937,455),173.549,197.695,-86.775,-330.137),'head':((227,141,415,350),94.572,105.136,.500,.920,0,-318.275),'armR':((1090,53,1317,495),114.190,222.344,.500,.050,69.420,-298.506),'armL':((198,520,431,955),117.209,218.823,.600,.050,-69.420,-298.506),'legA':((616,515,815,894),100.105,190.653,.330,.040,-45.123,-168.027),'legB':((1089,513,1307,905),109.663,197.192,.220,.030,45.123,-168.027)},
    'ICE': {'sheet':'boss_ice_rig_parts.png','torso':((585,38,947,467),152.324,180.517,-76.162,-294.340),'head':((173,82,430,417),108.142,140.963,.500,.920,0,-285.314),'armR':((1128,54,1290,449),68.167,166.210,.620,.100,60.930,-261.847),'armL':((227,514,383,907),65.642,165.368,.220,.080,-60.930,-261.847),'legA':((644,539,849,929),86.261,164.106,.520,.050,-33.511,-140.901),'legB':((1122,547,1330,925),87.523,159.057,.320,.050,33.511,-140.901)},
}
STATES = [('NEUTRAL',0,0),('CHARGE',1.7,.72),('HURT',2.5,0,1)]
PANEL_W, PANEL_H, BASELINE = 400, 520, 470
OUT_IMG = Image.new('RGBA',(PANEL_W*3,PANEL_H*6),(16,25,28,255))

def layer_part(sheet, spec, cx, target, angle, canvas):
    source,w,h,ax,ay,tx,ty = spec
    piece=sheet.crop(source).resize((round(w),round(h)),Image.Resampling.LANCZOS)
    left=cx+tx-ax*w; top=BASELINE+ty-ay*h
    layer=Image.new('RGBA',canvas.size,(0,0,0,0)); layer.alpha_composite(piece,(round(left),round(top)))
    layer=layer.rotate(-angle,center=(target[0],target[1]),resample=Image.Resampling.BICUBIC)
    canvas.alpha_composite(layer)

def draw_one(name, rig, phase, charge, hurt, facing_left, dest):
    sheet=Image.open(RES/rig['sheet']).convert('RGBA')
    panel=Image.new('RGBA',(PANEL_W,PANEL_H),(16,25,28,255)); cx=PANEL_W//2
    breath=math.sin(phase*1.35); stride=math.sin(phase*1.8)
    torso_tilt=breath*1.4-charge*5+hurt*3.5
    left_arm=-stride*8-charge*30+hurt*10; right_arm=stride*8+charge*24-hurt*15
    left_leg=stride*4.8; right_leg=-stride*4.8
    hip=(cx+rig['legA'][5],BASELINE+rig['legA'][6]); hip2=(cx+rig['legB'][5],BASELINE+rig['legB'][6])
    sh=(cx+rig['armR'][5],BASELINE+rig['armR'][6]); sh2=(cx+rig['armL'][5],BASELINE+rig['armL'][6])
    layer_part(sheet,rig['legA'],cx,hip,left_leg,panel); layer_part(sheet,rig['legB'],cx,hip2,right_leg,panel)
    layer_part(sheet,rig['armR'],cx,sh,right_arm,panel)
    src,w,h,left,top=rig['torso']; torso=sheet.crop(src).resize((round(w),round(h)),Image.Resampling.LANCZOS)
    layer=Image.new('RGBA',panel.size,(0,0,0,0)); layer.alpha_composite(torso,(round(cx+left),round(BASELINE+top)))
    pivot=(cx+left+w*.5,BASELINE+top+h*.65); panel.alpha_composite(layer.rotate(-torso_tilt,center=pivot,resample=Image.Resampling.BICUBIC))
    layer_part(sheet,rig['armL'],cx,sh2,left_arm,panel)
    head=rig['head']; neck=(cx+head[5],BASELINE+head[6]+breath*3); layer_part(sheet,head,cx,neck,breath*1.2-charge*3,panel)
    if facing_left: panel=panel.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    d=ImageDraw.Draw(panel); d.line((30,BASELINE,370,BASELINE),fill=(94,194,165,220),width=2)
    d.text((12,18),f'{name} {"LEFT" if facing_left else "RIGHT"}',fill=(230,245,240,255))
    dest.alpha_composite(panel)

for row,(state,phase,charge,*rest) in enumerate(STATES):
    hurt=rest[0] if rest else 0
    for col,(name,rig) in enumerate(RIGS.items()):
        dest=Image.new('RGBA',(PANEL_W,PANEL_H),(16,25,28,255))
        draw_one(name,rig,phase,charge,hurt,False,dest)
        OUT_IMG.alpha_composite(dest,(col*PANEL_W,row*PANEL_H))
        dest=Image.new('RGBA',(PANEL_W,PANEL_H),(16,25,28,255))
        draw_one(name,rig,phase,charge,hurt,True,dest)
        # left-facing panels occupy the second half of the corresponding row block via a compact 2x layout
        if row < 3:
            OUT_IMG.alpha_composite(dest,(col*PANEL_W,(row+3)*PANEL_H))

# Row labels in the margin area are intentionally omitted; each panel carries its state only through the global row bands.
d=ImageDraw.Draw(OUT_IMG)
for y,label in [(8,'RIGHT / NEUTRAL'),(PANEL_H+8,'RIGHT / CHARGE'),(2*PANEL_H+8,'RIGHT / HURT'),(3*PANEL_H+8,'LEFT / NEUTRAL'),(4*PANEL_H+8,'LEFT / CHARGE'),(5*PANEL_H+8,'LEFT / HURT')]:
    d.text((8,y),label,fill=(130,225,194,255))
OUT_IMG.save(OUT)
print(OUT)
