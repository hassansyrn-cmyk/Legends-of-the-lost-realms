from pathlib import Path
import io
import numpy as np
import cv2
from PIL import Image
from rembg import new_session, remove

ROOT = Path('/home/ubuntu/lost-realms')
UPLOAD = Path('/home/ubuntu/upload')
RES = ROOT / 'app/src/main/res/drawable-nodpi'
DEBUG = ROOT / 'tmp/isolated_selected_sprite_rebuild'
DEBUG.mkdir(parents=True, exist_ok=True)
BOSS_CELL = 544
CELL = 384
SESSION = new_session('isnet-general-use')


def transparent(size):
    return Image.new('RGBA', size, (0, 0, 0, 0))


def original_frame(name, index, count):
    image = Image.open(UPLOAD / name).convert('RGBA')
    left = round(index * image.width / count)
    right = round((index + 1) * image.width / count)
    frame = image.crop((left, 0, right, image.height))
    rgba = np.asarray(frame).copy()
    bgr = rgba[:, :, :3]
    hsv = cv2.cvtColor(bgr, cv2.COLOR_RGB2HSV)
    backdrop = (hsv[:, :, 0] >= 135) & (hsv[:, :, 0] <= 178) & (hsv[:, :, 1] >= 32) & (hsv[:, :, 2] >= 40)
    rgba[backdrop, 3] = 0
    # Rembg performs better on a single opaque background than on a partly transparent source.
    cleaned = Image.fromarray(rgba, 'RGBA')
    background = Image.new('RGBA', cleaned.size, (255, 255, 255, 255))
    background.alpha_composite(cleaned)
    return background.convert('RGB')


def isolate(frame):
    buffer = io.BytesIO()
    frame.save(buffer, format='PNG')
    result = Image.open(io.BytesIO(remove(buffer.getvalue(), session=SESSION))).convert('RGBA')
    alpha = np.asarray(result.getchannel('A'))
    ys, xs = np.where(alpha > 18)
    if len(xs) == 0:
        raise RuntimeError('Subject isolation removed every pixel')
    padding = 7
    return result.crop((max(0, int(xs.min()) - padding), max(0, int(ys.min()) - padding),
                        min(result.width, int(xs.max()) + 1 + padding), min(result.height, int(ys.max()) + 1 + padding)))


def fit(frame, max_width, max_height):
    scale = min(max_width / frame.width, max_height / frame.height)
    return frame.resize((max(1, round(frame.width * scale)), max(1, round(frame.height * scale))), Image.Resampling.LANCZOS)


def put_bottom(canvas, frame, x, y, size, inset):
    frame = fit(frame, size - inset * 2, size - inset * 2)
    canvas.alpha_composite(frame, (x + (size - frame.width) // 2, y + size - inset - frame.height))


def build_boss_atlas():
    atlas = transparent((BOSS_CELL * 4, BOSS_CELL * 3))
    sources = [
        ('boss_forest_FIXED2_8frames.png', 0),
        ('boss_stone_8frames_transparent.png', 0),
        ('boss_ice_8frames_transparent.png', 0),
    ]
    for row, (name, safe_frame) in enumerate(sources):
        isolated = isolate(original_frame(name, safe_frame, 8))
        for column in range(4):
            put_bottom(atlas, isolated, column * BOSS_CELL, row * BOSS_CELL, BOSS_CELL, 32)
    atlas.save(RES / 'boss_motion_atlas.png')
    atlas.save(DEBUG / 'boss_motion_atlas.png')


def build_checkpoint_atlas():
    path = RES / 'world_interactives_motion_atlas.png'
    atlas = Image.open(path).convert('RGBA')
    idle = isolate(original_frame('checkpoint_flag_6frames.png', 0, 6))
    active = isolate(original_frame('checkpoint_flag_6frames.png', 1, 6))
    for column, frame in enumerate((idle, active, active, active)):
        left = column * CELL
        atlas.paste((0, 0, 0, 0), (left, 0, left + CELL, CELL))
        cell = transparent((CELL, CELL))
        put_bottom(cell, frame, 0, 0, CELL, 22)
        atlas.alpha_composite(cell, (left, 0))
    atlas.save(path)
    atlas.save(DEBUG / 'world_interactives_motion_atlas.png')


def build_platform_sheet(name, target):
    sheet = transparent((CELL * 6, CELL))
    for index in range(6):
        frame = isolate(original_frame(name, index, 6))
        frame = fit(frame, CELL - 28, CELL - 28)
        sheet.alpha_composite(frame, (index * CELL + (CELL - frame.width) // 2, 12))
    sheet.save(RES / target)
    sheet.save(DEBUG / target)


build_boss_atlas()
build_checkpoint_atlas()
build_platform_sheet('NEW_04_ice_spike_6frames.png', 'hanging_ice_spikes_motion_sheet.png')
build_platform_sheet('NEW_05_golden_platform_6frames.png', 'golden_platform_motion_sheet.png')
build_platform_sheet('NEW_06_ice_platform_6frames.png', 'ice_platform_motion_sheet.png')
print('Rebuilt selected sprite sheets with individual-frame subject isolation.')
