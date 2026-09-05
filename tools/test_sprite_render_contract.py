"""Guard the runtime renderer against whole-sprite size and anchor drift."""

import json
import re
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text()
CONTRACT = json.loads((ROOT / "tools/sprite_sheet_contract.json").read_text())


def method_body(name, next_name):
    start = GAME_VIEW.index(name)
    end = GAME_VIEW.index(next_name, start)
    return GAME_VIEW[start:end]


player = method_body("private void drawPlayer", "private void drawEffects")
foes = method_body("private void drawFoes", "private void drawBoss")
boss = method_body("private void drawBoss", "private void drawSkeletalBoss")

# Actors must be routed through complete-frame bitmaps. Small whole-body
# transforms are permitted, but the modular hero renderer must never be active.
required_complete_calls = (
    "drawImageTransformAlpha(c, asterMotionSheet, asterFrame(row, frame), destination,",
    "drawImageTransformAlpha(c, enemyArt,",
    "drawImageTransformAlpha(c, bossArt, bossDestination, lean,",
)
for marker in required_complete_calls:
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing complete-frame actor renderer marker: {marker}")

if "drawModularHero(c, x, py + bob" in player:
    raise SystemExit("player: broken modular hero renderer is active")
for marker in ("float cellSize = 212f;", "top + 67f", "asterFrame(row, frame)"):
    if marker not in player:
        raise SystemExit(f"player: missing stable complete-frame footprint marker: {marker}")

frame_start = GAME_VIEW.index("private int enemyFrame")
frame_end = GAME_VIEW.index("private float supportingSurfaceY", frame_start)
frame_helpers = GAME_VIEW[frame_start:frame_end]
if "enemy.stateTime" in frame_helpers or "target.stateTime" in frame_helpers:
    raise SystemExit("state frame selection must not reverse when countdown timers decrease")
for marker in (
    "EnemyController.ATTACK)return 4 + frameIndex(animationClock,2",
    "EnemyController.PATROL)return 1 + frameIndex(animationClock,2",
    "State.ATTACK_EXECUTE)return 4 + frameIndex(animationClock,2",
    "State.ATTACK_WINDUP)return 3 + frameIndex(animationClock,2",
):
    if marker not in frame_helpers:
        raise SystemExit(f"Missing ordered state frame marker: {marker}")

if any("drawImageBottomScaled" in body for body in (player, foes, boss)):
    raise SystemExit("whole-image vertical scaling is not allowed for actors")
for marker in (
    "float surfaceY = supportingSurfaceY(",
    "RectF trapDestination = new RectF(mid-half-30, surfaceY-167, mid+half+30, surfaceY);",
    "new RectF(flag-48,checkpointBaseY-182,flag+84,checkpointBaseY)",
):
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing pixel-contact anchor marker: {marker}")


def bounds(path, cell_w, cell_h, rows, cols):
    image = Image.open(path).convert("RGBA")
    result = []
    for row in range(rows):
        row_boxes = []
        for column in range(cols):
            frame = image.crop(
                (column * cell_w, row * cell_h,
                 (column + 1) * cell_w, (row + 1) * cell_h)
            )
            box = frame.getchannel("A").getbbox()
            if box is None:
                raise SystemExit(f"{path.name}: empty frame at {row},{column}")
            row_boxes.append(box)
        result.append(row_boxes)
    return result


# The authored frames can change silhouette, but the overall footprint must
# stay stable enough that the actor does not appear to breathe in size.
for name in ("aster_motion_sheet", "enemies_motion_sheet", "bosses_motion_sheet"):
    spec = CONTRACT[name]
    for row, row_boxes in enumerate(
        bounds(RES / spec["file"], *spec["cell"], spec["rows"], spec["cols"])
    ):
        widths = [box[2] - box[0] for box in row_boxes]
        heights = [box[3] - box[1] for box in row_boxes]
        # Aster's attack row includes the full sword wind-up and extension,
        # while its destination rectangle and foot anchor remain fixed.
        allowed_width_drift = .45 if name == "aster_motion_sheet" and row == 3 else .24
        if (max(widths) - min(widths)) / max(widths) > allowed_width_drift:
            raise SystemExit(f"{name}: frame width drifts in row {row}: {widths}")
        if (max(heights) - min(heights)) / max(heights) > .24:
            raise SystemExit(f"{name}: frame height drifts in row {row}: {heights}")

print("Sprite render contract: OK")
print("Verified fixed actor footprints, ordered state loops, and surface contact compensation.")
