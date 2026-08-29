from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
ART = ROOT / 'art'
RES = ROOT / 'app/src/main/res/drawable-nodpi'
FRAME_HEIGHT = 342


def normalized_bounds(index: int, count: int, total: int) -> tuple[int, int]:
    return round(index * total / count), round((index + 1) * total / count)


def clean_trap_sheet() -> None:
    source = Image.open(ART / 'uploaded_trap_platform_sheet.png').convert('RGBA')
    if source.size != (1536, 1024):
        raise SystemExit(f'Unexpected trap-sheet size: {source.size}')
    rgba = np.asarray(source).copy()
    rgb = rgba[:, :, :3]
    # The supplied trap sheet has an opaque white/light-gray checkerboard. Keep
    # dark stone, metal, moss, and colored details; clear only the neutral bright backdrop.
    neutral_bright = (rgb.min(axis=2) >= 238) & ((rgb.max(axis=2) - rgb.min(axis=2)) <= 24)
    rgba[:, :, 3][neutral_bright] = 0
    cleaned = Image.fromarray(rgba, 'RGBA')

    # The authored trap layout is six columns by three rows, not 6×4.
    # Repack each frame into 256×342 cells so the runtime never crosses a row boundary.
    output = Image.new('RGBA', (1536, FRAME_HEIGHT * 3), (0, 0, 0, 0))
    for row in range(3):
        top, bottom = normalized_bounds(row, 3, source.height)
        for column in range(6):
            left, right = normalized_bounds(column, 6, source.width)
            frame = cleaned.crop((left, top, right, bottom))
            output.paste(frame, (column * 256, row * FRAME_HEIGHT))
    output.save(RES / 'trap_platform_motion_sheet.png')


def repack_checkpoint_sheet() -> None:
    source = Image.open(ART / 'uploaded_checkpoint_sheet.png').convert('RGBA')
    if source.size != (1536, 1024):
        raise SystemExit(f'Unexpected checkpoint-sheet size: {source.size}')

    # The checkpoint artwork is an eight-column by three-row sequence. Repacking
    # preserves the provided alpha and avoids the former six-column crop overlap.
    output = Image.new('RGBA', (1536, FRAME_HEIGHT * 3), (0, 0, 0, 0))
    for row in range(3):
        top, bottom = normalized_bounds(row, 3, source.height)
        for column in range(8):
            left, right = normalized_bounds(column, 8, source.width)
            frame = source.crop((left, top, right, bottom))
            output.paste(frame, (column * 192, row * FRAME_HEIGHT))
    output.save(RES / 'checkpoint_motion_sheet.png')


if __name__ == '__main__':
    RES.mkdir(parents=True, exist_ok=True)
    clean_trap_sheet()
    repack_checkpoint_sheet()
    print('Prepared trap sheet as 6x3 (256x342 cells) and checkpoint sheet as 8x3 (192x342 cells).')
