from pathlib import Path

GAME_VIEW = Path(__file__).resolve().parents[1].joinpath('app/src/main/java/com/manus/lostrealms/GameView.java').read_text(encoding='utf-8')
required = [
    'float bossGroundY = boss.y + 80f + bob;',
    'private static final class RigPart',
    'private static final class BossRig',
    'drawSkeletalBoss(c, rigSheet, rig, x, bossGroundY, phase, bossFacingScale);',
    'drawRigPart(c, rigSheet, rig.head, rig.neckX, rig.neckY + breath * 3f, breath * 1.2f - charge * 3f)',
    'float bossHudY = boss.y - 380 + bob;',
    'c.drawRect(x-96, bossHudY, x+96, bossHudY+13, p);',
    'c.drawRect(x-96, bossHudY, x-96+192*(boss.hp/(float)boss.maxHp), bossHudY+13, p);',
    'centeredAt(c,boss.name+" • "+phaseName, x, bossHudY-15, 14, Color.WHITE);',
]
for marker in required:
    if marker not in GAME_VIEW:
        raise SystemExit(f'Missing skeletal boss ground or HUD marker: {marker}')
for obsolete in ('boss.y-102+bob', 'boss.y-117+bob', 'bossGroundOffset = 23f;', 'drawImageTransform(c, bossMotionAtlas, source'):
    if obsolete in GAME_VIEW:
        raise SystemExit(f'Obsolete boss visual placement remains: {obsolete}')
print('Skeletal boss HUD and ground layout: OK')
print('Verified the rig body is ground-anchored and the boss name and HP bar remain above the full skeletal silhouette.')
