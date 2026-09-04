package com.manus.lostrealms;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

import org.junit.Test;

public class EnemyControllerTest {
    @Test
    public void everyArchetypeHasAReadableEngagementWindow() {
        for (int kind = EnemyController.GROUND_PATROLLER;
             kind <= EnemyController.RUNE_CASTER; kind++) {
            EnemyController.Archetype behavior = EnemyController.archetype(kind);
            assertTrue(behavior.noticeRange > 0f);
            assertTrue(behavior.noticeHeight > 0f);
            assertTrue(behavior.windupSeconds >= .30f);
            assertTrue(EnemyController.canNotice(kind, 0f, 0f));
            assertFalse(EnemyController.canNotice(kind, behavior.noticeRange + 1f, 0f));
        }
    }

    @Test
    public void onlyCommittedContactStatesDealHeavyDamage() {
        assertEquals(1, EnemyController.contactDamage(EnemyController.GROUND_PATROLLER, 1));
        assertEquals(1, EnemyController.contactDamage(EnemyController.GROUND_PATROLLER, EnemyController.ATTACK));
        assertEquals(2, EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, EnemyController.ATTACK));
        assertEquals(1, EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, EnemyController.RECOVERY));
        assertTrue(EnemyController.isCommittedContactAttack(
                EnemyController.FLYING_SWOOOPER, EnemyController.ATTACK));
    }

    @Test
    public void rangedCasterHasLongerTelegraphThanFastSkirmisher() {
        assertTrue(
                EnemyController.archetype(EnemyController.RUNE_CASTER).windupSeconds
                        > EnemyController.archetype(EnemyController.FAST_SKIRMISHER).windupSeconds);
    }

    @Test
    public void laterLevelsScaleWithoutRemovingTelegraphs() {
        assertTrue(EnemyController.scaledHealth(EnemyController.HEAVY_BRUTE, 10)
                > EnemyController.scaledHealth(EnemyController.HEAVY_BRUTE, 1));
        assertTrue(EnemyController.speedScale(10) > EnemyController.speedScale(1));
        assertTrue(EnemyController.timingScale(10) >= .80f);
    }

    @Test
    public void predictiveAimingLeadsMovementButCapsUnfairShots() {
        assertEquals(148f, EnemyController.predictedTarget(100f, 240f, .20f, 80f), .001f);
        assertEquals(180f, EnemyController.predictedTarget(100f, 900f, .30f, 80f), .001f);
        assertEquals(20f, EnemyController.predictedTarget(100f, -900f, .30f, 80f), .001f);
    }
}
