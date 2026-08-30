from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'app/src/main/res/drawable-nodpi'
GAME_VIEW = (ROOT / 'app/src/main/java/com/manus/lostrealms/GameView.java').read_text(encoding='utf-8')

required = {
    'boss_motion_atlas.png': (4352, 1632),
    'world_interactives_motion_atlas.png': (1536, 2304),
    'ice_platform_motion_sheet.png': (2304, 384),
    'golden_platform_motion_sheet.png': (2304, 384),
    'hanging_ice_spikes_motion_sheet.png': (2304, 384),
    'trap_platform_motion_sheet.png': (1536, 1026),
    'coin_gold_new.png': (1024, 1024),
    'ui_shield_new.png': (1024, 1024),
    'ui_energy_bolt_new.png': (1024, 1024),
}
for name, expected_size in required.items():
    image = Image.open(RES / name).convert('RGBA')
    if image.size != expected_size:
        raise SystemExit(f'{name}: expected {expected_size}, got {image.size}')
    alpha = image.getchannel('A')
    if alpha.getbbox() is None or alpha.getextrema()[0] != 0:
        raise SystemExit(f'{name}: expected a true transparent background')

sheet = Image.open(RES / 'trap_platform_motion_sheet.png').convert('RGBA')
for row in range(3):
    for column in range(6):
        frame = sheet.crop((column * 256, row * 342, (column + 1) * 256, (row + 1) * 342))
        if frame.getchannel('A').getbbox() is None:
            raise SystemExit(f'trap_platform_motion_sheet.png: empty cell at row {row}, column {column}')

required_markers = [
    'float checkpointBaseY = checkpointMarkerY + 54f;',
    'int checkpointFrame = checkpointActive ? 2 + ((int) (animationClock * 6f) % 2) : 0;',
    'new Rect(checkpointFrame * 384, 0, (checkpointFrame + 1) * 384, 384)',
    'drawImageTransform(c, worldInteractiveAtlas, checkpointSource, new RectF(flag - 38, checkpointBaseY - 192, flag + 90, checkpointBaseY), 0, 1f, 1f)',
    'trapPlatformMotionSheet = BitmapFactory.decodeResource',
    'int trapPulse = ((int) (animationClock * 4.2f + rect.left * .019f)) % 6;',
    'trapColumn = trapPulse <= 3 ? 2 + trapPulse : 8 - trapPulse;',
    'new Rect(trapColumn * 256, 0, (trapColumn + 1) * 256, 342)',
    'drawImage(c, trapPlatformMotionSheet, trapSource',
    'private static final class RigPart',
    'private static final class BossRig',
    'float bossFacingScale = px < boss.x ? -1f : 1f;',
    'drawSkeletalBoss(c, rigSheet, rig, x, bossGroundY, phase, bossFacingScale);',
]
for marker in required_markers:
    if marker not in GAME_VIEW:
        raise SystemExit(f'Missing selected-sprite integration marker: {marker}')

forbidden_markers = [
    'checkpointMotionSheet = BitmapFactory.decodeResource',
    'drawImage(c, checkpointMotionSheet, checkpointSource',
    'checkpointColumn * 192',
    'checkpointRow * 342',
    'worldInteractiveAtlas, hazardSource',
]
for marker in forbidden_markers:
    if marker in GAME_VIEW:
        raise SystemExit(f'Unexpected checkpoint shrine or old hazard renderer remains: {marker}')

print('Selected sprite integration: OK')
print('Verified restored fixed checkpoint flag, clean 6x3 trap motion, and hero-facing boss rendering.')
