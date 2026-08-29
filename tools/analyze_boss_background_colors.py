from collections import Counter
from pathlib import Path
from PIL import Image

path = Path('/home/ubuntu/lost-realms/app/src/main/res/drawable-nodpi/boss_motion_atlas.png')
image = Image.open(path).convert('RGBA')
colors = Counter(image.getdata())
print(f'{path.name}: {image.size}')
for color, count in colors.most_common(40):
    print(f'{color}: {count}')
