import json
from pathlib import Path

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
CONTRACT = json.loads((ROOT / "tools/sprite_sheet_contract.json").read_text())
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text()


def alpha_bounds(frame):
    return frame.getchannel("A").getbbox()


def silhouette_delta(left, right):
    left_alpha = left.getchannel("A").point(lambda value: 255 if value >= 48 else 0)
    right_alpha = right.getchannel("A").point(lambda value: 255 if value >= 48 else 0)
    changed = ImageChops.difference(left_alpha, right_alpha)
    return (left.width * left.height - changed.histogram()[0]) / (left.width * left.height)


for name, spec in CONTRACT.items():
    image = Image.open(RES / spec["file"]).convert("RGBA")
    cell_w, cell_h = spec["cell"]
    expected = (cell_w * spec["cols"], cell_h * spec["rows"])
    if image.size != expected:
        raise SystemExit(f"{name}: expected {expected}, found {image.size}")

    row_bounds = []
    for row in range(spec["rows"]):
        bounds = []
        for column in range(spec["cols"]):
            frame = image.crop((column * cell_w, row * cell_h, (column + 1) * cell_w, (row + 1) * cell_h))
            box = alpha_bounds(frame)
            if box is None:
                raise SystemExit(f"{name}: empty frame at row {row}, column {column}")
            if name != "realms_background_sheet" and (
                box[0] == 0 or box[1] == 0 or box[2] == cell_w or box[3] == cell_h
            ):
                raise SystemExit(f"{name}: frame touches cell edge at row {row}, column {column}")
            bounds.append(box)
        row_bounds.append(bounds)

    # Bottom anchors may move by only a small fraction of a cell inside a loop.
    # This catches the foot/base jitter that makes otherwise smooth animation vibrate.
    if name not in ("realms_background_sheet", "ui_motion_sheet"):
        for row, bounds in enumerate(row_bounds):
            bottoms = [box[3] for box in bounds]
            if max(bottoms) - min(bottoms) > max(8, int(cell_h * .09)):
                raise SystemExit(f"{name}: unstable bottom anchor in row {row}: {bottoms}")

    # Every animated row must change its visible silhouette. A color twinkle or
    # a one-pixel translation is not a readable character pose.
    if spec["cols"] > 1:
        for row in range(spec["rows"]):
            frames = [
                image.crop((column * cell_w, row * cell_h, (column + 1) * cell_w, (row + 1) * cell_h))
                for column in range(spec["cols"])
            ]
            strongest_change = max(
                silhouette_delta(frames[index], frames[index + 1])
                for index in range(len(frames) - 1)
            )
            threshold = .001 if name in ("platforms_motion_sheet", "collectibles_motion_sheet", "effects_motion_sheet") else .0025
            if strongest_change < threshold:
                raise SystemExit(
                    f"{name}: row {row} lacks a readable pose change ({strongest_change:.4f})"
                )

    # State-aware actor sheets must declare the exact contiguous column ranges
    # consumed by GameView. This prevents a future repack from silently turning
    # walk/attack/jump back into an unlabeled generic loop.
    if name in (
        "aster_motion_sheet",
        "enemies_motion_sheet",
        "bosses_motion_sheet",
        "world_motion_sheet",
    ):
        ranges = spec.get("state_ranges")
        if not ranges:
            raise SystemExit(f"{name}: missing named state_ranges")
        for state, span in ranges.items():
            if (
                not isinstance(span, list)
                or len(span) != 2
                or not all(isinstance(value, int) for value in span)
                or span[0] < 0
                or span[1] <= span[0]
                or span[1] > spec["cols"]
            ):
                raise SystemExit(f"{name}: invalid {state} frame range {span}")

for name in ("aster_motion_sheet", "enemies_motion_sheet", "bosses_motion_sheet", "world_motion_sheet"):
    if f'rebuild_character_sheet("{name}"' in GAME_VIEW:
        raise SystemExit(f"{name}: runtime must not procedurally fake complete-frame movement")

for marker in (
    "frameIndex(animationClock",
    "timedFrame(",
    "asterFrame(",
    "enemyFrame(",
    "bossFrame(",
    "worldFrame(",
    "collectibleFrame(",
    "effectFrame(",
):
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing runtime frame-selection marker: {marker}")

print(f"Sprite sheet contract: OK ({len(CONTRACT)} sheets)")