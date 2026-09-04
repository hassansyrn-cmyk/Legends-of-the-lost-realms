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

    static final int PATROL = 0;
    static final int NOTICE = 1;
    static final int WINDUP = 2;
    static final int ATTACK = 3;
    static final int RECOVERY = 4;
    static final int REPOSITION = 5;
    static final int HIT_REACTION = 6;

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
        final float noticeSeconds;
        final float attackSeconds;
        final float recoverySeconds;
        final float repositionSeconds;

        Archetype(String name, int maxHealth, float patrolSpeed, int accentColor,
                  float noticeRange, float noticeHeight, float windupSeconds,
                   int contactDamage, boolean committedContactAttack,
                   float noticeSeconds, float attackSeconds, float recoverySeconds,
                   float repositionSeconds) {
            this.name = name;
            this.maxHealth = maxHealth;
            this.patrolSpeed = patrolSpeed;
            this.accentColor = accentColor;
            this.noticeRange = noticeRange;
            this.noticeHeight = noticeHeight;
            this.windupSeconds = windupSeconds;
            this.contactDamage = contactDamage;
            this.committedContactAttack = committedContactAttack;
            this.noticeSeconds = noticeSeconds;
            this.attackSeconds = attackSeconds;
            this.recoverySeconds = recoverySeconds;
            this.repositionSeconds = repositionSeconds;
        }
    }

    private static final Archetype[] ARCHETYPES = {
        new Archetype("Moss Mask Crawler", 2, 30f, 0xFFFFBE68, 175f, 88f, .34f, 1, true, .16f, .30f, .40f, .42f),
        new Archetype("Ember Moth", 2, 42f, 0xFFFF8F62, 235f, 135f, .42f, 1, true, .18f, .42f, .46f, .60f),
        new Archetype("Dune Skirmisher", 2, 54f, 0xFFFFB246, 210f, 90f, .46f, 1, true, .12f, .34f, .28f, .48f),
        new Archetype("Frost Sentinel", 2, 66f, 0xFF8DE8FF, 150f, 95f, .58f, 2, false, .22f, .24f, .62f, .48f),
        new Archetype("Wind Wisp", 4, 78f, 0xFFC099FF, 260f, 125f, .38f, 1, true, .14f, .30f, .34f, .56f),
        new Archetype("Aegis Guard", 3, 26f, 0xFF69D9D1, 180f, 90f, .52f, 1, true, .20f, .32f, .48f, .58f),
        new Archetype("Stone Brute", 5, 18f, 0xFFFF826E, 205f, 100f, .72f, 2, true, .24f, .30f, .72f, .64f),
        new Archetype("Rune Caster", 3, 0f, 0xFFB5A1FF, 315f, 165f, .66f, 1, false, .20f, .18f, .64f, .72f),
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
        return archetype(kind).committedContactAttack && state == ATTACK;
    }

    static int contactDamage(int kind, int state) {
        return isCommittedContactAttack(kind, state) ? archetype(kind).contactDamage : 1;
    }

    static int scaledHealth(int kind, int level) {
        int tier = Math.max(0, Math.min(3, (level - 1) / 3));
        return archetype(kind).maxHealth + tier;
    }

    static float speedScale(int level) {
        return 1f + Math.max(0, Math.min(9, level - 1)) * .025f;
    }

    static float timingScale(int level) {
        return Math.max(.82f, 1f - Math.max(0, Math.min(9, level - 1)) * .02f);
    }

    /** Leads mobile targets without allowing impossible off-screen prediction. */
    static float predictedTarget(float position, float velocity, float leadSeconds, float maxLead) {
        float lead = Math.max(-maxLead, Math.min(maxLead, velocity * Math.max(0f, leadSeconds)));
        return position + lead;
    }

    static int scaledContactDamage(int kind, int state, int level) {
        // Difficulty rises through durability, speed, tighter (but floored) telegraphs,
        // mixed encounters, and denser hazards. Avoid damage spikes that can remove
        // most of Aster's base health during one crowded late-game exchange.
        return contactDamage(kind, state);
    }

    void update(GameView game, float deltaSeconds) {
        game.updateFoesInternal(deltaSeconds);
    }
}
