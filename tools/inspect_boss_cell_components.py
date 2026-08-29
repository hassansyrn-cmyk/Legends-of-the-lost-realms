from pathlib import Path
import cv2
import numpy as np
from PIL import Image

path = Path('/home/ubuntu/lost-realms/art/boss_forest_8frame_sheet_new.png')
rgba = np.asarray(Image.open(path).convert('RGBA'))
frame_h = rgba.shape[0] // 2
frame_w = rgba.shape[1] // 4
for frame in range(8):
    row, col = divmod(frame, 4)
    tile = rgba[row*frame_h:(row+1)*frame_h, col*frame_w:(col+1)*frame_w].copy()
    rgb, alpha = tile[:, :, :3], tile[:, :, 3].copy()
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    h, s, v = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
    fuchsia = (h >= 120) & (h <= 179) & (s >= 40) & (v >= 30)
    black = rgb.max(axis=2) <= 42
    alpha[fuchsia | black] = 0
    mask = (alpha >= 48).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    pieces = []
    for label in range(1, count):
        x, y, w, hgt, area = stats[label]
        if area >= 50:
            edge = x <= 1 or y <= 1 or x+w >= frame_w-1 or y+hgt >= frame_h-1
            pieces.append((area, x, y, w, hgt, edge))
    print(f'frame {frame}: {sorted(pieces, reverse=True)[:10]}')
