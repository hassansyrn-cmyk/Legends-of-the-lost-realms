from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text(encoding="utf-8")

for name in (
    "boss_heartwood_premium.png",
    "boss_sunscar_premium.png",
    "boss_whiteout_premium.png",
):
    image = Image.open(RES / name).convert("RGBA")
    if image.size != (768, 768):
        raise SystemExit(f"{name}: expected 768x768, got {image.size}")
    alpha = image.getchannel("A")
    if alpha.getbbox() is None or alpha.getextrema()[0] != 0:
        raise SystemExit(f"{name}: expected a visible boss on a transparent canvas")
    if sum(value > 0 for value in alpha.getdata()) < 20000:
        raise SystemExit(f"{name}: boss silhouette is unexpectedly sparse")

for marker in (
    "bossPremiumSprites",
    "Bitmap bossArt = bossPremiumSprites",
    "drawImageTransformAlpha(c, bossArt,",
    "boss.state==BossController.State.ATTACK_EXECUTE",
):
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing premium boss renderer marker: {marker}")

print("Premium boss art: OK")
print("Validated three standalone realm bosses and state-driven rendering.")
