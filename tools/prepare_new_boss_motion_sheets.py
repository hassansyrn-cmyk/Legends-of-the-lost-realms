from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
ART = ROOT / 'art'
RES = ROOT / 'app/src/main/res/drawable-nodpi'
CELL = 544
COLS = 8
ROWS = 3


def remove_generated_matte(rgba: np.ndarray) -> np.ndarray:
    """Convert generated black/fuchsia transparency guides to actual alpha."""
    rgb = rgba[:, :, :3]
    alpha = rgba[:, :, 3].copy()
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    h, s, v = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]

    # Fuchsia/purple guide rails do not belong to the three boss color palettes.
    fuchsia = (h >= 135) & (h <= 179) & (s >= 55) & (v >= 45)
    alpha[fuchsia] = 0

    # Remove only near-black background components connected to the outer frame boundary.
    black = (rgb.max(axis=2) <= 42) & (alpha >= 32)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(black.astype(np.uint8), connectivity=8)
    for label in range(1, count):
        x, y, width, height, _ = stats[label]
        if x == 0 or y == 0 or x + width == black.shape[1] or y + height == black.shape[0]:
            alpha[labels == label] = 0

    # The editor flattened the preview checkerboard into near-neutral white/gray
    # pixels (mainly 243–255) with opaque alpha. They are a background artifact,
    # not part of the darker and more chromatic boss palettes, so remove them
    # globally rather than leaving detached checkerboard tiles inside a cell.
    neutral_light = (rgb.min(axis=2) >= 238) & ((rgb.max(axis=2) - rgb.min(axis=2)) <= 24) & (alpha >= 32)
    alpha[neutral_light] = 0

    # Drop residual near-invisible alpha from flattened checkerboard anti-aliasing.
    # It has no usable sprite detail and must not leave pale guide traces in-game.
    alpha[alpha < 16] = 0
    rgba[:, :, 3] = alpha
    return rgba


def keep_subject_components(rgba: np.ndarray) -> np.ndarray:
    """Keep the central character and nearby disconnected details, not guide fragments."""
    alpha = rgba[:, :, 3]
    mask = (alpha >= 40).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if count <= 1:
        raise RuntimeError('No visible subject after matte cleanup')
    main_label = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    x, y, width, height, _ = stats[main_label]
    keep = np.zeros(mask.shape, dtype=bool)
    for label in range(1, count):
        cx, cy, cw, ch, area = stats[label]
        overlaps_subject = not (cx + cw < x - 86 or cx > x + width + 86 or cy + ch < y - 86 or cy > y + height + 86)
        # Guide rails are detached, extremely wide and shallow; they can overlap the
        # character's bounding box but are never valid limbs or armor details.
        is_horizontal_rail = cw >= 96 and cw >= ch * 8
        if not is_horizontal_rail and (label == main_label or (overlaps_subject and area >= 75)):
            keep |= labels == label
    rgba[~keep, 3] = 0
    return rgba


def remove_unattached_horizontal_rails(rgba: np.ndarray) -> np.ndarray:
    """Discard generated one-to-several-pixel horizontal guide rails before cropping."""
    mask = (rgba[:, :, 3] >= 40).astype(np.uint8)
    # The unwanted guides extend across large empty areas. A height-one kernel catches
    # single-pixel rails that a 3-pixel opening leaves behind, while the 150-pixel
    # width excludes normal character contours and limb details.
    rails = cv2.morphologyEx(mask, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (150, 1)))
    rails = cv2.dilate(rails, cv2.getStructuringElement(cv2.MORPH_RECT, (3, 3)))
    rgba[rails == 1, 3] = 0
    return rgba


def crop_frame(rgba: np.ndarray) -> Image.Image:
    rgba = keep_subject_components(remove_generated_matte(rgba))
    alpha = rgba[:, :, 3]
    ys, xs = np.where(alpha >= 40)
    if len(xs) == 0:
        raise RuntimeError('Frame became empty after subject isolation')
    pad = 14
    crop = rgba[max(0, int(ys.min()) - pad):min(rgba.shape[0], int(ys.max()) + 1 + pad),
                max(0, int(xs.min()) - pad):min(rgba.shape[1], int(xs.max()) + 1 + pad)]
    return Image.fromarray(crop, 'RGBA')


def fit_to_cell(image: Image.Image) -> Image.Image:
    scale = min((CELL - 48) / image.width, (CELL - 32) / image.height)
    width = max(1, round(image.width * scale))
    height = max(1, round(image.height * scale))
    return image.resize((width, height), Image.Resampling.LANCZOS)


def build_atlas():
    atlas = Image.new('RGBA', (CELL * COLS, CELL * ROWS), (0, 0, 0, 0))
    source_files = [
        'boss_forest_8frame_sheet_polished.png',
        'boss_stone_8frame_sheet_polished.png',
        'boss_ice_8frame_sheet_polished.png',
    ]
    for row, filename in enumerate(source_files):
        source = np.asarray(Image.open(ART / filename).convert('RGBA')).copy()
        frame_height = source.shape[0] // 2
        frame_width = source.shape[1] // 4
        for frame in range(8):
            frame_row, frame_col = divmod(frame, 4)
            raw = source[frame_row * frame_height:(frame_row + 1) * frame_height,
                         frame_col * frame_width:(frame_col + 1) * frame_width].copy()
            sprite = fit_to_cell(crop_frame(raw))
            x = frame * CELL + (CELL - sprite.width) // 2
            y = row * CELL + CELL - 18 - sprite.height
            atlas.alpha_composite(sprite, (x, y))
    atlas.save(RES / 'boss_motion_atlas.png')
    print(f'Built {RES / "boss_motion_atlas.png"}: {atlas.size[0]}x{atlas.size[1]}, 3 boss rows x 8 frames.')


if __name__ == '__main__':
    build_atlas()
