package com.manus.lostrealms;

/**
 * Owns reusable enemy archetype data while the current scene keeps runtime
 * positions and temporary combat state. This is a safe bridge away from
 * hard-coded per-level enemy tuning.
 */
final class EnemyController {
    static final int GROUND_PATROLLER = 0;
    static final int FLYING_SWOOOPER = 1;
    static final int FAST_SKIRMISHER = 2;
    static final int FROST_SENTINEL = 3;
    static final int WIND_WISP = 4;
    static final int SHIELD_GUARD = 5;
    static final int HEAVY_BRUTE = 6;
    static final int RUNE_CASTER = 7;

    static final class Archetype {
        final String name;
        final int maxHealth;
        final float patrolSpeed;
        final int accentColor;

        Archetype(String name, int maxHealth, float patrolSpeed, int accentColor) {
            this.name = name;
            this.maxHealth = maxHealth;
            this.patrolSpeed = patrolSpeed;
            this.accentColor = accentColor;
        }
    }

    private static final Archetype[] ARCHETYPES = {
        new Archetype("Moss Mask Crawler", 2, 30f, 0xFFFFBE68),
        new Archetype("Ember Moth", 2, 42f, 0xFFFF8F62),
        new Archetype("Dune Skirmisher", 2, 54f, 0xFFFFB246),
        new Archetype("Frost Sentinel", 2, 66f, 0xFF8DE8FF),
        new Archetype("Wind Wisp", 4, 78f, 0xFFC099FF),
        new Archetype("Aegis Guard", 3, 26f, 0xFF69D9D1),
        new Archetype("Stone Brute", 5, 18f, 0xFFFF826E),
        new Archetype("Rune Caster", 3, 0f, 0xFFB5A1FF),
    };

    static Archetype archetype(int kind) {
        if (kind < 0 || kind >= ARCHETYPES.length) {
            return ARCHETYPES[GROUND_PATROLLER];
        }
        return ARCHETYPES[kind];
    }

    void update(GameView game, float deltaSeconds) {
        game.updateFoesInternal(deltaSeconds);
    }
}
