from pathlib import Path
from PIL import Image
import numpy as np

import sys
path = Path(sys.argv[1]) if len(sys.argv) > 1 else Path('/home/ubuntu/lost-realms/art/boss_forest_motion_base.png')
image = np.asarray(Image.open(path).convert('RGBA'))
alpha = image[:, :, 3]
rgb = image[:, :, :3]
print(f'size={image.shape[1]}x{image.shape[0]}')
print(f'alpha range={alpha.min()}..{alpha.max()} transparent={int((alpha == 0).sum())} opaque={int((alpha == 255).sum())}')
for name, mask in {
    'magenta': (rgb[:, :, 0] > 180) & (rgb[:, :, 2] > 150) & (rgb[:, :, 1] < 100),
    'black': (rgb.max(axis=2) < 25),
}.items():
    count = int(mask.sum())
    opaque = int(((alpha >= 128) & mask).sum())
    print(f'{name}: pixels={count}, alpha>=128={opaque}')
