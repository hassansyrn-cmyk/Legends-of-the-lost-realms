from collections import Counter
from pathlib import Path
import cv2
import numpy as np

UPLOAD = Path('/home/ubuntu/upload')
for filename in ('boss_forest_FIXED2_8frames.png', 'boss_stone_8frames_transparent.png', 'boss_ice_8frames_transparent.png'):
    image = cv2.imread(str(UPLOAD / filename), cv2.IMREAD_UNCHANGED)
    b, g, r, a = cv2.split(image)
    r16, g16, b16 = r.astype(np.int16), g.astype(np.int16), b.astype(np.int16)
    fuchsia = (r16 >= 105) & (b16 >= 75) & (g16 <= 185) & ((r16 - g16) >= 28) & ((b16 - g16) >= 8)
    mask = (a > 12) & ~fuchsia
    rgb = np.stack((r[mask], g[mask], b[mask]), axis=1)
    neutral = (rgb.max(axis=1) - rgb.min(axis=1) <= 32) & (rgb.mean(axis=1) >= 55)
    colors = Counter(map(tuple, rgb[neutral])).most_common(16)
    print(filename, colors)
