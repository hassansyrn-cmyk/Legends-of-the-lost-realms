from pathlib import Path
from PIL import Image

UPLOAD = Path('/home/ubuntu/upload')
OUT = Path('/home/ubuntu/lost-realms/art/source_frame_references')
OUT.mkdir(parents=True, exist_ok=True)
assets = {
    'boss_forest_FIXED2_8frames.png': (8, 0, 'forest_boss_source_frame.png'),
    'boss_stone_8frames_transparent.png': (8, 0, 'stone_boss_source_frame.png'),
    'boss_ice_8frames_transparent.png': (8, 0, 'ice_boss_source_frame.png'),
    'checkpoint_flag_6frames.png': (6, 0, 'checkpoint_source_frame.png'),
    'NEW_04_ice_spike_6frames.png': (6, 2, 'ice_spike_source_frame.png'),
    'NEW_05_golden_platform_6frames.png': (6, 0, 'golden_platform_source_frame.png'),
    'NEW_06_ice_platform_6frames.png': (6, 0, 'ice_platform_source_frame.png'),
}
for filename, (count, index, target) in assets.items():
    image = Image.open(UPLOAD / filename).convert('RGBA')
    left = round(index * image.width / count)
    right = round((index + 1) * image.width / count)
    image.crop((left, 0, right, image.height)).save(OUT / target)
print('Saved independent source-frame references.')
