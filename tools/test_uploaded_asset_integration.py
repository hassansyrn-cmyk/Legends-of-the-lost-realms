from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
GAME_VIEW = (ROOT / "app/src/main/java/com/manus/lostrealms/GameView.java").read_text(encoding="utf-8")

premium_files = sorted(RES.glob("*_premium.png"))
if len(premium_files) < 34:
    raise SystemExit(f"Expected at least 34 integrated premium assets, found {len(premium_files)}")

raw_bytes = 0
for path in premium_files:
    image = Image.open(path).convert("RGBA")
    raw_bytes += image.width * image.height * 4
    if image.width > 1280 or image.height > 768:
        raise SystemExit(f"{path.name}: exceeds the mobile texture budget at {image.size}")
if raw_bytes > 45 * 1024 * 1024:
    raise SystemExit(f"Premium art raw bitmap budget is too high: {raw_bytes / 1024 / 1024:.1f} MB")

legacy_decodes = [
    "R.drawable.enemy_archetype_motion_atlas",
    "R.drawable.boss_motion_atlas",
    "R.drawable.boss_forest_rig_parts",
    "R.drawable.boss_stone_rig_parts",
    "R.drawable.boss_ice_rig_parts",
    "R.drawable.trap_platform_motion_sheet",
    "R.drawable.world_interactives_motion_atlas",
]
for marker in legacy_decodes:
    if marker in GAME_VIEW:
        raise SystemExit(f"Obsolete runtime decode remains: {marker}")

legacy_files = [
    "enemy_archetype_motion_atlas.png",
    "boss_motion_atlas.png",
    "action_fx_motion_atlas.png",
    "trap_platform_motion_sheet.png",
    "world_interactives_motion_atlas.png",
    "collectibles_fx_motion_atlas.png",
]
for name in legacy_files:
    if (RES / name).exists():
        raise SystemExit(f"Obsolete packaged resource remains: {name}")

for marker in (
    "VISUAL REBIRTH  •  BUILD 5.0.0",
    "R.drawable.realm_map_premium",
    "R.drawable.aster_motion_sheet",
    "R.drawable.enemies_motion_sheet",
    "R.drawable.bosses_motion_sheet",
    "R.drawable.realms_background_sheet",
    "R.drawable.platforms_motion_sheet",
    "R.drawable.world_motion_sheet",
    "R.drawable.collectibles_motion_sheet",
    "R.drawable.effects_motion_sheet",
    "R.drawable.ui_motion_sheet",
    "collectibleFrame(k.gem ? 1 : 0, frame)",
    "enemyFrame(e)",
    "bossFrame(boss)",
):
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing premium integration marker: {marker}")

print("Premium asset integration: OK")
print(f"Validated {len(premium_files)} assets with a {raw_bytes / 1024 / 1024:.1f} MB decoded bitmap budget.")