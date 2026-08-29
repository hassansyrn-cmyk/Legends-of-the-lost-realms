from pathlib import Path
from PIL import Image
import numpy as np
import cv2

path = Path('/home/ubuntu/lost-realms/art/forest_boss_transparent.png')
rgba = np.asarray(Image.open(path).convert('RGBA'))
hsv = cv2.cvtColor(rgba[:, :, :3], cv2.COLOR_RGB2HSV)
# Samples fall inside the obvious remaining horizontal rail areas in the current rendered image.
for x, y in [(530, 38), (520, 66), (767, 69), (905, 140), (70, 705), (835, 895), (650, 1250)]:
    print(f'{x},{y}: rgba={tuple(rgba[y, x])} hsv={tuple(hsv[y, x])}')
