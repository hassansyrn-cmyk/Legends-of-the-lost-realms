from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text(encoding="utf-8")

ENEMIES = (
    "enemy_moss_premium.png",
    "enemy_ember_moth_premium.png",
    "enemy_dune_premium.png",
    "enemy_frost_premium.png",
    "enemy_wisp_premium.png",
    "enemy_aegis_premium.png",
    "enemy_brute_premium.png",
    "enemy_caster_premium.png",
)

for name in ENEMIES:
    image = Image.open(RES / name).convert("RGBA")
    if image.size != (384, 384):
        raise SystemExit(f"{name}: expected 384x384, got {image.size}")
    alpha = image.getchannel("A")
    if alpha.getbbox() is None or alpha.getextrema()[0] != 0:
        raise SystemExit(f"{name}: expected visible art on a true transparent canvas")
    if sum(value > 0 for value in alpha.get_flattened_data()) < 5000:
        raise SystemExit(f"{name}: visible character area is unexpectedly small")

required = [
    "R.drawable.enemies_motion_sheet",
    "enemyFrame(e)",
    "drawImageTransform(c,enemySpriteSheet,enemyFrame(",
    "e.state==EnemyController.HIT_REACTION",
]
for marker in required:
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing premium enemy renderer marker: {marker}")

print("Premium enemy art: OK")
print("Validated eight source enemies and state-driven sprite-sheet routing.")