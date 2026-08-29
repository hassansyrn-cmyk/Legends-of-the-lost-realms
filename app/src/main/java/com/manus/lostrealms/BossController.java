package com.manus.lostrealms;

/** Reusable boss phase data; the active scene owns transient arena hazards. */
final class BossController {
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

    void update(GameView game, float deltaSeconds) {
        game.updateBossInternal(deltaSeconds);
    }
}
