package com.manus.lostrealms;

/**
 * Boss combat rules and deterministic attack selection.
 *
 * <p>The active scene still owns coordinates, rendering, audio, and hazards,
 * while this controller owns the state vocabulary and tactical decisions. This
 * keeps decisions testable without an Android runtime and prevents bosses from
 * degrading into a constant walk-and-attack loop.</p>
 */
final class BossController {
    enum State {
        OBSERVE,
        APPROACH,
        RETREAT,
        ATTACK_WINDUP,
        ATTACK_EXECUTE,
        ATTACK_RECOVERY,
        STAGGER,
        PHASE_TRANSITION,
        DEFEAT
    }

    enum Attack {
        ROOT_STRIKE("Root Strike", .42f, .18f, .46f),
        ROOT_WALL("Root Wall", .56f, .24f, .58f),
        ROOT_STORM("Root Storm", .68f, .32f, .72f),
        STONE_SLAM("Stone Slam", .58f, .20f, .64f),
        QUAKE_LANE("Quake Lane", .72f, .24f, .68f),
        FALLING_DEBRIS("Falling Debris", .64f, .32f, .72f),
        ICE_LANE("Ice Lane", .50f, .22f, .50f),
        FROST_WAVE("Frost Wave", .62f, .28f, .62f),
        WHITEOUT("Whiteout", .76f, .36f, .78f);

        final String displayName;
        final float windupSeconds;
        final float activeSeconds;
        final float recoverySeconds;

        Attack(String displayName, float windupSeconds, float activeSeconds, float recoverySeconds) {
            this.displayName = displayName;
            this.windupSeconds = windupSeconds;
            this.activeSeconds = activeSeconds;
            this.recoverySeconds = recoverySeconds;
        }
    }

    enum PowerReaction {
        BURN_EXPOSED,
        FROST_CRACKED,
        MELT_INTERRUPTED,
        GALE_DISPLACED,
        RESISTED
    }

    static final class Profile {
        final String phaseTwoName;
        final String phaseThreeName;
        final float phaseTwoThreshold;
        final float phaseThreeThreshold;
        final float phaseOneSpeed;
        final float phaseTwoSpeed;
        final float phaseThreeSpeed;

        Profile(String phaseTwoName, String phaseThreeName, float phaseTwoThreshold,
                float phaseThreeThreshold, float phaseOneSpeed, float phaseTwoSpeed,
                float phaseThreeSpeed) {
            this.phaseTwoName = phaseTwoName;
            this.phaseThreeName = phaseThreeName;
            this.phaseTwoThreshold = phaseTwoThreshold;
            this.phaseThreeThreshold = phaseThreeThreshold;
            this.phaseOneSpeed = phaseOneSpeed;
            this.phaseTwoSpeed = phaseTwoSpeed;
            this.phaseThreeSpeed = phaseThreeSpeed;
        }
    }

    private static final Profile[] PROFILES = {
        null,
        new Profile("Root Frenzy", "Heartwood Rampage", .60f, .27f, 55f, 78f, 102f),
        new Profile("Sandstorm", "Worldbreaker", .60f, .27f, 55f, 82f, 106f),
        new Profile("Whiteout", "Crown of Frost", .60f, .27f, 55f, 80f, 104f),
    };

    static Profile profile(int world) {
        return PROFILES[Math.max(1, Math.min(3, world))];
    }

    static int phaseForHealth(int health, int maxHealth, int world) {
        Profile profile = profile(world);
        float ratio = Math.max(0f, health) / (float) Math.max(1, maxHealth);
        if (ratio <= profile.phaseThreeThreshold) return 3;
        if (ratio <= profile.phaseTwoThreshold) return 2;
        return 1;
    }

    /**
     * Chooses an attack by purpose, not pure randomness. Position and recent
     * player behavior determine the attack family; the cycle only breaks ties.
     */
    static Attack chooseAttack(
            int world,
            int phase,
            float horizontalDistance,
            float verticalDistance,
            float playerVelocityX,
            boolean playerDodging,
            Attack previousAttack,
            int decisionCycle) {
        int safeWorld = Math.max(1, Math.min(3, world));
        int safePhase = Math.max(1, Math.min(3, phase));
        float distance = Math.abs(horizontalDistance);
        boolean playerAbove = verticalDistance < -82f;
        boolean fastApproach = Math.abs(playerVelocityX) > 260f;
        Attack selected;

        if (safeWorld == 1) {
            if (safePhase == 3 && (playerDodging || decisionCycle % 4 == 3)) {
                selected = Attack.ROOT_STORM;
            } else if (distance > 300f || playerAbove) {
                selected = Attack.ROOT_WALL;
            } else {
                selected = Attack.ROOT_STRIKE;
            }
        } else if (safeWorld == 2) {
            if (safePhase == 3 && (playerDodging || fastApproach)) {
                selected = Attack.FALLING_DEBRIS;
            } else if (distance > 270f || playerAbove) {
                selected = Attack.QUAKE_LANE;
            } else {
                selected = Attack.STONE_SLAM;
            }
        } else {
            if (safePhase == 3 && (playerDodging || decisionCycle % 3 == 2)) {
                selected = Attack.WHITEOUT;
            } else if (distance > 285f || playerAbove) {
                selected = Attack.FROST_WAVE;
            } else {
                selected = Attack.ICE_LANE;
            }
        }

        if (selected == previousAttack) {
            return alternateAttack(safeWorld, safePhase, selected);
        }
        return selected;
    }

    static float decisionCooldown(int phase, Attack attack) {
        float phasePressure = phase >= 3 ? .70f : phase == 2 ? .88f : 1f;
        float attackWeight = attack == Attack.ROOT_STORM
                || attack == Attack.FALLING_DEBRIS
                || attack == Attack.WHITEOUT ? 1.18f : 1f;
        return (attack.windupSeconds + attack.activeSeconds + attack.recoverySeconds)
                * phasePressure * attackWeight;
    }

    /**
     * Phase two introduces occasional two-move strings; phase three uses them
     * consistently. Follow-ups always change attack family, preserving a
     * readable recovery gap between the two telegraphs.
     */
    static Attack comboFollowUp(int world, int phase, Attack opening, int decisionCycle) {
        if (phase < 2 || (phase == 2 && decisionCycle % 2 != 0)) return null;
        int safeWorld = Math.max(1, Math.min(3, world));
        if (safeWorld == 1) {
            return opening == Attack.ROOT_WALL ? Attack.ROOT_STRIKE
                    : phase >= 3 ? Attack.ROOT_STORM : Attack.ROOT_WALL;
        }
        if (safeWorld == 2) {
            return opening == Attack.QUAKE_LANE ? Attack.STONE_SLAM
                    : phase >= 3 ? Attack.FALLING_DEBRIS : Attack.QUAKE_LANE;
        }
        return opening == Attack.FROST_WAVE ? Attack.ICE_LANE
                : phase >= 3 ? Attack.WHITEOUT : Attack.FROST_WAVE;
    }

    static PowerReaction powerReaction(int world, int power) {
        if (power == 2) return PowerReaction.GALE_DISPLACED;
        if (world == 1 && power == 0) return PowerReaction.BURN_EXPOSED;
        if (world == 2 && power == 1) return PowerReaction.FROST_CRACKED;
        if (world == 3 && power == 0) return PowerReaction.MELT_INTERRUPTED;
        return PowerReaction.RESISTED;
    }

    private static Attack alternateAttack(int world, int phase, Attack selected) {
        if (world == 1) {
            return selected == Attack.ROOT_STRIKE
                    ? Attack.ROOT_WALL
                    : phase >= 3 ? Attack.ROOT_STORM : Attack.ROOT_STRIKE;
        }
        if (world == 2) {
            return selected == Attack.STONE_SLAM
                    ? Attack.QUAKE_LANE
                    : phase >= 3 ? Attack.FALLING_DEBRIS : Attack.STONE_SLAM;
        }
        return selected == Attack.ICE_LANE
                ? Attack.FROST_WAVE
                : phase >= 3 ? Attack.WHITEOUT : Attack.ICE_LANE;
    }

    void update(GameView game, float deltaSeconds) {
        game.updateBossInternal(deltaSeconds);
    }
}
