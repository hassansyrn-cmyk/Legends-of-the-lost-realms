package com.manus.lostrealms;

/**
 * Android-free enemy locomotion/state adapter. GameView retains combat effects,
 * rendering, audio, and the runtime collection.
 */
final class EnemySceneAdapter {
    interface Hooks {
        float playerX();
        float playerY();
        void warning();
        void dash();
        void swoop();
        void damage(int amount);
        void projectile(float x, float y, float vx, float vy);
    }

    private final Hooks hooks;

    EnemySceneAdapter(Hooks hooks) {
        this.hooks = hooks;
    }

    void updateBehavior(GameView.Foe e, float dt, float clock, int level) {
        float dx = hooks.playerX() - e.x;
        float timing = EnemyController.timingScale(level);
        if (e.state == EnemyController.HIT_REACTION) {
            e.x -= e.dir * 72f * dt;
            if ((e.stateTime -= dt) <= 0) state(e, EnemyController.RECOVERY, e.behavior.recoverySeconds * .65f);
            return;
        }
        if (e.state == EnemyController.PATROL) {
            if (e.kind == EnemyController.FLYING_SWOOOPER || e.kind == EnemyController.FROST_SENTINEL || e.kind == EnemyController.RUNE_CASTER) {
                float frequency = e.kind == EnemyController.FLYING_SWOOOPER ? 3.7f : 2.2f;
                e.y = e.baseY + (float)Math.sin(clock * frequency + e.x * .018f)
                        * (e.kind == EnemyController.FLYING_SWOOOPER ? 18f : 8f);
            } else {
                e.x += e.dir * e.speed * dt;
                if (e.x < e.minX || e.x > e.maxX) e.dir *= -1;
            }
            if (EnemyController.canNotice(e.kind, dx, hooks.playerY() - e.y)) {
                e.dir = dx < 0 ? -1 : 1;
                state(e, EnemyController.NOTICE, e.behavior.noticeSeconds * timing);
                hooks.warning();
            }
            return;
        }
        if (e.state == EnemyController.NOTICE) {
            e.dir = dx < 0 ? -1 : 1;
            if ((e.stateTime -= dt) <= 0) state(e, EnemyController.WINDUP, e.behavior.windupSeconds * timing);
            return;
        }
        if (e.state == EnemyController.WINDUP) {
            if (e.kind != EnemyController.FAST_SKIRMISHER) e.dir = dx < 0 ? -1 : 1;
            if ((e.stateTime -= dt) <= 0) {
                e.targetX = hooks.playerX();
                e.targetY = hooks.playerY() + 18f;
                e.didAttack = false;
                state(e, EnemyController.ATTACK, e.behavior.attackSeconds);
                if (e.kind == EnemyController.FLYING_SWOOOPER || e.kind == EnemyController.FROST_SENTINEL || e.kind == EnemyController.RUNE_CASTER) hooks.swoop();
                else hooks.dash();
            }
            return;
        }
        if (e.state == EnemyController.ATTACK) {
            attack(e, dt, level);
            if ((e.stateTime -= dt) <= 0) state(e, EnemyController.RECOVERY, e.behavior.recoverySeconds);
            return;
        }
        if (e.state == EnemyController.RECOVERY) {
            if ((e.stateTime -= dt) <= 0) state(e, EnemyController.REPOSITION, e.behavior.repositionSeconds);
            return;
        }
        float retreat = e.kind == EnemyController.FAST_SKIRMISHER ? 190f
                : e.kind == EnemyController.SHIELD_GUARD ? 82f
                : e.kind == EnemyController.HEAVY_BRUTE ? 55f : 120f;
        e.x -= e.dir * retreat * (e.kind == EnemyController.FLYING_SWOOOPER ? .45f : 1f) * dt;
        if (e.kind == EnemyController.FLYING_SWOOOPER || e.kind == EnemyController.WIND_WISP)
            e.y = approach(e.y, e.baseY, 150f * dt);
        if ((e.stateTime -= dt) <= 0) {
            e.x = Math.max(e.minX - 70, Math.min(e.maxX + 70, e.x));
            state(e, EnemyController.PATROL, 0);
        }
    }

    private void attack(GameView.Foe e, float dt, int level) {
        float speed = EnemyController.speedScale(level);
        if (e.kind == EnemyController.GROUND_PATROLLER) e.x += e.dir * 245f * speed * dt;
        else if (e.kind == EnemyController.FLYING_SWOOOPER) {
            e.x = approach(e.x, e.targetX, 300f * speed * dt);
            e.y = approach(e.y, e.targetY, 245f * speed * dt);
        } else if (e.kind == EnemyController.FAST_SKIRMISHER) e.x = approach(e.x, e.targetX, 390f * speed * dt);
        else if (e.kind == EnemyController.WIND_WISP) {
            e.x = approach(e.x, e.targetX, 430f * speed * dt);
            e.y = approach(e.y, e.targetY, 340f * speed * dt);
        } else if (e.kind == EnemyController.SHIELD_GUARD) e.x += e.dir * 138f * speed * dt;
        else if (e.kind == EnemyController.FROST_SENTINEL && !e.didAttack) {
            if (Math.abs(hooks.playerX() - e.x) < 115 && Math.abs(hooks.playerY() - e.y) < 90) hooks.damage(2);
            e.didAttack = true;
        } else if (e.kind == EnemyController.RUNE_CASTER && !e.didAttack) {
            float dx = e.targetX - e.x, dy = e.targetY - e.y;
            float length = Math.max(1, (float)Math.sqrt(dx * dx + dy * dy));
            hooks.projectile(e.x, e.y - 18, dx / length * 265f * speed, dy / length * 265f * speed);
            e.didAttack = true;
        }
    }

    private static void state(GameView.Foe e, int state, float duration) {
        e.state = state;
        e.stateTime = duration;
    }

    private static float approach(float value, float target, float amount) {
        return value < target ? Math.min(target, value + amount)
                : value > target ? Math.max(target, value - amount) : target;
    }
}