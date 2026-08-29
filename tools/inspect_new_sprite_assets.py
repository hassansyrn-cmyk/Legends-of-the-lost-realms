from pathlib import Path
from PIL import Image

UPLOAD = Path('/home/ubuntu/upload')
NAMES = [
    'boss_stone_8frames_transparent.png',
    'boss_ice_8frames_transparent.png',
    'boss_forest_FIXED2_8frames.png',
    'checkpoint_flag_6frames.png',
    'NEW_06_ice_platform_6frames.png',
    'NEW_04_ice_spike_6frames.png',
    'NEW_05_golden_platform_6frames.png',
    'ui_coin_special.png',
    'ui_shield.png',
    'ui_energy_bolt.png',
]

for name in NAMES:
    path = UPLOAD / name
    image = Image.open(path).convert('RGBA')
    alpha = image.getchannel('A')
    bbox = alpha.getbbox()
    values = list(alpha.getdata())
    opaque = sum(value == 255 for value in values)
    transparent = sum(value == 0 for value in values)
    partial = len(values) - opaque - transparent
    print(f'{name}: size={image.width}x{image.height}; alpha_bbox={bbox}; transparent={transparent}; partial={partial}; opaque={opaque}')
