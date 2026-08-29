from pathlib import Path
from PIL import Image

RES = Path('/home/ubuntu/lost-realms/app/src/main/res/drawable-nodpi')
for name in ('boss_forest_rig_parts.png', 'boss_stone_rig_parts.png', 'boss_ice_rig_parts.png'):
    image = Image.open(RES / name).convert('RGBA')
    if image.size != (1536, 1024):
        raise SystemExit(f'{name}: expected 1536x1024, got {image.size}')
    alpha = image.getchannel('A')
    if alpha.getbbox() is None or min(alpha.get_flattened_data()) != 0:
        raise SystemExit(f'{name}: expected a true transparent canvas')
    for row in range(2):
        for column in range(3):
            cell = alpha.crop((column * 512, row * 512, (column + 1) * 512, (row + 1) * 512))
            bbox = cell.getbbox()
            pixels = sum(value > 0 for value in cell.get_flattened_data())
            if bbox is None or pixels < 3000:
                raise SystemExit(f'{name}: rig part {row},{column} is unexpectedly empty ({pixels} pixels)')
            if bbox[0] == 0 and bbox[1] == 0 and bbox[2] == 512 and bbox[3] == 512:
                raise SystemExit(f'{name}: rig part {row},{column} retains an opaque background')
            print(f'{name}: part {row},{column} bbox={bbox} alpha_pixels={pixels}')
print('Boss rig parts: OK')
