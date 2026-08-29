from collections import Counter
from pathlib import Path
from PIL import Image

for name in ('boss_forest_8frames_cleaned.png', 'boss_stone_8frames_cleaned.png', 'boss_ice_8frames_cleaned.png', 'checkpoint_flag_6frames_cleaned.png', 'ice_platform_6frames_cleaned.png', 'golden_platform_6frames_cleaned.png'):
    image = Image.open(Path('/home/ubuntu/lost-realms/art') / name).convert('RGBA')
    samples = []
    for y in range(0, image.height, 32):
        for x in range(0, image.width, 32):
            samples.append(image.getpixel((x, y)))
    print(name, Counter(samples).most_common(8))
