from pathlib import Path
from PIL import Image

RES = Path('/home/ubuntu/lost-realms/app/src/main/res/drawable-nodpi')
for name in ('boss_motion_atlas.png', 'world_interactives_motion_atlas.png', 'ice_platform_motion_sheet.png', 'golden_platform_motion_sheet.png'):
    image = Image.open(RES / name).convert('RGBA')
    alpha = image.getchannel('A')
    pixels = list(alpha.getdata())
    transparent = sum(value == 0 for value in pixels)
    opaque = sum(value == 255 for value in pixels)
    print(f'{name}: transparent={transparent}, opaque={opaque}, transparent_ratio={transparent / len(pixels):.3f}')
