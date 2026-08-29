package com.manus.lostrealms;

/** Pure balance rules for the three player powers. */
final class PowerSystem {
    static final int EMBER = 0;
    static final int FROST = 1;
    static final int GALE = 2;

    private PowerSystem() {
    }

    static float energyCost(int power, int relicRank) {
        float base = power == EMBER ? 34f : power == FROST ? 30f : 26f;
        return Math.max(16f, base - Math.max(0, relicRank) * 2f);
    }

    static float castDuration(int power) {
        return power == EMBER ? .34f : power == FROST ? .38f : .42f;
    }

    static float emberDuration() { return 3.0f; }
    static float emberTickInterval() { return .60f; }
    static float frostDuration() { return 2.5f; }
    static float galeBurstDuration() { return .34f; }

    static int emberTickDamage(int attackRank) {
        return 1 + (attackRank >= 3 ? 1 : 0);
    }

    static float galeKnockback(float deltaX) {
        return deltaX < 0 ? -520f : 520f;
    }
}
