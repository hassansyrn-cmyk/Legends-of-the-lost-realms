from pathlib import Path
import sys
import cv2
import numpy as np
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
sys.path.insert(0, str(ROOT / 'tools'))
from prepare_new_boss_motion_sheets import remove_generated_matte

source_path = ROOT / 'art' / 'boss_forest_8frame_sheet_clean.png'
source = np.asarray(Image.open(source_path).convert('RGBA')).copy()
frame_height, frame_width = source.shape[0] // 2, source.shape[1] // 4

for frame in range(8):
    row, col = divmod(frame, 4)
    rgba = source[row * frame_height:(row + 1) * frame_height, col * frame_width:(col + 1) * frame_width].copy()
    rgba = remove_generated_matte(rgba)
    mask = (rgba[:, :, 3] >= 40).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    records = []
    for label in range(1, count):
        x, y, width, height, area = stats[label]
        if area >= 40:
            records.append((area, x, y, width, height, round(width / max(1, height), 2)))
    records.sort(reverse=True)
    print(f'FRAME {frame}: {len(records)} components')
    for area, x, y, width, height, ratio in records[:25]:
        print(f'  area={area:6d} bbox=({x:3d},{y:3d},{width:3d},{height:3d}) ratio={ratio:5.2f}')
