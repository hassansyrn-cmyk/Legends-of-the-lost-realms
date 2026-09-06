from pathlib import Path

GAME_VIEW = Path(__file__).resolve().parents[1].joinpath(
    "app/src/main/java/com/manus/lostrealms/GameView.java"
).read_text(encoding="utf-8")

required = [
    "float bossGroundY = boss.y + 80f + bob;",
    "float bossWidth=boss.world==3?330f:300f;",
    "RectF bossDestination=new RectF(x-bossWidth/2,bossGroundY-bossHeight,x+bossWidth/2,bossGroundY);",
    "drawImageTransformAlpha(c,forestElementalBossSheet,bossFrame(action,frame),",
    "float bossHudY = boss.y - 380 + bob;",
    "c.drawRect(x-96, bossHudY, x+96, bossHudY+13, p);",
    "centeredAt(c,boss.name+\" • \"+phaseName, x, bossHudY-15, 14, Color.WHITE);",
]
for marker in required:
    if marker not in GAME_VIEW:
        raise SystemExit(f"Missing premium boss ground or HUD marker: {marker}")

print("Rigged boss HUD and ground layout: OK")
print("Verified the bottom-anchored forest rig and elevated boss label and health bar.")
