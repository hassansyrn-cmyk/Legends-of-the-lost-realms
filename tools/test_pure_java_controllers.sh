#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

PACKAGE_DIR="$TMP/com/manus/lostrealms"
mkdir -p "$PACKAGE_DIR"
cp "$ROOT/app/src/main/java/com/manus/lostrealms/BossController.java" "$PACKAGE_DIR/"
cp "$ROOT/app/src/main/java/com/manus/lostrealms/EnemyController.java" "$PACKAGE_DIR/"

cat > "$PACKAGE_DIR/GameView.java" <<'EOF'
package com.manus.lostrealms;

final class GameView {
    void updateBossInternal(float deltaSeconds) {}
    void updateFoesInternal(float deltaSeconds) {}
}
EOF

cat > "$PACKAGE_DIR/ControllerHarness.java" <<'EOF'
package com.manus.lostrealms;

public final class ControllerHarness {
    private static void check(boolean condition, String message) {
        if (!condition) throw new AssertionError(message);
    }

    public static void main(String[] args) {
        check(BossController.phaseForHealth(24, 24, 1) == 1, "boss phase one");
        check(BossController.phaseForHealth(14, 24, 1) == 2, "boss phase two");
        check(BossController.phaseForHealth(6, 24, 1) == 3, "boss phase three");
        check(BossController.chooseAttack(
                1, 1, 420f, 0f, 0f, false, null, 0) == BossController.Attack.ROOT_WALL,
                "Thornwold should use lane control at range");
        check(BossController.chooseAttack(
                3, 3, 180f, 0f, 0f, true, null, 0) == BossController.Attack.WHITEOUT,
                "Vyrn should punish repeated dodging in phase three");
        check(BossController.chooseAttack(
                2, 2, 120f, 0f, 0f, false, BossController.Attack.STONE_SLAM, 1)
                != BossController.Attack.STONE_SLAM,
                "boss attacks should not repeat immediately");
        check(BossController.comboFollowUp(
                1, 1, BossController.Attack.ROOT_STRIKE, 0) == null,
                "phase one should not combo");
        check(BossController.comboFollowUp(
                2, 3, BossController.Attack.STONE_SLAM, 2) != BossController.Attack.STONE_SLAM,
                "boss combo follow-up should change attack");
        check(BossController.powerReaction(1, 0) == BossController.PowerReaction.BURN_EXPOSED,
                "forest boss should react to ember");
        check(BossController.powerReaction(2, 1) == BossController.PowerReaction.FROST_CRACKED,
                "stone boss should react to frost");
        check(BossController.powerReaction(3, 0) == BossController.PowerReaction.MELT_INTERRUPTED,
                "ice boss should react to ember");

        for (int kind = EnemyController.GROUND_PATROLLER;
             kind <= EnemyController.RUNE_CASTER; kind++) {
            EnemyController.Archetype behavior = EnemyController.archetype(kind);
            check(behavior.windupSeconds >= .30f, "enemy telegraph " + kind);
            check(EnemyController.canNotice(kind, 0f, 0f), "enemy detection " + kind);
            check(!EnemyController.canNotice(kind, behavior.noticeRange + 1f, 0f),
                    "enemy range boundary " + kind);
        }
        check(EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, EnemyController.ATTACK) == 2,
                "committed brute hit");
        check(EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, EnemyController.RECOVERY) == 1,
                "recovering brute contact");
        check(EnemyController.scaledHealth(EnemyController.HEAVY_BRUTE, 10)
                > EnemyController.scaledHealth(EnemyController.HEAVY_BRUTE, 1),
                "enemy health scales by level");

        System.out.println("Pure Java combat controller checks: OK");
    }
}
EOF

javac -d "$TMP/out" "$PACKAGE_DIR"/*.java
java -cp "$TMP/out" com.manus.lostrealms.ControllerHarness