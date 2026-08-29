import json
from pathlib import Path

levels_dir = Path('/home/ubuntu/lost-realms/app/src/main/assets/levels')
for path in sorted(levels_dir.glob('level_*.json'), key=lambda item: int(item.stem.split('_')[1])):
    level = json.loads(path.read_text(encoding='utf-8'))
    checkpoint = level['checkpoint']
    x = checkpoint['x']
    supports = [platform for platform in level['platforms'] if platform['x'] <= x <= platform['x'] + platform['width'] and platform['y'] >= checkpoint['y']]
    support = min(supports, key=lambda platform: platform['y']) if supports else None
    print(f"{path.name}: checkpoint=({x},{checkpoint['y']}), support_y={support['y'] if support else 'NONE'}")
