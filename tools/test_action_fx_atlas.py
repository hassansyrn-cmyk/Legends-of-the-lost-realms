from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text(encoding="utf-8")

effects = {
    "fx_slash_premium.png": (384, 384),
    "fx_dash_premium.png": (512, 256),
    "fx_sparkle_premium.png": (384, 384),
}
for name, size in effects.items():
    image = Image.open(RES / name).convert("RGBA")
    alpha = image.getchannel("A")
    if image.size != size or alpha.getbbox() is None or alpha.getextrema()[0] != 0:
        raise SystemExit(f"{name}: expected visible premium effect on transparent {size[0]}x{size[1]} canvas")

required_markers = [
    "R.drawable.effects_motion_sheet",
    "effectFrame(0,",
    "effectFrame(1,",
    "effectFrame(2,",
]
for marker in required_markers:
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing premium action FX renderer marker: {marker}")

for obsolete in ("R.drawable.action_fx_motion_atlas", "R.drawable.fx_trail", "R.drawable.super_hit_line"):
    if obsolete in GAME_VIEW:
        raise SystemExit(f"Obsolete action FX decode remains: {obsolete}")

print("Premium action effects: OK")
print("Validated standalone dash, sword-slash, impact, and collectible effects.")