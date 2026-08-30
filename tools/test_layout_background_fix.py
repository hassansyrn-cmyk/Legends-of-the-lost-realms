from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text(encoding="utf-8")

for name in ("realm_verdant_premium.png", "realm_dunes_premium.png", "realm_frozen_premium.png"):
    if Image.open(RES / name).size != (1280, 720):
        raise SystemExit(f"{name}: expected 1280x720")

required_markers = [
    "realmBackgroundSheet != null",
    "float parallax = cameraX * .035f;",
    "drawPremiumPlatform(c, pl, x, bob);",
    "float surfaceY = rect.bottom;",
    "RectF trapDestination = new RectF(mid-half-30, surfaceY-164, mid+half+30, surfaceY+3);",
    "drawImageBottomScaled(c, worldSpriteSheet, worldFrame(trapRow, trapFrame), trapDestination, rise);",
    "float checkpointBaseY = checkpointMarkerY + 54f;",
    "worldFrame(3, checkpointFrame)",
    "effectFrame(0,",
]
for marker in required_markers:
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing premium layout marker: {marker}")

print("Premium layout routing: OK")
print("Verified new realm backgrounds, surface-anchored traps, platforms, and checkpoint shrine.")