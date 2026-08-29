from pathlib import Path
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
ATLAS = ROOT / 'app/src/main/res/drawable-nodpi/action_fx_motion_atlas.png'
GAME_VIEW = (ROOT / 'app/src/main/java/com/manus/lostrealms/GameView.java').read_text(encoding='utf-8')

image = Image.open(ATLAS).convert('RGBA')
if image.size != (2176, 1632):
    raise SystemExit(f'Unexpected action FX atlas dimensions: {image.size}; expected 2176x1632.')

for row in range(3):
    for column in range(4):
        cell = image.crop((column * 544, row * 544, (column + 1) * 544, (row + 1) * 544))
        alpha_count = sum(pixel[3] > 0 for pixel in cell.get_flattened_data())
        if alpha_count < 600:
            raise SystemExit(f'Action FX cell row {row}, column {column} is unexpectedly empty ({alpha_count} alpha pixels).')

required_markers = [
    'R.drawable.action_fx_motion_atlas',
    'drawImageTransform(c, actionFxAtlas, runSource',
    'float runContactY = py + 52f;',
    'new RectF(tailX-74, runContactY-21, tailX+74, runContactY+9)',
    'drawImageTransform(c,actionFxAtlas,attackSource',
    'drawImageTransform(c,actionFxAtlas,sparkleSource',
]
for marker in required_markers:
    if marker not in GAME_VIEW:
        raise SystemExit(f'Missing action FX renderer marker: {marker}')

print('Action FX atlas: OK')
print('Validated 3 rows × 4 frames for run trails, sword attacks, and coin collection sparkles, including the foot-level run-trail placement.')
