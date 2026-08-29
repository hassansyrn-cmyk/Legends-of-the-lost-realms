from pathlib import Path
import cv2
import numpy as np
from PIL import Image

UPLOAD = Path('/home/ubuntu/upload')
OUT = Path('/home/ubuntu/lost-realms/tmp/grabcut_rect_trial')
OUT.mkdir(parents=True, exist_ok=True)
CONFIG = {
    'boss_forest_FIXED2_8frames.png': (8, (12, 10, 370, 760)),
    'boss_stone_8frames_transparent.png': (8, (18, 5, 430, 540)),
    'boss_ice_8frames_transparent.png': (8, (18, 18, 440, 525)),
}
for name, (count, rect) in CONFIG.items():
    image = cv2.imread(str(UPLOAD / name), cv2.IMREAD_UNCHANGED)
    left, right = 0, round(image.shape[1] / count)
    frame = image[:, left:right, :3]
    mask = np.zeros(frame.shape[:2], np.uint8)
    bgd = np.zeros((1, 65), np.float64)
    fgd = np.zeros((1, 65), np.float64)
    cv2.grabCut(frame, mask, rect, bgd, fgd, 10, cv2.GC_INIT_WITH_RECT)
    alpha = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 255, 0).astype(np.uint8)
    hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
    magenta = (hsv[:, :, 0] >= 134) & (hsv[:, :, 0] <= 178) & (hsv[:, :, 1] >= 28)
    alpha[magenta] = 0
    rgba = cv2.cvtColor(frame, cv2.COLOR_BGR2RGBA)
    rgba[:, :, 3] = alpha
    Image.fromarray(rgba, 'RGBA').save(OUT / f'{Path(name).stem}_frame_01.png')
print('Saved trial cutouts.')
