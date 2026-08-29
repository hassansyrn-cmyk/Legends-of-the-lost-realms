from pathlib import Path
import cv2
import numpy as np
from PIL import Image

atlas = np.asarray(Image.open('/home/ubuntu/lost-realms/app/src/main/res/drawable-nodpi/boss_motion_atlas.png').convert('RGBA'))
tile = atlas[:544, :544]
alpha = tile[:, :, 3]
for y in range(544):
    active = alpha[y] >= 128
    start = None
    for x, present in enumerate(np.append(active, False)):
        if present and start is None:
            start = x
        elif not present and start is not None:
            if x - start >= 180:
                colors = tile[y, start:x, :3]
                hsv = cv2.cvtColor(colors.reshape(1, -1, 3), cv2.COLOR_RGB2HSV)[0]
                print(f'run y={y} x={start}..{x-1} length={x-start}; rgb median={np.median(colors,axis=0).astype(int)} hsv median={np.median(hsv,axis=0).astype(int)}')
            start = None
