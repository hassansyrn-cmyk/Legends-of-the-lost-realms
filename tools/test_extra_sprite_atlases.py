from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text(encoding="utf-8")

OPAQUE = {
    "realm_verdant_premium.png": (1280, 720),
    "realm_dunes_premium.png": (1280, 720),
    "realm_frozen_premium.png": (1280, 720),
    "realm_map_premium.png": (1280, 720),
}
TRANSPARENT = {
    "aster_premium.png": (512, 512),
    "checkpoint_premium.png": (384, 512),
    "platform_verdant_premium.png": (768, 320),
    "platform_dunes_premium.png": (768, 320),
    "platform_frozen_premium.png": (768, 320),
    "trap_verdant_premium.png": (384, 384),
    "trap_dunes_premium.png": (384, 384),
    "trap_frozen_premium.png": (384, 384),
    "attack_button_premium.png": (256, 256),
    "coin_premium.png": (256, 256),
    "gem_premium.png": (256, 256),
    "heart_premium.png": (128, 128),
    "heart_empty_premium.png": (128, 128),
    "shield_premium.png": (128, 128),
    "energy_premium.png": (128, 128),
}

for name, size in OPAQUE.items():
    image = Image.open(RES / name).convert("RGBA")
    if image.size != size or image.getchannel("A").getbbox() != (0, 0, *size):
        raise SystemExit(f"{name}: expected a complete opaque {size[0]}x{size[1]} environment")

for name, size in TRANSPARENT.items():
    image = Image.open(RES / name).convert("RGBA")
    alpha = image.getchannel("A")
    if image.size != size or alpha.getbbox() is None or alpha.getextrema()[0] != 0:
        raise SystemExit(f"{name}: expected visible art on a transparent {size[0]}x{size[1]} canvas")

for marker in (
    "R.drawable.realms_background_sheet",
    "R.drawable.platforms_motion_sheet",
    "R.drawable.world_motion_sheet",
    "drawPremiumPlatform(c, pl, x, bob);",
    "worldFrame(3, checkpointFrame)",
    "asterFrame(heroRow,heroFrame)",
):
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing premium asset routing marker: {marker}")

print("Premium world and UI art: OK")
print("Validated backgrounds, map, hero, platforms, traps, checkpoint, collectibles, and HUD icons.")