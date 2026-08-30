"""Conservative, deterministic reachability checks for authored level data.

This is intentionally an envelope simulation rather than a replacement for the
runtime collision loop.  Its constants mirror GameView's player collider,
jump impulses, gravity, run speed, platform sine motion, and crumble life.
"""
import json
import math
from collections import deque
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ROOT = PROJECT_ROOT / 'app/src/main/assets/levels'
# GameView spawns at x=120 and completes non-boss levels only after px > 2145.
SPAWN_X, GOAL_X = 120, 2145
# GameView.playerRect() is 36 x 54.  Platform landing deliberately uses the
# wider +/- 26 collision test, so it is kept separate from damage occupancy.
PLAYER_BODY_HALF_WIDTH, PLAYER_BODY_HEIGHT = 18, 54
PLAYER_LANDING_HALF_WIDTH = 26
RUN_SPEED, GRAVITY = 310, 1450
FIRST_JUMP, DOUBLE_JUMP = 680, 635
DOUBLE_JUMP_RISE = (FIRST_JUMP ** 2 + DOUBLE_JUMP ** 2) / (2 * GRAVITY)
MAX_RISE = int(DOUBLE_JUMP_RISE) - 4
MAX_DROP = 420
MAX_AIR_TIME = FIRST_JUMP / GRAVITY + 2 * DOUBLE_JUMP / GRAVITY
PHASE_SECONDS, PHASE_COUNT = .25, 48
CRUMBLE_LIFE = 2.6
ICICLE_HALF_WIDTH = 10


def platform_at(platform, phase):
    """Exact sine position used by Platform.update(), sampled over 12 seconds."""
    clock = phase * PHASE_SECONDS
    wave = math.sin(clock * platform['moveSpeed'] + platform['x'] * .01) if platform['moveSpeed'] else 0
    return platform['x'] + platform['moveX'] * wave, platform['y'] + platform['moveY'] * wave


def vertical_overlap(platform_y, rect):
    # Player standing on this platform occupies [y - 54, y].
    return platform_y >= rect['top'] and platform_y - PLAYER_BODY_HEIGHT <= rect['bottom']


def blocked_intervals(data, platform, phase):
    x, y = platform_at(platform, phase)
    intervals = []
    for rect in data['hazards'] + data['environment']['heatZones']:
        if vertical_overlap(y, rect):
            intervals.append((rect['left'] - PLAYER_BODY_HALF_WIDTH, rect['right'] + PLAYER_BODY_HALF_WIDTH))
    return intervals


def segments(data, platform, phase):
    """Landing intervals that do not overlap static hazards or heat."""
    x, _ = platform_at(platform, phase)
    # This matches GameView's px +/- 26 landing overlap rather than requiring
    # a character center to stay inside visual platform bounds.
    parts = [(x - PLAYER_LANDING_HALF_WIDTH, x + platform['width'] + PLAYER_LANDING_HALF_WIDTH)]
    for left, right in blocked_intervals(data, platform, phase):
        replacement = []
        for part_left, part_right in parts:
            if right <= part_left or left >= part_right:
                replacement.append((part_left, part_right))
            else:
                if part_left < left:
                    replacement.append((part_left, left))
                if right < part_right:
                    replacement.append((right, part_right))
        parts = replacement
    return [(left, right) for left, right in parts if right - left >= 8]


def stable(platform):
    return not platform['crumble'] and platform['moveX'] == 0 and platform['moveY'] == 0


def flight_time(source_y, target_y):
    # Full double-jump airtime is conservative for same-height/lower targets.
    # Higher targets necessarily shorten the landing window.
    rise = source_y - target_y
    return max(.35, MAX_AIR_TIME - max(0, rise) / 600)


def wind_displacement(data, left, right, y, direction, duration):
    """Worst directed acceleration from every wind zone crossed by the jump."""
    force = 0
    for zone in data['environment']['windZones']:
        if right >= zone['left'] and left <= zone['right'] and zone['top'] <= y <= zone['bottom']:
            force += zone['forceX'] * direction
    return .5 * force * duration * duration


def directed_gap(source, target):
    if target[0] >= source[1]:
        return target[0] - source[1], 1
    if source[0] >= target[1]:
        return source[0] - target[1], -1
    return 0, 0


def can_jump(data, source_platform, source_segment, phase, target_platform, target_segment, arrival_phase):
    source_x, source_y = platform_at(source_platform, phase)
    _, target_y = platform_at(target_platform, arrival_phase)
    rise = source_y - target_y
    if rise > MAX_RISE or rise < -MAX_DROP:
        return False
    gap, direction = directed_gap(source_segment, target_segment)
    if direction == 0:
        return True
    duration = flight_time(source_y, target_y)
    # Run speed is the actual base horizontal velocity.  Wind is evaluated in
    # the travel direction, so adverse zones reduce the directed envelope.
    reach = RUN_SPEED * duration + wind_displacement(
        data, min(source_x, target_segment[0]), max(source_x + source_platform['width'], target_segment[1]),
        source_y - PLAYER_BODY_HEIGHT / 2, direction, duration)
    return gap <= max(0, reach)


def phase_advance(duration):
    return max(1, math.ceil(duration / PHASE_SECONDS))


def build_graph(data, allowed):
    platforms = data['platforms']
    nodes = {
        (index, phase, segment_index): segment
        for index, platform in enumerate(platforms) if index in allowed
        for phase in range(PHASE_COUNT)
        for segment_index, segment in enumerate(segments(data, platform, phase))
    }
    edges = {node: [] for node in nodes}
    for node, source_segment in nodes.items():
        source_index, phase, _ = node
        source = platforms[source_index]
        next_phase = (phase + 1) % PHASE_COUNT
        # Waiting is permitted only on a non-crumbling platform.  A moving
        # platform carries its rider to the next sampled runtime position.
        if not source['crumble']:
            for next_segment_index, next_segment in enumerate(segments(data, source, next_phase)):
                if max(source_segment[0], next_segment[0]) <= min(source_segment[1], next_segment[1]):
                    candidate = (source_index, next_phase, next_segment_index)
                    if candidate in nodes:
                        edges[node].append(candidate)
        for target_index, target in enumerate(platforms):
            if target_index not in allowed or target_index == source_index:
                continue
            duration = flight_time(platform_at(source, phase)[1], platform_at(target, phase)[1])
            arrival_phase = (phase + phase_advance(duration)) % PHASE_COUNT
            for target_segment_index, target_segment in enumerate(segments(data, target, arrival_phase)):
                candidate = (target_index, arrival_phase, target_segment_index)
                if candidate in nodes and can_jump(data, source, source_segment, phase, target, target_segment, arrival_phase):
                    # A crumble platform can be used for one immediate transfer;
                    # its 2.6 s runtime life exceeds the modeled flight time.
                    if not source['crumble'] or duration < CRUMBLE_LIFE:
                        edges[node].append(candidate)
    return nodes, edges


def bfs(edges, starts):
    queue, reached = deque(starts), set(starts)
    while queue:
        node = queue.popleft()
        for other in edges[node]:
            if other not in reached:
                reached.add(other)
                queue.append(other)
    return reached


def node_supports(nodes, node, x, y, tolerance=12, horizontal_tolerance=0):
    platform_index, _, _ = node
    segment = nodes[node]
    platform = CURRENT_PLATFORMS[platform_index]
    _, platform_y = platform_at(platform, node[1])
    return (
        segment[0] - horizontal_tolerance <= x <= segment[1] + horizontal_tolerance
        and abs((platform_y - PLAYER_BODY_HEIGHT) - y) <= PLAYER_BODY_HEIGHT + tolerance
    )


def pickup_reachable(nodes, reached, pickup):
    for node in reached:
        platform_index, phase, _ = node
        platform = CURRENT_PLATFORMS[platform_index]
        segment = nodes[node]
        _, platform_y = platform_at(platform, phase)
        if pickup['x'] < segment[0] - RUN_SPEED * MAX_AIR_TIME or pickup['x'] > segment[1] + RUN_SPEED * MAX_AIR_TIME:
            continue
        if -80 <= platform_y - pickup['y'] <= MAX_RISE + 20:
            return True
    return False


def check_icicle_timing(data, nodes, safe_reached):
    """Icicles are periodic, not permanent walls; verify timed crossing space."""
    for index, spawner in enumerate(data['environment']['icicleSpawners']):
        if spawner['interval'] <= .25 + (2 * (PLAYER_BODY_HALF_WIDTH + ICICLE_HALF_WIDTH)) / RUN_SPEED:
            raise SystemExit(f'Level {data["id"]}: icicle lane {index} has no deterministic crossing window')
        safe_left = safe_right = False
        for node in safe_reached:
            segment = nodes[node]
            if segment[1] < spawner['x'] - PLAYER_BODY_HALF_WIDTH - ICICLE_HALF_WIDTH:
                safe_left = True
            if segment[0] > spawner['x'] + PLAYER_BODY_HALF_WIDTH + ICICLE_HALF_WIDTH:
                safe_right = True
        if not (safe_left and safe_right):
            raise SystemExit(f'Level {data["id"]}: icicle lane {index} lacks stable SAFE staging on both sides')


for level_id in range(1, 11):
    data = json.loads((ROOT / f'level_{level_id}.json').read_text(encoding='utf-8'))
    CURRENT_PLATFORMS = data['platforms']
    all_allowed = set(range(len(CURRENT_PLATFORMS)))
    nodes, edges = build_graph(data, all_allowed)
    starts = [node for node in nodes if node_supports(nodes, node, SPAWN_X, 566)]
    if not starts:
        raise SystemExit(f'Level {level_id}: spawn has no hazard-free supporting surface')
    reached = bfs(edges, starts)
    reached_platforms = {node[0] for node in reached}
    missing = sorted(all_allowed - reached_platforms)
    if missing:
        raise SystemExit(f'Level {level_id}: unreachable authored platforms {missing}')

    # Every crumble shelf must be entered and escaped in its 2.6-second life.
    for platform_index, platform in enumerate(CURRENT_PLATFORMS):
        if platform['crumble']:
            crumble_nodes = [node for node in reached if node[0] == platform_index]
            if not crumble_nodes or not any(edges[node] for node in crumble_nodes):
                raise SystemExit(f'Level {level_id}: crumble platform {platform_index} has no timed escape')

    stable_allowed = {index for index, platform in enumerate(CURRENT_PLATFORMS) if stable(platform)}
    safe_nodes, safe_edges = build_graph(data, stable_allowed)
    safe_starts = [node for node in safe_nodes if node_supports(safe_nodes, node, SPAWN_X, 566)]
    safe_reached = bfs(safe_edges, safe_starts)
    checkpoint = data['checkpoint']
    checkpoint_nodes = [node for node in safe_reached if node_supports(safe_nodes, node, checkpoint['x'], checkpoint['y'], 70)]
    if not checkpoint_nodes:
        raise SystemExit(f'Level {level_id}: no stable SAFE route reaches the checkpoint')
    # Continue from an actually reached checkpoint state: this forbids a graph
    # that happens to reach both areas only by mutually exclusive branches.
    after_checkpoint = bfs(safe_edges, checkpoint_nodes)
    goal_nodes = [node for node in after_checkpoint if safe_nodes[node][1] >= GOAL_X]
    if not goal_nodes:
        raise SystemExit(f'Level {level_id}: no stable SAFE route reaches the goal zone')

    for pickup_index, pickup in enumerate(data.get('pickups', [])):
        if not pickup_reachable(nodes, reached, pickup):
            raise SystemExit(f'Level {level_id}: pickup {pickup_index} ({pickup["route"]}) is unreachable')
        if pickup['route'] == 'SAFE' and not pickup_reachable(safe_nodes, safe_reached, pickup):
            raise SystemExit(f'Level {level_id}: SAFE pickup {pickup_index} lacks a stable route')

    for foe_index, foe in enumerate(data.get('foes', [])):
        if not any(node_supports(nodes, node, foe['x'], foe['y'], 50, 30) for node in reached):
            raise SystemExit(f'Level {level_id}: foe {foe_index} lacks a reachable combat surface')
    if data.get('boss') and not any(node_supports(safe_nodes, node, data['boss']['x'], data['boss']['y'], 90) for node in safe_reached):
        raise SystemExit(f'Level {level_id}: boss access lacks a stable SAFE route')

    secret_ids = {secret['id'] for secret in data.get('secrets', [])}
    reached_secret_ids = {
        pickup.get('secretId') for pickup in data.get('pickups', [])
        if pickup.get('route') == 'SECRET' and pickup_reachable(nodes, reached, pickup)
    }
    if secret_ids != reached_secret_ids:
        raise SystemExit(f'Level {level_id}: secret route does not resolve to a reachable reward')
    check_icicle_timing(data, safe_nodes, safe_reached)

print('Deterministic level-bound simulation: OK')
print('Validated player collider, double-jump rise, directed wind-adjusted jumps, 48 moving-platform phases, rider carry, crumble exits, static/heat occupancy, icicle timing, SAFE checkpoint-to-goal routes, collectibles, secrets, combat surfaces, and boss access.')