import json
from pathlib import Path

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
CONTRACT = json.loads((ROOT / "tools/sprite_sheet_contract.json").read_text())
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text()


def alpha_bounds(frame):
    return frame.getchannel("A").getbbox()


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

    # Every animated row must contain actual frame variation.
    if spec["cols"] > 1:
        for row in range(spec["rows"]):
            first = image.crop((0, row * cell_h, cell_w, (row + 1) * cell_h))
            changed = False
            for column in range(1, spec["cols"]):
                frame = image.crop((column * cell_w, row * cell_h, (column + 1) * cell_w, (row + 1) * cell_h))
                if ImageChops.difference(first, frame).getbbox():
                    changed = True
                    break
            if not changed:
                raise SystemExit(f"{name}: row {row} repeats one static frame")

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