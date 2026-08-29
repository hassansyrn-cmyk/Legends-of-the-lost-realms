from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')


def extract(source, destination):
    rgba = np.asarray(Image.open(source).convert('RGBA')).copy()
    rgb = rgba[:, :, :3]
    alpha = rgba[:, :, 3]
    # The generator output uses opaque black around the subject plus trace magenta key pixels.
    black_background = rgb.max(axis=2) <= 38
    magenta_matte = (rgb[:, :, 0] >= 175) & (rgb[:, :, 2] >= 140) & (rgb[:, :, 1] <= 120)
    alpha[black_background | magenta_matte] = 0
    mask = (alpha >= 48).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if count <= 1:
        raise RuntimeError(f'No character remained in {source}')
    main_label = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    main = labels == main_label
    # Preserve any substantial components close to the central body, but discard remote residue.
    x, y, width, height, _ = stats[main_label]
    near = np.zeros_like(main)
    for label in range(1, count):
        cx, cy, cw, ch, area = stats[label]
        close = not (cx + cw < x - 80 or cx > x + width + 80 or cy + ch < y - 80 or cy > y + height + 80)
        if label == main_label or (close and area >= 160):
            near |= labels == label
    alpha[~near] = 0
    rgba[:, :, 3] = alpha
    ys, xs = np.where(alpha >= 48)
    pad = 18
    crop = rgba[max(0, ys.min()-pad):min(rgba.shape[0], ys.max()+1+pad),
                max(0, xs.min()-pad):min(rgba.shape[1], xs.max()+1+pad)]
    Image.fromarray(crop, 'RGBA').save(destination)
    print(f'{source.name} -> {destination.name}: {crop.shape[1]}x{crop.shape[0]}')


if __name__ == '__main__':
    extract(ROOT / 'art/boss_forest_motion_base.png', ROOT / 'art/boss_forest_motion_reference_clean.png')
