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

        for (int kind = EnemyController.GROUND_PATROLLER;
             kind <= EnemyController.RUNE_CASTER; kind++) {
            EnemyController.Archetype behavior = EnemyController.archetype(kind);
            check(behavior.windupSeconds >= .30f, "enemy telegraph " + kind);
            check(EnemyController.canNotice(kind, 0f, 0f), "enemy detection " + kind);
            check(!EnemyController.canNotice(kind, behavior.noticeRange + 1f, 0f),
                    "enemy range boundary " + kind);
        }
        check(EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, 2) == 2,
                "committed brute hit");
        check(EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, 3) == 1,
                "recovering brute contact");

        System.out.println("Pure Java combat controller checks: OK");
    }
}
EOF

javac -d "$TMP/out" "$PACKAGE_DIR"/*.java
java -cp "$TMP/out" com.manus.lostrealms.ControllerHarness