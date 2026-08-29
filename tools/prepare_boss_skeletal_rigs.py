from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path('/home/ubuntu/lost-realms')
ART = ROOT / 'art'
RES = ROOT / 'app/src/main/res/drawable-nodpi'
SOURCES = {
    'boss_forest_rig_parts_raw.png': 'boss_forest_rig_parts.png',
    'boss_stone_rig_parts_raw.png': 'boss_stone_rig_parts.png',
    'boss_ice_rig_parts_raw.png': 'boss_ice_rig_parts.png',
}


def remove_checkerboard(source: Image.Image) -> Image.Image:
    rgba = np.asarray(source.convert('RGBA')).copy()
    rgb = rgba[:, :, :3]
    minimum = rgb.min(axis=2)
    chroma = rgb.max(axis=2) - minimum
    # The generated checkerboard consists of very bright neutral pixels. Preserve
    # painted outlines and colored/high-chroma details; retain a 2-pixel border
    # around them so bright armor highlights are not clipped.
    clearly_painted = (minimum < 225) | (chroma > 18)
    expanded = Image.fromarray((clearly_painted.astype(np.uint8) * 255), 'L').filter(ImageFilter.MaxFilter(5))
    keep = np.asarray(expanded) > 0
    rgba[:, :, 3] = np.where(keep, rgba[:, :, 3], 0).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA')


def prepare(source_name: str, output_name: str) -> None:
    source = Image.open(ART / source_name).convert('RGBA')
    if source.size != (2304, 1536):
        raise SystemExit(f'{source_name}: unexpected size {source.size}')
    clean = remove_checkerboard(source)
    # Preserve the authored 3×2 cells while reducing each 768px source cell to
    # a compact 512px runtime cell, leaving no overlap between bones.
    output = Image.new('RGBA', (1536, 1024), (0, 0, 0, 0))
    for row in range(2):
        for column in range(3):
            part = clean.crop((column * 768, row * 768, (column + 1) * 768, (row + 1) * 768))
            part = part.resize((512, 512), Image.Resampling.LANCZOS)
            output.paste(part, (column * 512, row * 512), part)
    output.save(RES / output_name)


if __name__ == '__main__':
    RES.mkdir(parents=True, exist_ok=True)
    for source, output in SOURCES.items():
        prepare(source, output)
    print('Prepared three transparent 3x2 boss skeletal-part sheets (512px cells).')
