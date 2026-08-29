from pathlib import Path
from PIL import Image
import numpy as np
import cv2

for name in ('boss_forest_FIXED2_8frames.png', 'boss_stone_8frames_transparent.png', 'boss_ice_8frames_transparent.png', 'checkpoint_flag_6frames.png'):
    rgba = np.asarray(Image.open(Path('/home/ubuntu/upload') / name).convert('RGBA'))
    rgb = rgba[:, :, :3]
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    samples = [(0, 0), (20, 20), (rgb.shape[1] // 2, 20), (rgb.shape[1] - 10, 100)]
    print(name)
    for x, y in samples:
        print(f'  {x},{y}: rgb={tuple(rgb[y, x])}, hsv={tuple(hsv[y, x])}, alpha={rgba[y, x, 3]}')
