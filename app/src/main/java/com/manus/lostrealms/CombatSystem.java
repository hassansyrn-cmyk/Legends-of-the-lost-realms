package com.manus.lostrealms;

/** Pure combat predicates; the owning view still applies state changes and effects. */
final class CombatSystem {
    private CombatSystem() {
    }

    static boolean isTargetInFront(boolean facingLeft, float playerX, float targetX) {
        return facingLeft ? targetX < playerX - 4f : targetX > playerX + 4f;
    }

    static boolean canHitFromGround(
            boolean airAttack,
            boolean facingLeft,
            float playerX,
            float playerY,
            float targetX,
            float targetY,
            float reach,
            float verticalReach) {
        return !airAttack
                && isTargetInFront(facingLeft, playerX, targetX)
                && Math.abs(playerX - targetX) < reach
                && Math.abs(playerY - targetY) < verticalReach;
    }

    static int comboDamage(int stage, int attackRank, boolean charged, boolean counterWindow) {
        int normalizedStage = Math.max(1, Math.min(4, stage));
        int stageBonus = normalizedStage == 4 ? 2 : normalizedStage >= 2 ? 1 : 0;
        int chargedBonus = charged ? 2 : 0;
        int counterBonus = counterWindow ? 1 : 0;
        return 1 + Math.max(0, attackRank) + stageBonus + chargedBonus + counterBonus;
    }

    static boolean isPerfectDodge(float dodgeRemainingSeconds) {
        return dodgeRemainingSeconds >= .11f;
    }

    static boolean canHitFromAir(
            boolean airAttack,
            float playerX,
            float playerY,
            float targetX,
            float targetY,
            float horizontalReach,
            float playerAboveTargetReach,
            float playerBottomReach,
            float targetAbovePlayerReach) {
        return airAttack
                && Math.abs(playerX - targetX) < horizontalReach
                && playerY < targetY + playerAboveTargetReach
                && playerY + playerBottomReach > targetY - targetAbovePlayerReach;
    }
}
