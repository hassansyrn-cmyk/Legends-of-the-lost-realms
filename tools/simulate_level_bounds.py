import json
from collections import deque
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ROOT = PROJECT_ROOT / 'app/src/main/assets/levels'
SPAWN_X = 120
GOAL_X = 2000
MAX_GAP = 480
MAX_HEIGHT_DELTA = 330

for level_id in range(1, 11):
    data = json.loads((ROOT / f'level_{level_id}.json').read_text(encoding='utf-8'))
    platforms = data['platforms']
    starts = [i for i, p in enumerate(platforms) if p['x'] <= SPAWN_X <= p['x'] + p['width']]
    if not starts:
        raise SystemExit(f'Level {level_id}: player spawn has no supporting platform')

    queue = deque(starts)
    seen = set(starts)
    while queue:
        index = queue.popleft()
        current = platforms[index]
        current_left = current['x']
        current_right = current['x'] + current['width']
        for other_index, other in enumerate(platforms):
            if other_index in seen:
                continue
            other_left = other['x']
            other_right = other['x'] + other['width']
            horizontal_gap = max(0, other_left - current_right, current_left - other_right)
            if horizontal_gap <= MAX_GAP and abs(other['y'] - current['y']) <= MAX_HEIGHT_DELTA:
                seen.add(other_index)
                queue.append(other_index)

    if not any(platforms[index]['x'] + platforms[index]['width'] >= GOAL_X for index in seen):
        raise SystemExit(f'Level {level_id}: no approximate platform path reaches the goal zone')

print('Headless level-bound simulation: OK')
print('Validated spawn support and an approximate connected platform route to x >= 2000 for all 10 levels.')
