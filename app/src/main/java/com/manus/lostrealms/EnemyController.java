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
        final float noticeRange;
        final float noticeHeight;
        final float windupSeconds;
        final int contactDamage;
        final boolean committedContactAttack;

        Archetype(String name, int maxHealth, float patrolSpeed, int accentColor,
                  float noticeRange, float noticeHeight, float windupSeconds,
                  int contactDamage, boolean committedContactAttack) {
            this.name = name;
            this.maxHealth = maxHealth;
            this.patrolSpeed = patrolSpeed;
            this.accentColor = accentColor;
            this.noticeRange = noticeRange;
            this.noticeHeight = noticeHeight;
            this.windupSeconds = windupSeconds;
            this.contactDamage = contactDamage;
            this.committedContactAttack = committedContactAttack;
        }
    }

    private static final Archetype[] ARCHETYPES = {
        new Archetype("Moss Mask Crawler", 2, 30f, 0xFFFFBE68, 175f, 88f, .34f, 1, true),
        new Archetype("Ember Moth", 2, 42f, 0xFFFF8F62, 235f, 135f, .42f, 1, true),
        new Archetype("Dune Skirmisher", 2, 54f, 0xFFFFB246, 210f, 90f, .46f, 1, true),
        new Archetype("Frost Sentinel", 2, 66f, 0xFF8DE8FF, 150f, 95f, .58f, 2, false),
        new Archetype("Wind Wisp", 4, 78f, 0xFFC099FF, 260f, 125f, .38f, 1, true),
        new Archetype("Aegis Guard", 3, 26f, 0xFF69D9D1, 180f, 90f, .52f, 1, true),
        new Archetype("Stone Brute", 5, 18f, 0xFFFF826E, 205f, 100f, .72f, 2, true),
        new Archetype("Rune Caster", 3, 0f, 0xFFB5A1FF, 315f, 165f, .66f, 1, false),
    };

    static Archetype archetype(int kind) {
        if (kind < 0 || kind >= ARCHETYPES.length) {
            return ARCHETYPES[GROUND_PATROLLER];
        }
        return ARCHETYPES[kind];
    }

    static boolean canNotice(int kind, float horizontalDistance, float verticalDistance) {
        Archetype archetype = archetype(kind);
        return Math.abs(horizontalDistance) < archetype.noticeRange
                && Math.abs(verticalDistance) < archetype.noticeHeight;
    }

    static boolean isCommittedContactAttack(int kind, int state) {
        return archetype(kind).committedContactAttack && state == 2;
    }

    static int contactDamage(int kind, int state) {
        return isCommittedContactAttack(kind, state) ? archetype(kind).contactDamage : 1;
    }

    void update(GameView game, float deltaSeconds) {
        game.updateFoesInternal(deltaSeconds);
    }
}
