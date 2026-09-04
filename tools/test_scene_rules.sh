#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
PACKAGE_DIR="$TMP/com/manus/lostrealms"
mkdir -p "$PACKAGE_DIR"

cp "$ROOT/app/src/main/java/com/manus/lostrealms/EnvironmentRules.java" "$PACKAGE_DIR/"
cp "$ROOT/app/src/main/java/com/manus/lostrealms/EnemyController.java" "$PACKAGE_DIR/"
cp "$ROOT/app/src/main/java/com/manus/lostrealms/EnemySceneAdapter.java" "$PACKAGE_DIR/"

cat > "$PACKAGE_DIR/GameView.java" <<'EOF'
package com.manus.lostrealms;

final class GameView {
    void updateFoesInternal(float deltaSeconds) {}

    static class Foe {
        float x, y, baseY, targetX, targetY, minX, maxX, dir = 1, speed, stateTime;
        int kind, state;
        boolean didAttack;
        EnemyController.Archetype behavior;
    }
}
EOF

cat > "$PACKAGE_DIR/SceneRulesHarness.java" <<'EOF'
package com.manus.lostrealms;

public final class SceneRulesHarness {
    private static void check(boolean ok, String message) {
        if (!ok) throw new AssertionError(message);
    }

    private static GameView.Foe foe(int kind) {
        GameView.Foe foe = new GameView.Foe();
        foe.kind = kind;
        foe.behavior = EnemyController.archetype(kind);
        foe.x = foe.baseY = foe.y = 100;
        foe.minX = 20;
        foe.maxX = 180;
        foe.speed = foe.behavior.patrolSpeed;
        foe.state = EnemyController.PATROL;
        return foe;
    }

    public static void main(String[] args) {
        EnvironmentRules.PlatformMotion first = EnvironmentRules.movingPlatform(
                100, 300, 40, 18, 1.5f, 0, 100, 300);
        EnvironmentRules.PlatformMotion second = EnvironmentRules.movingPlatform(
                100, 300, 40, 18, 1.5f, 1, first.x, first.y);
        check(Math.abs(first.x - (100f + 40f * (float)Math.sin(1f))) < .001f,
                "platform preserves the established phase offset");
        check(Math.abs(second.deltaX) > 0f && Math.abs(second.deltaY) > 0f,
                "platform reports rider delta");
        check(EnvironmentRules.spawnerDue(.02f, .03f), "spawner due");
        check(EnvironmentRules.nextSpawnerTimer(.02f, .03f, .7f) > 0f,
                "spawner timer wraps");
        EnvironmentRules.IcicleMotion icicle =
                EnvironmentRules.fallingIcicle(50, 0, .1f, 300);
        check(icicle.velocityY > 0 && icicle.y > 50, "icicle accelerates");
        check(EnvironmentRules.fallingIcicle(299, 980, .1f, 300).landed,
                "icicle landing is deterministic");
        check(EnvironmentRules.advanceAge(.2f, .3f) == .5f, "hazard age advances");
        check(EnvironmentRules.hazardExpired(1f, 1f), "hazard expiry");

        final int[] warnings = {0};
        final int[] damages = {0};
        final int[] projectiles = {0};
        EnemySceneAdapter adapter = new EnemySceneAdapter(new EnemySceneAdapter.Hooks() {
            public float playerX() { return 130; }
            public float playerY() { return 100; }
            public void warning() { warnings[0]++; }
            public void dash() {}
            public void swoop() {}
            public void damage(int amount) { damages[0] += amount; }
            public void projectile(float x, float y, float vx, float vy) { projectiles[0]++; }
        });
        GameView.Foe ground = foe(EnemyController.GROUND_PATROLLER);
        adapter.updateBehavior(ground, .01f, 0, 1);
        check(ground.state == EnemyController.NOTICE && warnings[0] == 1,
                "enemy enters notice through adapter");
        ground.stateTime = 0;
        adapter.updateBehavior(ground, .01f, 0, 1);
        check(ground.state == EnemyController.WINDUP, "notice transitions to windup");
        ground.stateTime = 0;
        adapter.updateBehavior(ground, .01f, 0, 1);
        check(ground.state == EnemyController.ATTACK, "windup transitions to attack");
        GameView.Foe caster = foe(EnemyController.RUNE_CASTER);
        caster.state = EnemyController.ATTACK;
        caster.stateTime = .1f;
        caster.targetX = 130;
        caster.targetY = 100;
        adapter.updateBehavior(caster, .01f, 0, 1);
        check(projectiles[0] == 1 && caster.didAttack, "caster fires once");
        System.out.println("Scene rule extraction checks: OK");
    }
}
EOF

javac -d "$TMP/out" "$PACKAGE_DIR"/*.java
if ! SCENE_OUTPUT="$(java -cp "$TMP/out" com.manus.lostrealms.SceneRulesHarness 2>&1)"; then
    printf '%s\n' "$SCENE_OUTPUT"
    FAILURE="$(printf '%s\n' "$SCENE_OUTPUT" | grep -m1 'AssertionError' || true)"
    echo "::error title=Scene rules failed::${FAILURE:-SceneRulesHarness exited unsuccessfully}"
    exit 1
fi
printf '%s\n' "$SCENE_OUTPUT"
