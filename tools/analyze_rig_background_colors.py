from collections import Counter
from pathlib import Path
from PIL import Image

for filename in ('boss_forest_rig_parts_clean.png', 'boss_stone_rig_parts_raw.png', 'boss_ice_rig_parts_raw.png'):
    image = Image.open(Path('/home/ubuntu/lost-realms/art') / filename).convert('RGBA')
    pixels = list(image.getdata())
    counts = Counter(pixels)
    alpha_values = Counter(pixel[3] for pixel in pixels)
    print(f'\n{filename}: alpha={alpha_values.most_common(8)}')
    for color, count in counts.most_common(18):
        print(f'  {color}: {count}')
