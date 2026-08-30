package com.manus.lostrealms;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotEquals;
import static org.junit.Assert.assertNull;

import org.junit.Test;

public class BossControllerTest {
    @Test
    public void healthThresholdsCreateThreeDistinctPhases() {
        assertEquals(1, BossController.phaseForHealth(24, 24, 1));
        assertEquals(2, BossController.phaseForHealth(14, 24, 1));
        assertEquals(3, BossController.phaseForHealth(6, 24, 1));
    }

    @Test
    public void thornwoldUsesRangeToChooseRootWall() {
        assertEquals(
                BossController.Attack.ROOT_WALL,
                BossController.chooseAttack(1, 1, 420f, 0f, 0f, false, null, 0));
    }

    @Test
    public void finalPhasePunishesRepeatedDodgeBehavior() {
        assertEquals(
                BossController.Attack.WHITEOUT,
                BossController.chooseAttack(3, 3, 180f, 0f, 0f, true, null, 0));
    }

    @Test
    public void attackSelectionAvoidsImmediateRepeats() {
        BossController.Attack next = BossController.chooseAttack(
                2, 2, 120f, 0f, 0f, false, BossController.Attack.STONE_SLAM, 1);
        assertNotEquals(BossController.Attack.STONE_SLAM, next);
    }

    @Test
    public void combosEscalateAndNeverRepeatTheOpening() {
        assertNull(BossController.comboFollowUp(
                1, 1, BossController.Attack.ROOT_STRIKE, 0));
        BossController.Attack followUp = BossController.comboFollowUp(
                2, 3, BossController.Attack.STONE_SLAM, 2);
        assertNotEquals(BossController.Attack.STONE_SLAM, followUp);
    }

    @Test
    public void eachRealmHasAThematicPowerCounter() {
        assertEquals(BossController.PowerReaction.BURN_EXPOSED,
                BossController.powerReaction(1, PowerSystem.EMBER));
        assertEquals(BossController.PowerReaction.FROST_CRACKED,
                BossController.powerReaction(2, PowerSystem.FROST));
        assertEquals(BossController.PowerReaction.MELT_INTERRUPTED,
                BossController.powerReaction(3, PowerSystem.EMBER));
    }
}