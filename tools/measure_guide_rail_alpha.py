from pathlib import Path
import numpy as np
from PIL import Image

for name, count in [('boss_forest_FIXED2_8frames.png', 8), ('boss_stone_8frames_transparent.png', 8), ('boss_ice_8frames_transparent.png', 8)]:
    rgba = np.asarray(Image.open(Path('/home/ubuntu/upload') / name).convert('RGBA'))
    frame = rgba[:, :round(rgba.shape[1] / count)]
    alpha = frame[:, :, 3]
    runs = []
    for y, row in enumerate(alpha):
        active = row >= 48
        start = None
        for x, present in enumerate(np.append(active, False)):
            if present and start is None:
                start = x
            elif not present and start is not None:
                length = x - start
                if length >= 72:
                    values = row[start:x]
                    runs.append((length, y, start, x - 1, int(values.min()), int(np.median(values)), int(values.max())))
                start = None
    print('\n' + name)
    for run in sorted(runs, reverse=True)[:15]:
        print('  width=%3d y=%3d x=%3d..%3d alpha min/med/max=%3d/%3d/%3d' % run)
