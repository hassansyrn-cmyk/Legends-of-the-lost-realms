import json
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ROOT = PROJECT_ROOT / 'app/src/main/assets/levels'
BOSS_LEVELS = {4, 7, 10}
VALID_STORIES = {'none', 'realm_intro', 'boss_intro'}
VALID_DESIGN_ROLES = {'INTRODUCE', 'DEVELOP', 'COMBINE', 'MASTERY'}
VALID_PICKUP_ROUTES = {'SAFE', 'RISK', 'SECRET'}
REQUIRED_SECRET_LEVELS = {3, 6, 9}
present_secret_ids = set()
VALID_PLATFORM_MATERIALS = {'STONE', 'SAND', 'ICE'}
REQUIRED_ENVIRONMENT_FIELDS = {'windZones', 'heatZones', 'icicleSpawners'}
REQUIRED_PHASE4_ARCHETYPES = {5, 6, 7}
present_enemy_kinds = set()
REQUIRED_DESIGN_FIELDS = {
    'role',
    'primaryMechanic',
    'playerBrief',
    'safePathIntent',
    'riskPathIntent',
    'restBeatIntent',
}
EXPECTED_DESIGN_ROLES = {
    1: 'INTRODUCE', 2: 'DEVELOP', 3: 'COMBINE', 4: 'MASTERY',
    5: 'INTRODUCE', 6: 'DEVELOP', 7: 'MASTERY',
    8: 'INTRODUCE', 9: 'COMBINE', 10: 'MASTERY',
}

for level_id in range(1, 11):
    path = ROOT / f'level_{level_id}.json'
    if not path.exists():
        raise SystemExit(f'Missing level asset: {path.name}')
    data = json.loads(path.read_text(encoding='utf-8'))
    if data.get('id') != level_id:
        raise SystemExit(f'Level id mismatch in {path.name}')
    if not data.get('stageName') or data.get('story') not in VALID_STORIES:
        raise SystemExit(f'Invalid metadata in {path.name}')
    design = data.get('design')
    if not isinstance(design, dict) or set(design) != REQUIRED_DESIGN_FIELDS:
        raise SystemExit(f'Invalid design contract in {path.name}')
    if design['role'] not in VALID_DESIGN_ROLES:
        raise SystemExit(f'Invalid design role in {path.name}')
    if design['role'] != EXPECTED_DESIGN_ROLES[level_id]:
        raise SystemExit(f'Unexpected curriculum role in {path.name}')
    if any(not isinstance(design[field], str) or not design[field].strip() for field in REQUIRED_DESIGN_FIELDS - {'role'}):
        raise SystemExit(f'Incomplete design text in {path.name}')
    checkpoint = data.get('checkpoint', {})
    if not 0 <= checkpoint.get('x', -1) <= 2300 or not 0 <= checkpoint.get('y', -1) <= 720:
        raise SystemExit(f'Invalid checkpoint in {path.name}')
    if not data.get('platforms'):
        raise SystemExit(f'No platforms in {path.name}')
    for platform in data['platforms']:
        if platform['width'] <= 0 or not 0 <= platform['x'] <= 2300 or not 0 <= platform['y'] <= 720:
            raise SystemExit(f'Invalid platform in {path.name}: {platform}')
        if platform.get('material') not in VALID_PLATFORM_MATERIALS:
            raise SystemExit(f'Invalid platform material in {path.name}: {platform}')
        if not all(isinstance(platform.get(key), (int, float)) for key in ('moveX', 'moveY', 'moveSpeed')):
            raise SystemExit(f'Invalid platform movement metadata in {path.name}: {platform}')
    pickup_routes = set()
    for pickup in data.get('pickups', []):
        if not 0 <= pickup['x'] <= 2300 or not 0 <= pickup['y'] <= 720:
            raise SystemExit(f'Invalid pickup in {path.name}: {pickup}')
        if pickup.get('route') not in VALID_PICKUP_ROUTES:
            raise SystemExit(f'Invalid pickup route in {path.name}: {pickup}')
        pickup_routes.add(pickup['route'])
    if not {'SAFE', 'RISK'}.issubset(pickup_routes):
        raise SystemExit(f'Missing SAFE or RISK pickup route in {path.name}')
    secrets = data.get('secrets')
    if not isinstance(secrets, list):
        raise SystemExit(f'Missing secrets contract in {path.name}')
    if level_id in REQUIRED_SECRET_LEVELS and len(secrets) != 1:
        raise SystemExit(f'Missing authored secret in {path.name}')
    if level_id not in REQUIRED_SECRET_LEVELS and secrets:
        raise SystemExit(f'Unexpected secret in {path.name}')
    for secret in secrets:
        if not secret.get('id') or secret['id'] in present_secret_ids or secret.get('rewardGems') != 2:
            raise SystemExit(f'Invalid secret metadata in {path.name}: {secret}')
        present_secret_ids.add(secret['id'])
    environment = data.get('environment')
    if not isinstance(environment, dict) or set(environment) != REQUIRED_ENVIRONMENT_FIELDS:
        raise SystemExit(f'Invalid environment contract in {path.name}')
    for zone in environment['windZones']:
        if not (0 <= zone['left'] < zone['right'] <= 2300 and 0 <= zone['top'] < zone['bottom'] <= 720 and -180 <= zone['forceX'] <= 180):
            raise SystemExit(f'Invalid wind zone in {path.name}: {zone}')
    for zone in environment['heatZones']:
        if not (0 <= zone['left'] < zone['right'] <= 2300 and 0 <= zone['top'] < zone['bottom'] <= 720):
            raise SystemExit(f'Invalid heat zone in {path.name}: {zone}')
    for spawner in environment['icicleSpawners']:
        if not (0 <= spawner['x'] <= 2300 and 0 <= spawner['spawnY'] < spawner['landingY'] <= 720 and 1.5 <= spawner['interval'] <= 6):
            raise SystemExit(f'Invalid icicle spawner in {path.name}: {spawner}')
    if level_id in {5, 6} and (not environment['windZones'] or not environment['heatZones']):
        raise SystemExit(f'Burning Dunes identity missing in {path.name}')
    if level_id in {8, 9} and (not environment['windZones'] or not environment['icicleSpawners']):
        raise SystemExit(f'Frozen Peaks identity missing in {path.name}')
    for foe in data.get('foes', []):
        if foe['kind'] not in set(range(8)) or not 0 <= foe['x'] <= 2300:
            raise SystemExit(f'Invalid foe in {path.name}: {foe}')
        present_enemy_kinds.add(foe['kind'])
    for hazard in data.get('hazards', []):
        if not (0 <= hazard['left'] < hazard['right'] <= 2300 and 0 <= hazard['top'] < hazard['bottom'] <= 720):
            raise SystemExit(f'Invalid hazard in {path.name}: {hazard}')
    has_boss = data.get('boss') is not None
    if has_boss != (level_id in BOSS_LEVELS):
        raise SystemExit(f'Boss placement mismatch in {path.name}')

if len(present_secret_ids) != len(REQUIRED_SECRET_LEVELS):
    raise SystemExit('Missing required Phase 8 secret caches in level data')

if not REQUIRED_PHASE4_ARCHETYPES.issubset(present_enemy_kinds):
    raise SystemExit('Missing required Phase 4 enemy archetypes in level data')

print('Level data test: OK')
print('Validated 10 JSON levels: metadata, curriculum, SAFE/RISK routes, platform materials, environment contracts, enemy archetypes, Phase 4 coverage, Phase 8 secrets, bounds, and boss placement.')
