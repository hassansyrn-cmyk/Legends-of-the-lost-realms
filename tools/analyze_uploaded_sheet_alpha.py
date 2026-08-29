from collections import Counter
from pathlib import Path
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
for folder, filename in ((ROOT / 'art', 'uploaded_trap_platform_sheet.png'), (ROOT / 'art', 'uploaded_checkpoint_sheet.png'), (ROOT / 'app/src/main/res/drawable-nodpi', 'trap_platform_motion_sheet.png'), (ROOT / 'app/src/main/res/drawable-nodpi', 'checkpoint_motion_sheet.png')):
    image = Image.open(folder / filename).convert('RGBA')
    alpha = image.getchannel('A')
    colors = Counter(image.getdata())
    print(f'{filename}: size={image.size}, alpha_range={alpha.getextrema()}, transparent_pixels={sum(1 for a in alpha.getdata() if a == 0)}')
    for color, count in colors.most_common(12):
        print(f'  {color}: {count}')
