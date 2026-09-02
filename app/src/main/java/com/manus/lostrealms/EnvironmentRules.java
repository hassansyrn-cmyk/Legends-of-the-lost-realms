package com.manus.lostrealms;

/**
 * Deterministic, Android-free rules for moving surfaces and timed hazards.
 * Rendering and collision ownership remain in GameView.
 */
final class EnvironmentRules {
    static final class PlatformMotion {
        final float x;
        final float y;
        final float deltaX;
        final float deltaY;

        PlatformMotion(float x, float y, float deltaX, float deltaY) {
            this.x = x;
            this.y = y;
            this.deltaX = deltaX;
            this.deltaY = deltaY;
        }
    }

    static final class IcicleMotion {
        final float y;
        final float velocityY;
        final boolean landed;

        IcicleMotion(float y, float velocityY, boolean landed) {
            this.y = y;
            this.velocityY = velocityY;
            this.landed = landed;
        }
    }

    private EnvironmentRules() {}

    static PlatformMotion movingPlatform(float baseX, float baseY, float moveX, float moveY,
            float moveSpeed, float clock, float previousX, float previousY) {
        float wave = moveSpeed > 0f
                ? (float) Math.sin(clock * moveSpeed + baseX * .01f) : 0f;
        float x = baseX + moveX * wave;
        float y = baseY + moveY * wave;
        return new PlatformMotion(x, y, x - previousX, y - previousY);
    }

    static boolean spawnerDue(float timer, float deltaSeconds) {
        return timer - deltaSeconds <= 0f;
    }

    static float nextSpawnerTimer(float timer, float deltaSeconds, float interval) {
        float next = timer - deltaSeconds;
        while (next <= 0f) next += Math.max(.01f, interval);
        return next;
    }

    static IcicleMotion fallingIcicle(float y, float velocityY, float deltaSeconds,
            float landingY) {
        float nextVelocity = Math.min(980f, velocityY + 1450f * deltaSeconds);
        float nextY = y + nextVelocity * deltaSeconds;
        return new IcicleMotion(nextY, nextVelocity, nextY >= landingY);
    }

    static boolean hazardExpired(float age, float life) {
        return age >= life;
    }

    static float advanceAge(float age, float deltaSeconds) {
        return age + deltaSeconds;
    }
}