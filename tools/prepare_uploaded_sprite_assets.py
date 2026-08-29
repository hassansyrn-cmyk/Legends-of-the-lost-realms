from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
RES = ROOT / 'app/src/main/res/drawable-nodpi'
UPLOAD = Path('/home/ubuntu/upload')

BOSS_CELL = 544
INTERACTIVE_CELL = 384


def transparent(size):
    return Image.new('RGBA', size, (0, 0, 0, 0))


def load_original_with_true_alpha(filename):
    """Removes only the opaque fuchsia generation backdrop from the uploaded original."""
    rgba = np.asarray(Image.open(UPLOAD / filename).convert('RGBA')).copy()
    red = rgba[:, :, 0].astype(np.int16)
    green = rgba[:, :, 1].astype(np.int16)
    blue = rgba[:, :, 2].astype(np.int16)
    # The supplied sheets use bright fuchsia as a backdrop.  Keep painted blue, green,
    # gold, and dark outlines, while removing magenta and all related horizontal bands.
    fuchsia = (red >= 145) & (blue >= 125) & (green <= 175) & ((red - green) >= 45) & ((blue - green) >= 20)
    rgba[fuchsia, 3] = 0
    return Image.fromarray(rgba, 'RGBA')


def clean_crop(image, index, count, padding=7):
    start = round(index * image.width / count)
    end = round((index + 1) * image.width / count)
    frame = image.crop((start, 0, end, image.height)).convert('RGBA')
    bbox = frame.getchannel('A').getbbox()
    if bbox is None:
        return transparent((1, 1))
    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(frame.width, bbox[2] + padding)
    bottom = min(frame.height, bbox[3] + padding)
    return frame.crop((left, top, right, bottom))


def fit(frame, max_width, max_height):
    ratio = min(max_width / frame.width, max_height / frame.height)
    width = max(1, round(frame.width * ratio))
    height = max(1, round(frame.height * ratio))
    return frame.resize((width, height), Image.Resampling.LANCZOS)


def paste_bottom(target, frame, left, top, cell_size, inset=24):
    scaled = fit(frame, cell_size - inset * 2, cell_size - inset * 2)
    x = left + (cell_size - scaled.width) // 2
    y = top + cell_size - inset - scaled.height
    target.alpha_composite(scaled, (x, y))


def build_boss_atlas():
    atlas = transparent((BOSS_CELL * 4, BOSS_CELL * 3))
    ordered = [
        'boss_forest_FIXED2_8frames.png',
        'boss_stone_8frames_transparent.png',
        'boss_ice_8frames_transparent.png',
    ]
    frame_indices = (0, 1, 4, 6)
    for row, filename in enumerate(ordered):
        image = load_original_with_true_alpha(filename)
        for column, frame_index in enumerate(frame_indices):
            paste_bottom(atlas, clean_crop(image, frame_index, 8), column * BOSS_CELL, row * BOSS_CELL, BOSS_CELL)
    atlas.save(RES / 'boss_motion_atlas.png')


def clear_cell(image, column, row, cell_size):
    image.paste((0, 0, 0, 0), (column * cell_size, row * cell_size, (column + 1) * cell_size, (row + 1) * cell_size))


def build_checkpoint_atlas():
    atlas_path = RES / 'world_interactives_motion_atlas.png'
    atlas = Image.open(atlas_path).convert('RGBA')
    image = load_original_with_true_alpha('checkpoint_flag_6frames.png')
    for column, frame_index in enumerate((0, 1, 4, 5)):
        # Clear the complete old cell before compositing, preventing the former checkpoint
        # sprite from remaining behind the replacement artwork.
        clear_cell(atlas, column, 0, INTERACTIVE_CELL)
        cell = transparent((INTERACTIVE_CELL, INTERACTIVE_CELL))
        paste_bottom(cell, clean_crop(image, frame_index, 6), 0, 0, INTERACTIVE_CELL, inset=18)
        atlas.alpha_composite(cell, (column * INTERACTIVE_CELL, 0))
    atlas.save(atlas_path)


def build_platform_sheet(source_filename, destination_name):
    image = load_original_with_true_alpha(source_filename)
    output = transparent((INTERACTIVE_CELL * 6, INTERACTIVE_CELL))
    for index in range(6):
        frame = clean_crop(image, index, 6)
        scaled = fit(frame, INTERACTIVE_CELL - 24, INTERACTIVE_CELL - 28)
        x = index * INTERACTIVE_CELL + (INTERACTIVE_CELL - scaled.width) // 2
        y = 12
        output.alpha_composite(scaled, (x, y))
    output.save(RES / destination_name)


def copy_hud_assets():
    mapping = {
        'ui_coin_special.png': 'coin_gold_new.png',
        'ui_shield.png': 'ui_shield_new.png',
        'ui_energy_bolt.png': 'ui_energy_bolt_new.png',
    }
    for source_name, target_name in mapping.items():
        Image.open(UPLOAD / source_name).convert('RGBA').save(RES / target_name)


build_boss_atlas()
build_checkpoint_atlas()
build_platform_sheet('NEW_06_ice_platform_6frames.png', 'ice_platform_motion_sheet.png')
build_platform_sheet('NEW_05_golden_platform_6frames.png', 'golden_platform_motion_sheet.png')
build_platform_sheet('NEW_04_ice_spike_6frames.png', 'hanging_ice_spikes_motion_sheet.png')
copy_hud_assets()
print('Rebuilt uploaded asset resources from original sheets with true fuchsia-keyed alpha.')
