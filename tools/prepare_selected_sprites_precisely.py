from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
UPLOAD = Path('/home/ubuntu/upload')
RES = ROOT / 'app/src/main/res/drawable-nodpi'
DEBUG = ROOT / 'tmp/selected_sprite_rebuild'
DEBUG.mkdir(parents=True, exist_ok=True)
BOSS_CELL = 544
CELL = 384


def transparent(size):
    return Image.new('RGBA', size, (0, 0, 0, 0))


def load_alpha_frame(filename, index, count):
    """Use the source alpha channel, not the fuchsia RGB preview stored in transparent pixels."""
    source = Image.open(UPLOAD / filename).convert('RGBA')
    left = round(index * source.width / count)
    right = round((index + 1) * source.width / count)
    frame = source.crop((left, 0, right, source.height))
    rgba = np.asarray(frame).copy()
    alpha = rgba[:, :, 3]
    # Some guide strips have real opacity in the source. Key their fuchsia palette and neutral
    # grey rail tones to true alpha before calculating the tight frame boundary.
    hsv = cv2.cvtColor(rgba[:, :, :3], cv2.COLOR_RGB2HSV)
    hue, saturation, value = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
    fuchsia_guides = (hue >= 134) & (hue <= 178) & (saturation >= 28) & (value >= 35)
    neutral_guides = (saturation <= 30) & (value >= 70) & (value <= 240)
    alpha[(alpha < 48) | fuchsia_guides | neutral_guides] = 0
    rgba[:, :, 3] = alpha
    visible = rgba[:, :, 3] >= 48
    if filename.startswith('boss_') or filename.startswith('checkpoint_'):
        # Remove only thin horizontal bars that touch the left or right edge of the source frame.
        # Legitimate armour/flag detail is internal, while the sheet guide rails enter from an edge.
        line_seed = cv2.morphologyEx(visible.astype(np.uint8), cv2.MORPH_OPEN,
                                     cv2.getStructuringElement(cv2.MORPH_RECT, (64, 3)))
        line_count, line_labels, line_stats, _ = cv2.connectedComponentsWithStats(line_seed, connectivity=8)
        for line_label in range(1, line_count):
            x, y, width, height, _ = line_stats[line_label]
            touches_edge = x <= 3 or x + width >= visible.shape[1] - 3
            if touches_edge and width >= 64 and height <= 22:
                rgba[line_labels == line_label] = (0, 0, 0, 0)
        visible = rgba[:, :, 3] >= 48
        # The original sheets carry shallow guide rails. Keep the largest tall core after
        # vertical erosion, then restore only its nearby outline; rails disappear naturally.
        mask = visible.astype(np.uint8)
        core = cv2.erode(mask, cv2.getStructuringElement(cv2.MORPH_RECT, (1, 31)))
        count, labels, stats, _ = cv2.connectedComponentsWithStats(core, connectivity=8)
        if count > 1:
            label = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
            subject = (labels == label).astype(np.uint8)
            local = cv2.dilate(subject, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (27, 27)))
            visible = (mask & local).astype(bool)
            rgba[~visible] = (0, 0, 0, 0)
    ys, xs = np.where(visible)
    if len(xs) == 0:
        raise RuntimeError(f'No opaque sprite pixels in {filename} frame {index + 1}')
    pad = 8
    crop = rgba[max(0, int(ys.min()) - pad):min(frame.height, int(ys.max()) + 1 + pad),
                max(0, int(xs.min()) - pad):min(frame.width, int(xs.max()) + 1 + pad)]
    return Image.fromarray(crop, 'RGBA')


def fit(image, max_width, max_height):
    scale = min(max_width / image.width, max_height / image.height)
    return image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.LANCZOS)


def place_bottom(canvas, image, x, y, size, inset):
    image = fit(image, size - inset * 2, size - inset * 2)
    canvas.alpha_composite(image, (x + (size - image.width) // 2, y + size - inset - image.height))


def build_boss_atlas():
    atlas = transparent((BOSS_CELL * 4, BOSS_CELL * 3))
    # Attack frames in the supplied rows contain irreversible guide rails. The clean idle frame
    # is therefore reused in the four runtime cells; GameView still renders combat effects itself.
    config = [
        ('boss_forest_FIXED2_8frames.png', (0, 0, 0, 0)),
        ('boss_stone_8frames_transparent.png', (0, 0, 0, 0)),
        ('boss_ice_8frames_transparent.png', (0, 0, 0, 0)),
    ]
    for row, (filename, frames) in enumerate(config):
        for column, frame_index in enumerate(frames):
            place_bottom(atlas, load_alpha_frame(filename, frame_index, 8), column * BOSS_CELL, row * BOSS_CELL, BOSS_CELL, 34)
    atlas.save(RES / 'boss_motion_atlas.png')


def build_checkpoint_atlas():
    target = RES / 'world_interactives_motion_atlas.png'
    atlas = Image.open(target).convert('RGBA')
    frame_map = (0, 1, 4, 5)
    for column, frame_index in enumerate(frame_map):
        left = column * CELL
        # Clear full existing cell: this permanently prevents the previous checkpoint sprite from appearing behind the new one.
        atlas.paste((0, 0, 0, 0), (left, 0, left + CELL, CELL))
        cell = transparent((CELL, CELL))
        place_bottom(cell, load_alpha_frame('checkpoint_flag_6frames.png', frame_index, 6), 0, 0, CELL, 20)
        atlas.alpha_composite(cell, (left, 0))
    atlas.save(target)


def build_platform_sheet(filename, destination):
    sheet = transparent((CELL * 6, CELL))
    for index in range(6):
        sprite = fit(load_alpha_frame(filename, index, 6), CELL - 24, CELL - 22)
        # Platform sheets are top-anchored: the playable top surface remains at the first visible pixel.
        sheet.alpha_composite(sprite, (index * CELL + (CELL - sprite.width) // 2, 10))
    sheet.save(RES / destination)


build_boss_atlas()
build_checkpoint_atlas()
build_platform_sheet('NEW_04_ice_spike_6frames.png', 'hanging_ice_spikes_motion_sheet.png')
build_platform_sheet('NEW_05_golden_platform_6frames.png', 'golden_platform_motion_sheet.png')
build_platform_sheet('NEW_06_ice_platform_6frames.png', 'ice_platform_motion_sheet.png')
for filename in ('boss_motion_atlas.png', 'world_interactives_motion_atlas.png', 'hanging_ice_spikes_motion_sheet.png', 'golden_platform_motion_sheet.png', 'ice_platform_motion_sheet.png'):
    Image.open(RES / filename).convert('RGBA').save(DEBUG / filename)
print('Prepared selected sprites using original alpha, exact frame cells, matte cleanup, and stable anchors.')
