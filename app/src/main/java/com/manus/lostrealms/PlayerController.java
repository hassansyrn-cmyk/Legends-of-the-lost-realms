package com.manus.lostrealms;

/** Small pure movement helpers shared by player and enemy movement. */
final class PlayerController {
    private PlayerController() {
    }

    static float approach(float value, float target, float amount) {
        if (value < target) {
            return Math.min(target, value + amount);
        }
        return Math.max(target, value - amount);
    }
}
