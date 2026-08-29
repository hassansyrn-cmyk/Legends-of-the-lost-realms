from pathlib import Path
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
ATLAS = ROOT / 'app/src/main/res/drawable-nodpi/enemy_archetype_motion_atlas.png'
GAME_VIEW = (ROOT / 'app/src/main/java/com/manus/lostrealms/GameView.java').read_text(encoding='utf-8')

image = Image.open(ATLAS).convert('RGBA')
if image.size != (1536, 2304):
    raise SystemExit(f'Unexpected atlas dimensions: {image.size}; expected 1536x2304.')

frame_width, frame_height = 384, 384
for row in range(6):
    for column in range(4):
        frame = image.crop((column * frame_width, row * frame_height, (column + 1) * frame_width, (row + 1) * frame_height))
        alpha_count = sum(pixel[3] > 0 for pixel in frame.get_flattened_data())
        if alpha_count < 1200:
            raise SystemExit(f'Atlas frame row {row}, column {column} is unexpectedly empty ({alpha_count} opaque pixels).')

required = [
    'R.drawable.enemy_archetype_motion_atlas',
    'int atlasRow = e.kind - EnemyController.FAST_SKIRMISHER;',
    'int atlasColumn = e.hurtTime > 0 ? 3 : (attacking || warning ? 2',
    'drawImageTransform(c, enemyMotionAtlas, source',
]
for marker in required:
    if marker not in GAME_VIEW:
        raise SystemExit(f'Missing enemy-atlas renderer marker: {marker}')

print('Enemy sprite atlas: OK')
print('Validated 6 rows × 4 animation poses, alpha-bearing cells, and GameView routing for enemy kinds 2–7.')
