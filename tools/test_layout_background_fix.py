from pathlib import Path
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
GAME_VIEW = (ROOT / 'app/src/main/java/com/manus/lostrealms/GameView.java').read_text(encoding='utf-8')
BACKGROUND = ROOT / 'app/src/main/res/drawable-nodpi/verdant_waterfall_backdrop.png'

if Image.open(BACKGROUND).size != (1672, 941):
    raise SystemExit('Verdant waterfall backdrop does not match the supplied image dimensions.')

required_markers = [
    'R.drawable.verdant_waterfall_backdrop',
    'drawImage(c,verdantWaterfallBackdrop',
    'float surfaceY = rect.bottom;',
    'surfaceY - 156',
    'new Rect(trapColumn * 256, 0, (trapColumn + 1) * 256, 342)',
    'surfaceY + 4',
    'float checkpointBaseY = checkpointMarkerY + 54f;',
    'new RectF(flag - 38, checkpointBaseY - 192, flag + 90, checkpointBaseY)',
    'drawImageTransform(c, worldInteractiveAtlas, checkpointSource',
    'drawImageTransform(c, actionFxAtlas, runSource',
    'facingLeft ? -.86f : .86f',
]
for marker in required_markers:
    if marker not in GAME_VIEW:
        raise SystemExit(f'Missing layout/background fix marker: {marker}')

for forbidden in [
    '(float)Math.sin(animationClock*2.1f)*2.2f',
    'new RectF(flag - 38, 468, flag + 90, 660)',
    'drawImage(c, checkpointMotionSheet, checkpointSource',
    'checkpointColumn * 192',
    'worldInteractiveAtlas, hazardSource',
    'rect.bottom+24',
]:
    if forbidden in GAME_VIEW:
        raise SystemExit(f'Obsolete placement or motion marker remains: {forbidden}')

print('Layout and Verdant background fix: OK')
print('Verified supplied background routing, surface-anchored trap motion, restored fixed checkpoint flag, and corrected run direction.')
