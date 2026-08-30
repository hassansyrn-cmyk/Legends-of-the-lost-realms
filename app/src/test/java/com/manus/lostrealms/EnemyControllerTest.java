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
        assertEquals(1, EnemyController.contactDamage(EnemyController.GROUND_PATROLLER, 2));
        assertEquals(2, EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, 2));
        assertEquals(1, EnemyController.contactDamage(EnemyController.HEAVY_BRUTE, 3));
        assertTrue(EnemyController.isCommittedContactAttack(
                EnemyController.FLYING_SWOOOPER, 2));
    }

    @Test
    public void rangedCasterHasLongerTelegraphThanFastSkirmisher() {
        assertTrue(
                EnemyController.archetype(EnemyController.RUNE_CASTER).windupSeconds
                        > EnemyController.archetype(EnemyController.FAST_SKIRMISHER).windupSeconds);
    }
}