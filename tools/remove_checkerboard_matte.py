from pathlib import Path
import cv2
import numpy as np
from PIL import Image


def remove_edge_connected_checkerboard(source_path, target_path, padding=12):
    image = cv2.imread(str(source_path), cv2.IMREAD_UNCHANGED)
    if image is None:
        raise SystemExit(f'Cannot read {source_path}')
    if image.shape[2] == 3:
        image = cv2.cvtColor(image, cv2.COLOR_BGR2BGRA)
    hsv = cv2.cvtColor(image[:, :, :3], cv2.COLOR_BGR2HSV)
    hue, saturation, value = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
    # The generated checkerboard is an artificial low-saturation white/grey matte. Remove it
    # wherever it appears, including gaps between antlers and limbs, to restore true alpha.
    matte = (saturation <= 34) & (value >= 58) & (image[:, :, 3] >= 24)
    image[matte, 3] = 0
    height, width = image.shape[:2]
    alpha = image[:, :, 3]
    ys, xs = np.where(alpha >= 24)
    if len(xs) == 0:
        raise SystemExit(f'No foreground remained after checkerboard cleanup: {source_path}')
    crop = image[max(0, int(ys.min()) - padding):min(height, int(ys.max()) + 1 + padding),
                 max(0, int(xs.min()) - padding):min(width, int(xs.max()) + 1 + padding)]
    Image.fromarray(cv2.cvtColor(crop, cv2.COLOR_BGRA2RGBA), 'RGBA').save(target_path)
    print(f'{source_path.name}: {crop.shape[1]}x{crop.shape[0]} saved to {target_path.name}')


if __name__ == '__main__':
    root = Path('/home/ubuntu/lost-realms')
    remove_edge_connected_checkerboard(root / 'art/forest_boss_isolated_clean.png', root / 'art/forest_boss_transparent.png')
