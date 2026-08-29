from pathlib import Path
import cv2
import numpy as np

UPLOAD = Path('/home/ubuntu/upload')
ASSETS = {
    'NEW_04_ice_spike_6frames.png': 6,
    'NEW_05_golden_platform_6frames.png': 6,
    'NEW_06_ice_platform_6frames.png': 6,
    'checkpoint_flag_6frames.png': 6,
    'boss_forest_FIXED2_8frames.png': 8,
    'boss_stone_8frames_transparent.png': 8,
    'boss_ice_8frames_transparent.png': 8,
}

for name, count in ASSETS.items():
    image = cv2.imread(str(UPLOAD / name), cv2.IMREAD_UNCHANGED)
    if image is None:
        raise SystemExit(f'Cannot read {name}')
    if image.shape[2] == 3:
        image = cv2.cvtColor(image, cv2.COLOR_BGR2BGRA)
    b, g, r, a = cv2.split(image)
    r16, g16, b16 = r.astype(np.int16), g.astype(np.int16), b.astype(np.int16)
    magenta = (r16 >= 130) & (b16 >= 105) & (g16 <= 185) & ((r16 - g16) >= 30) & ((b16 - g16) >= 8)
    base = ((a > 12) & ~magenta).astype(np.uint8)
    width = image.shape[1]
    print(f'\n{name}: {width}x{image.shape[0]}, nominal frame width={width / count:.2f}')
    for index in range(count):
        left = round(index * width / count)
        right = round((index + 1) * width / count)
        frame = base[:, left:right]
        number, _, stats, _ = cv2.connectedComponentsWithStats(frame, connectivity=8)
        components = []
        for component in range(1, number):
            x, y, w, h, area = stats[component]
            if area >= 64:
                components.append((int(area), int(x), int(y), int(w), int(h)))
        components.sort(reverse=True)
        summary = ', '.join(f'a={area}@{x},{y} {w}x{h}' for area, x, y, w, h in components[:4])
        print(f'  frame {index + 1}: {summary}')
