from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'app/src/main/res/drawable-nodpi'
GAME_VIEW = (ROOT / 'app/src/main/java/com/manus/lostrealms/GameView.java').read_text(encoding='utf-8')
ATLAS_SPECS = [
    ('world_interactives_motion_atlas.png', (1536, 2304), 4, 6, 384, 384),
    ('trap_platform_motion_sheet.png', (1536, 1026), 6, 3, 256, 342),
    ('boss_forest_rig_parts.png', (1536, 1024), 3, 2, 512, 512),
    ('boss_stone_rig_parts.png', (1536, 1024), 3, 2, 512, 512),
    ('boss_ice_rig_parts.png', (1536, 1024), 3, 2, 512, 512),
]

for filename, dimensions, columns, rows, cell_width, cell_height in ATLAS_SPECS:
    image = Image.open(RES / filename).convert('RGBA')
    if image.size != dimensions:
        raise SystemExit(f'{filename} has {image.size}; expected {dimensions}.')
    if min(image.getchannel('A').get_flattened_data()) != 0:
        raise SystemExit(f'{filename} requires a true transparent background.')
    for row in range(rows):
        for column in range(columns):
            cell = image.crop((column * cell_width, row * cell_height, (column + 1) * cell_width, (row + 1) * cell_height))
            alpha_count = sum(pixel[3] > 0 for pixel in cell.get_flattened_data())
            if alpha_count < 900:
                raise SystemExit(f'{filename} cell row {row}, column {column} is unexpectedly empty ({alpha_count} alpha pixels).')

required_markers = [
    'R.drawable.world_interactives_motion_atlas',
    'R.drawable.trap_platform_motion_sheet',
    'R.drawable.boss_forest_rig_parts',
    'R.drawable.boss_stone_rig_parts',
    'R.drawable.boss_ice_rig_parts',
    'private static final class RigPart',
    'private static final class BossRig',
    'private void drawSkeletalBoss(Canvas c, Bitmap rigSheet, BossRig rig, float x, float groundY, float phase, float facingScale)',
    'private void drawRigPart(Canvas c, Bitmap sheet, RigPart part, float targetX, float targetY, float rotation)',
    'Bitmap rigSheet = boss.world == 1 ? bossForestRigParts : boss.world == 2 ? bossStoneRigParts : bossIceRigParts;',
    'float bossFacingScale = px < boss.x ? -1f : 1f;',
    'float bossGroundY = boss.y + 80f + bob;',
    'drawSkeletalBoss(c, rigSheet, rig, x, bossGroundY, phase, bossFacingScale);',
    'float leftArmAngle = -stride * 8f - charge * 30f + hurt * 10f;',
    'float rightArmAngle = stride * 8f + charge * 24f - hurt * 15f;',
    'int trapPulse = ((int) (animationClock * 4.2f + rect.left * .019f)) % 6;',
    'Rect trapSource = new Rect(trapColumn * 256, 0, (trapColumn + 1) * 256, 342)',
    'float surfaceY = rect.bottom;',
    'drawImage(c, trapPlatformMotionSheet, trapSource, new RectF(mid - half - 26, surfaceY - 156, mid + half + 26, surfaceY + 4));',
    'int checkpointFrame = checkpointActive ? 2 + ((int) (animationClock * 6f) % 2) : 0;',
    'drawImageTransform(c, worldInteractiveAtlas, checkpointSource',
    'private void drawProceduralCoinDrop(Canvas c, float x, float y, float radius, float phase)',
    'private void drawProceduralGemDrop(Canvas c, float x, float y, float radius, float phase, boolean secret)',
]
for marker in required_markers:
    if marker not in GAME_VIEW:
        raise SystemExit(f'Missing rig, renderer, or procedural-drop marker: {marker}')

for forbidden_marker in (
        'drawImageTransform(c, bossMotionAtlas, source',
        'bossFrame = 4 +',
        'drawImageTransform(c, collectiblesFxAtlas, pickupSource',
        'int pickupRow =',
        'int pickupFrame ='):
    if forbidden_marker in GAME_VIEW:
        raise SystemExit(f'Obsolete sprite-sheet renderer remains: {forbidden_marker}')

print('Skeletal boss rigs and additional sprite atlases: OK')
print('Validated three transparent 3x2 rig-part sheets, joint-based boss motion, 6x3 traps, restored flag, and procedural drops.')
