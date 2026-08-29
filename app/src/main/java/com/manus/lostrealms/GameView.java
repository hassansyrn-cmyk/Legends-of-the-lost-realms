package com.manus.lostrealms;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.Rect;
import android.graphics.RectF;
import android.graphics.RadialGradient;
import android.graphics.Shader;
import android.graphics.Typeface;
import android.view.MotionEvent;
import android.view.View;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.Random;

public class GameView extends View {
    static final float VIRTUAL_WIDTH = 1280f;
    static final float VIRTUAL_HEIGHT = 720f;
    /** Set to false for a no-juice before/after comparison build. */
    private static boolean JUICE_ENABLED = true;
    private static final float VW = VIRTUAL_WIDTH;
    private static final float VH = VIRTUAL_HEIGHT;
    static final int SPLASH = 0;
    static final int MENU = 1;
    static final int MAP = 2;
    static final int LEVEL = 3;
    static final int UPGRADES = 4;
    static final int SETTINGS = 5;
    static final int PAUSE = 6;
    static final int COMPLETE = 7;
    static final int GAMEOVER = 8;
    static final int DEV_TOOLS = 9;
    private final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint stroke = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final SaveManager save;
    private final GameAudio audio;
    private final InputHandler inputHandler = new InputHandler();
    private final GameRenderer gameRenderer = new GameRenderer();
    private final EnemyController enemyController = new EnemyController();
    private final BossController bossController = new BossController();
    private final Bitmap splashArt, mapArt, verdantArt, dunesArt, frozenArt, verdantFar, verdantMid, verdantPlatform, verdantThorns, verdantCoin, verdantGem, verdantLevelBackdrop, jungleStageBackground, jungleGroundPlatform, jungleFloatingPlatform, verdantWaterfallBackdrop, icePlatformMotionSheet, goldenPlatformMotionSheet, hangingIceSpikesMotionSheet, trapPlatformMotionSheet;
    private final Bitmap asterIdleSheet, asterRunSheet, asterJump, asterAttackSheet, asterHurtSheet, asterDefeatSheet, enemySheet, enemyMotionAtlas, mossCrawlerSheet, emberMothSheet, worldProps, worldPropsClean, attackButton, uiHeartFull, uiHeartEmpty, uiShield, uiEnergyBolt, fxSwordHit, fxTrail, fxCoinSparkle, coinNew, gemNew, superHeartFull, superHeartEmpty, superHudBox, superHitLine, bossSheet, bossMotionAtlas, bossForestRigParts, bossStoneRigParts, bossIceRigParts, worldInteractiveAtlas, collectiblesFxAtlas, actionFxAtlas;
    private final Random rng = new Random(23);
    private final ArrayList<Platform> platforms = new ArrayList<>();
    private final ArrayList<Pickup> pickups = new ArrayList<>();
    private final ArrayList<Foe> foes = new ArrayList<>();
    private final ArrayList<Hazard> hazards = new ArrayList<>();
    private final ArrayList<WindZone> windZones = new ArrayList<>();
    private final ArrayList<HeatZone> heatZones = new ArrayList<>();
    private final ArrayList<IcicleSpawner> icicleSpawners = new ArrayList<>();
    private final ArrayList<FallingIcicle> fallingIcicles = new ArrayList<>();
    private final ArrayList<EnemyProjectile> enemyProjectiles = new ArrayList<>();
    private final ArrayList<JuiceParticle> juiceParticles = new ArrayList<>();
    private LevelData data;
    private Boss boss;
    private String levelDesignRole = "";
    private String groundMaterial = "STONE";
    private int screen = SPLASH, currentLevel = 1, health = 5, maxHealth = 5, power = 0, coinsRun = 0, gemsRun = 0, damageTaken = 0, completedStars = 0, completedCoinReward = 0, completedGemReward = 0;
    private long completedTimeMillis = 0;
    private boolean newStarRecord = false, newBestTime = false, worldRestored = false;
    private float energy = 100f, maxEnergy = 100f, levelElapsed = 0f, storyTime = 0f;
    private float px, py, vx, vy, cameraX, checkpointX, checkpointY, checkpointMarkerX, checkpointMarkerY;
    private float splashElapsed = 0, attackTime = 0, attackDuration = 0, comboWindow = 0, chargeTime = 0, counterTime = 0, perfectDodgeTime = 0, powerTime = 0, galeBurstTime = 0, invincible = 0, hurtFlash = 0, hurtTime = 0, defeatTime = 0, windTime = 0, swordFxTime = 0, hitFxTime = 0, sparkleFxTime = 0, animationClock = 0, fxX = 0, fxY = 0, coyoteTime = 0, jumpBufferTime = 0, landingPulse = 0, hitPause = 0, knockbackTime = 0, footstepTimer = 0, dashTime = 0, dashCooldown = 0, slideTime = 0, dashFxTime = 0, dodgeTime = 0, dodgeFxTime = 0, airStrikeFxTime = 0, wallTop = 0, screenShakeTime = 0, screenShakeStrength = 0, playerSquashTime = 0;
    private float secretMessageTime = 0, combatCalloutTime = 0, combatCalloutX = 0, combatCalloutY = 0;
    private String secretMessage = "", combatCallout = "";
    private long lastLeftTap = 0, lastRightTap = 0;
    private boolean grounded, canDouble, leftHeld, rightHeld, jumpHeld, attackHolding = false, chargeReady = false, chargedAttack = false, facingLeft = false, pausedBySystem = false, checkpointActive = false, jumpQueued = false, comboQueued = false, wallSliding = false, airAttack = false, playerSquashLanding = false, devStatsOverlay = false, devCoordinatesOverlay = false;
    private int wallDir = 0, storyKind = 0;
    private int attackStage = 0;
    private long lastNanos = System.nanoTime();
    private final String[] powers = {"EMBER", "FROST", "GALE"};

    public GameView(Context context) {
        super(context);
        setFocusable(true);
        p.setTypeface(Typeface.create("sans", Typeface.BOLD));
        stroke.setStyle(Paint.Style.STROKE);
        stroke.setStrokeWidth(3);
        stroke.setColor(Color.WHITE);
        save = new SaveManager(context);
        audio = new GameAudio();
        audio.configure(save.musicEnabled(), save.sfxEnabled());
        splashArt = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_splash);
        mapArt = BitmapFactory.decodeResource(getResources(), R.drawable.realm_map);
        verdantArt = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_splash);
        dunesArt = BitmapFactory.decodeResource(getResources(), R.drawable.burning_dunes);
        frozenArt = BitmapFactory.decodeResource(getResources(), R.drawable.frozen_peaks);
        verdantFar = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_far);
        verdantMid = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_mid);
        verdantPlatform = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_platform);
        verdantThorns = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_thorns);
        verdantCoin = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_coin);
        verdantGem = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_gem);
        verdantLevelBackdrop = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_level_backdrop);
        jungleStageBackground = BitmapFactory.decodeResource(getResources(), R.drawable.jungle_stage_background);
        jungleGroundPlatform = BitmapFactory.decodeResource(getResources(), R.drawable.jungle_ground_platform);
        jungleFloatingPlatform = BitmapFactory.decodeResource(getResources(), R.drawable.jungle_floating_platform);
        verdantWaterfallBackdrop = BitmapFactory.decodeResource(getResources(), R.drawable.verdant_waterfall_backdrop);
        icePlatformMotionSheet = BitmapFactory.decodeResource(getResources(), R.drawable.ice_platform_motion_sheet);
        goldenPlatformMotionSheet = BitmapFactory.decodeResource(getResources(), R.drawable.golden_platform_motion_sheet);
        hangingIceSpikesMotionSheet = BitmapFactory.decodeResource(getResources(), R.drawable.hanging_ice_spikes_motion_sheet);
        trapPlatformMotionSheet = BitmapFactory.decodeResource(getResources(), R.drawable.trap_platform_motion_sheet);
        asterIdleSheet = BitmapFactory.decodeResource(getResources(), R.drawable.aster_idle_sheet);
        asterRunSheet = BitmapFactory.decodeResource(getResources(), R.drawable.aster_run_sheet);
        asterJump = BitmapFactory.decodeResource(getResources(), R.drawable.aster_jump);
        asterAttackSheet = BitmapFactory.decodeResource(getResources(), R.drawable.aster_attack_sheet);
        asterHurtSheet = BitmapFactory.decodeResource(getResources(), R.drawable.aster_hurt_sheet);
        asterDefeatSheet = BitmapFactory.decodeResource(getResources(), R.drawable.aster_defeat_sheet);
        enemySheet = BitmapFactory.decodeResource(getResources(), R.drawable.enemy_sheet);
        enemyMotionAtlas = BitmapFactory.decodeResource(getResources(), R.drawable.enemy_archetype_motion_atlas);
        mossCrawlerSheet = BitmapFactory.decodeResource(getResources(), R.drawable.moss_mask_crawler_sheet);
        emberMothSheet = BitmapFactory.decodeResource(getResources(), R.drawable.ember_moth_sheet);
        worldProps = BitmapFactory.decodeResource(getResources(), R.drawable.world_props);
        worldPropsClean = BitmapFactory.decodeResource(getResources(), R.drawable.world_props_clean);
        attackButton = BitmapFactory.decodeResource(getResources(), R.drawable.attack_button_blue);
        uiHeartFull = BitmapFactory.decodeResource(getResources(), R.drawable.ui_heart_full_new);
        uiHeartEmpty = BitmapFactory.decodeResource(getResources(), R.drawable.ui_heart_empty_new);
        uiShield = BitmapFactory.decodeResource(getResources(), R.drawable.ui_shield_new);
        uiEnergyBolt = BitmapFactory.decodeResource(getResources(), R.drawable.ui_energy_bolt_new);
        fxSwordHit = BitmapFactory.decodeResource(getResources(), R.drawable.fx_sword_hit);
        fxTrail = BitmapFactory.decodeResource(getResources(), R.drawable.fx_trail);
        fxCoinSparkle = BitmapFactory.decodeResource(getResources(), R.drawable.fx_coin_sparkle);
        actionFxAtlas = BitmapFactory.decodeResource(getResources(), R.drawable.action_fx_motion_atlas);
        coinNew = BitmapFactory.decodeResource(getResources(), R.drawable.coin_gold_new);
        gemNew = BitmapFactory.decodeResource(getResources(), R.drawable.realm_gem_blue_new);
        superHeartFull = BitmapFactory.decodeResource(getResources(), R.drawable.super_heart_full);
        superHeartEmpty = BitmapFactory.decodeResource(getResources(), R.drawable.super_heart_empty);
        superHudBox = BitmapFactory.decodeResource(getResources(), R.drawable.super_hud_box);
        superHitLine = BitmapFactory.decodeResource(getResources(), R.drawable.super_hit_line);
        bossSheet = BitmapFactory.decodeResource(getResources(), R.drawable.boss_sheet);
        bossMotionAtlas = BitmapFactory.decodeResource(getResources(), R.drawable.boss_motion_atlas);
        bossForestRigParts = BitmapFactory.decodeResource(getResources(), R.drawable.boss_forest_rig_parts);
        bossStoneRigParts = BitmapFactory.decodeResource(getResources(), R.drawable.boss_stone_rig_parts);
        bossIceRigParts = BitmapFactory.decodeResource(getResources(), R.drawable.boss_ice_rig_parts);
        worldInteractiveAtlas = BitmapFactory.decodeResource(getResources(), R.drawable.world_interactives_motion_atlas);
        collectiblesFxAtlas = BitmapFactory.decodeResource(getResources(), R.drawable.collectibles_fx_motion_atlas);
        audio.startMusic(context);
    }

    public void pauseGame() { pausedBySystem = true; audio.pauseMusic(); }
    public void resumeGame() { pausedBySystem = false; if (screen != PAUSE) audio.resumeMusic(); lastNanos = System.nanoTime(); }

    @Override protected void onDraw(Canvas raw) {
        super.onDraw(raw);
        float nowDt = Math.min(0.05f, (System.nanoTime() - lastNanos) / 1_000_000_000f);
        lastNanos = System.nanoTime();
        if (perfectDodgeTime > 0) {
            perfectDodgeTime = Math.max(0, perfectDodgeTime - nowDt);
            nowDt *= .42f;
        }
        float scale = Math.min(getWidth() / VW, getHeight() / VH);
        float offsetX = (getWidth() - VW * scale) * 0.5f;
        float offsetY = (getHeight() - VH * scale) * 0.5f;
        raw.drawColor(Color.rgb(3, 12, 17));
        raw.save(); raw.translate(offsetX, offsetY); raw.scale(scale, scale);
        if (!pausedBySystem) update(nowDt);
        if (JUICE_ENABLED && screenShakeTime > 0) {
            float shakeX = (float) Math.sin(animationClock * 93f) * screenShakeStrength;
            float shakeY = (float) Math.cos(animationClock * 117f) * screenShakeStrength * .58f;
            raw.translate(shakeX, shakeY);
        }
        gameRenderer.render(this, raw);
        raw.restore();
        postInvalidateOnAnimation();
    }

    private void update(float dt) {
        animationClock += dt;
        if (screen == SPLASH) { splashElapsed += dt; if (splashElapsed > 2.0f) screen = MENU; return; }
        if (screen == GAMEOVER) { defeatTime += dt; return; }
        if (screen != LEVEL) return;
        if (storyTime > 0) { storyTime -= dt; return; }
        if (secretMessageTime > 0) secretMessageTime -= dt;
        if (combatCalloutTime > 0) combatCalloutTime -= dt;
        levelElapsed += dt;
        if (attackTime > 0) {
            attackTime -= dt;
            if (attackTime <= 0) {
                airAttack = false;
                if (comboQueued && attackStage < 4) {
                    chargedAttack = false;
                    beginAttack(attackStage + 1);
                } else {
                    attackStage = 0;
                    chargedAttack = false;
                }
            }
        }
        if (comboWindow > 0) comboWindow -= dt;
        if (counterTime > 0) counterTime -= dt;
        if (attackHolding && attackTime <= 0) {
            chargeTime += dt;
            if (chargeTime >= .38f && !chargeReady) {
                chargeReady = true;
                audio.powerSelect();
            }
        }
        if (powerTime > 0) powerTime -= dt;
        else energy = Math.min(maxEnergy, energy + 15f * dt);
        if (galeBurstTime > 0) galeBurstTime -= dt;
        if (invincible > 0) invincible -= dt;
        if (hurtFlash > 0) hurtFlash -= dt;
        if (hurtTime > 0) hurtTime -= dt;
        if (windTime > 0) windTime -= dt;
        if (swordFxTime > 0) swordFxTime -= dt;
        if (hitFxTime > 0) hitFxTime -= dt;
        if (sparkleFxTime > 0) sparkleFxTime -= dt;
        if (landingPulse > 0) landingPulse -= dt;
        if (footstepTimer > 0) footstepTimer -= dt;
        if (dashCooldown > 0) dashCooldown -= dt;
        if (slideTime > 0) slideTime -= dt;
        if (dashFxTime > 0) dashFxTime -= dt;
        if (dodgeTime > 0) dodgeTime -= dt;
        if (dodgeFxTime > 0) dodgeFxTime -= dt;
        if (airStrikeFxTime > 0) airStrikeFxTime -= dt;
        if (screenShakeTime > 0) {
            screenShakeTime -= dt;
            if (screenShakeTime <= 0) {
                screenShakeStrength = 0;
            }
        }
        if (playerSquashTime > 0) playerSquashTime -= dt;
        updateJuiceParticles(dt);
        updateEnvironmentalSystems(dt);
        boolean wasDashing = dashTime > 0;
        if (dashTime > 0) dashTime -= dt;
        if (hitPause > 0) { hitPause -= dt; return; }
        if (knockbackTime > 0) knockbackTime -= dt;
        if (grounded) coyoteTime = .10f; else coyoteTime = Math.max(0, coyoteTime - dt);
        if (jumpQueued) { jumpBufferTime = .12f; jumpQueued = false; }
        else if (jumpBufferTime > 0) jumpBufferTime -= dt;
        float speed = 310 + save.windRank() * 24;
        if (leftHeld && !rightHeld) facingLeft = true;
        else if (rightHeld && !leftHeld) facingLeft = false;
        if (wasDashing && dashTime <= 0 && grounded) slideTime = Math.max(slideTime, .18f);
        if (knockbackTime <= 0) {
            if (dashTime > 0) {
                vx = facingLeft ? -650f : 650f;
            } else if (slideTime > 0 && grounded) {
                vx = approach(vx, facingLeft ? -220f : 220f, 1700f * dt);
            } else {
                float target = leftHeld == rightHeld ? 0 : (leftHeld ? -speed : speed);
                float traction = grounded && "ICE".equals(groundMaterial) ? .48f : 1f;
                float rate = target == 0 ? (grounded ? 2500f : 860f) : (grounded ? 2100f : 1250f);
                vx = approach(vx, target, rate * traction * dt);
            }
        }
        if (jumpBufferTime > 0 && jump()) jumpBufferTime = 0;
        if (airAttack && attackTime > 0) vy = Math.max(vy, 540f);
        float gravity = wallSliding && vy > 0 ? 0 : (windTime > 0 && vy > 0 ? 520 : 1450);
        if (!jumpHeld && vy < -170) gravity *= 1.72f;
        vy += gravity * dt;
        vy = Math.min(vy, 950);
        float oldBottom = py + 54, fallSpeed = vy;
        px += vx * dt;
        px = Math.max(12, Math.min(2240, px));
        py += vy * dt;
        boolean wasGrounded = grounded;
        grounded = false;
        for (Platform pl : platforms) {
            if (px + 26 > pl.x && px - 26 < pl.x + pl.w && oldBottom <= pl.y + 8 && py + 54 >= pl.y && vy >= 0) {
                if (!wasGrounded) {
                    audio.land(fallSpeed > 420);
                    if (fallSpeed > 420) {
                        landingPulse = .14f;
                        triggerSquash(true, .12f);
                        triggerShake(3.6f, .09f);
                        spawnJuiceParticles(px, py + 52, Color.rgb(235, 221, 166), 7, 105f);
                    }
                }
                py = pl.y - 54; vy = 0; grounded = true; canDouble = true; groundMaterial = pl.material;
                if (pl.crumble) pl.life -= dt;
            }
        }
        platforms.removeIf(pl -> pl.crumble && pl.life <= 0);
        wallSliding = false;
        if (!grounded && vy > 0) {
            for (Platform pl : platforms) {
                boolean verticalContact = py + 48 > pl.y + 8 && py < pl.y + pl.h;
                if (verticalContact && rightHeld && px + 26 >= pl.x && px < pl.x + pl.w * .30f) { px = pl.x - 26; vx = Math.min(vx, 0); wallSliding = true; wallDir = -1; wallTop = pl.y; break; }
                if (verticalContact && leftHeld && px - 26 <= pl.x + pl.w && px > pl.x + pl.w * .70f) { px = pl.x + pl.w + 26; vx = Math.max(vx, 0); wallSliding = true; wallDir = 1; wallTop = pl.y; break; }
            }
            if (wallSliding) { vy = Math.min(vy, 165f); canDouble = true; }
        }
        applyWindForces(dt);
        if (grounded && Math.abs(vx) > 85 && footstepTimer <= 0) {
            audio.step(Math.abs(vx));
            footstepTimer = Math.abs(vx) > 250 ? .21f : .29f;
        }
        if (py > 760) respawn();
        if (!checkpointActive && Math.abs(px - checkpointMarkerX) < 62 && Math.abs(py - checkpointMarkerY) < 145) { checkpointActive = true; checkpointX = checkpointMarkerX; checkpointY = checkpointMarkerY; audio.checkpoint(); }
        float lookAhead = JUICE_ENABLED ? (facingLeft ? -58f : 58f) * Math.min(1f, Math.abs(vx) / 190f) : 0f;
        float cameraTarget = px - 280 + lookAhead;
        cameraX += (cameraTarget - cameraX) * Math.min(1, dt * 4.4f);
        cameraX = Math.max(0, Math.min(1100, cameraX));
        updatePickups();
        enemyController.update(this, dt);
        bossController.update(this, dt);
        Iterator<Hazard> hazardIt=hazards.iterator(); RectF playerHitbox=playerRect();
        while(hazardIt.hasNext()){ Hazard h=hazardIt.next(); h.age+=dt; if(h.age>=h.life){hazardIt.remove(); continue;} if(h.active()&&RectF.intersects(playerHitbox,h.rect())) damage(1); }
        applyHeatZones(playerHitbox);
        if (!data.boss && px > 2145) completeLevel();
        if (data.boss && boss != null && boss.hp <= 0 && px > 2125) completeLevel();
    }

    private void updatePickups() {
        RectF r = playerRect();
        Iterator<Pickup> it = pickups.iterator();
        while (it.hasNext()) {
            Pickup k = it.next();
            if (RectF.intersects(r, k.rect())) {
                if ("SECRET".equals(k.route)) {
                    if (save.recordSecretFound(k.secretId)) {
                        int reward = Math.max(1, k.secretRewardGems);
                        gemsRun += reward;
                        energy = Math.min(maxEnergy, energy + 28f);
                        for (int index = 0; index < reward; index++) save.recordPickup(true);
                        secretMessage = "SECRET CACHE  +" + reward + " GEMS";
                        secretMessageTime = 2.4f;
                        spawnJuiceParticles(k.x, k.y, Color.rgb(208, 150, 255), 14, 155f);
                    }
                } else {
                    if (k.gem) { gemsRun++; energy = Math.min(maxEnergy, energy + 24f); } else coinsRun++;
                    spawnJuiceParticles(k.x, k.y, k.gem ? Color.rgb(128, 232, 255) : Color.rgb(255, 219, 98), k.gem ? 9 : 5, k.gem ? 125f : 88f);
                    save.recordPickup(k.gem);
                }
                fxX = k.x; fxY = k.y; sparkleFxTime = .34f;
                audio.collect(true); it.remove();
            }
        }
    }

    void updateFoesInternal(float dt) {
        for (Foe e : foes) {
            if (e.hurtTime > 0) e.hurtTime -= dt;
            if (e.hitLock > 0) e.hitLock -= dt;
            if (e.burnTime > 0) {
                e.burnTime -= dt;
                e.burnTick -= dt;
                if (e.burnTick <= 0) {
                    e.hp -= PowerSystem.emberTickDamage(save.attackRank());
                    e.burnTick = PowerSystem.emberTickInterval();
                    e.hurtTime = .12f;
                    spawnJuiceParticles(e.x, e.y - 18, Color.rgb(255, 130, 62), 4, 88f);
                }
            }
            if (e.frozen > 0) { e.frozen -= dt; continue; }
            float dx = px - e.x;
            if (e.kind == EnemyController.GROUND_PATROLLER) {
                if (e.state == 0) { e.x += e.dir * e.speed * dt; if (e.x < e.minX || e.x > e.maxX) e.dir *= -1; if (Math.abs(dx) < 175 && Math.abs(py-e.y)<88) { e.state=1; e.stateTime=.34f; e.dir=dx<0?-1:1; audio.enemyWarning(); } }
                else if (e.state == 1) { e.stateTime -= dt; if (e.stateTime <= 0) { e.state=2; e.stateTime=.30f; audio.enemyDash(); } }
                else { e.x += e.dir * 245 * dt; e.stateTime -= dt; if (e.stateTime <= 0 || e.x < e.minX-45 || e.x > e.maxX+45) { e.state=0; e.dir*=-1; } }
            } else if (e.kind == EnemyController.FLYING_SWOOOPER) {
                if (e.state == 0) { e.y = e.baseY + (float)Math.sin(animationClock*3.7f+e.x*.02f)*18; if (Math.abs(dx)<235 && Math.abs(py-e.y)<135) { e.state=1; e.stateTime=.42f; e.dir=dx<0?-1:1; e.targetX=px; e.targetY=py+18; audio.enemyWarning(); } }
                else if (e.state == 1) { e.stateTime -= dt; if (e.stateTime<=0) { e.state=2; e.stateTime=.34f; audio.enemySwoop(); } }
                else { e.x = approach(e.x,e.targetX,275*dt); e.y = approach(e.y,e.targetY,225*dt); e.stateTime -= dt; if (e.stateTime<=0) { e.state=0; e.baseY=e.y; } }
            } else if (e.kind == EnemyController.FAST_SKIRMISHER) {
                if (e.state == 0) { e.x += e.dir * e.speed * dt; if (e.x < e.minX || e.x > e.maxX) e.dir *= -1; if (Math.abs(dx)<210 && Math.abs(py-e.y)<90) { e.state=1; e.stateTime=.46f; e.dir=dx<0?-1:1; e.targetX=px; audio.enemyWarning(); } }
                else if (e.state == 1) { e.stateTime-=dt; if(e.stateTime<=0){e.state=2;e.stateTime=.34f;audio.enemyDash();} }
                else if (e.state == 2) { e.x=approach(e.x,e.targetX,360*dt); e.stateTime-=dt; if(e.stateTime<=0){e.state=3;e.stateTime=.24f;} }
                else { e.x -= e.dir*150*dt; e.stateTime-=dt; if(e.stateTime<=0)e.state=0; }
            } else if (e.kind == EnemyController.FROST_SENTINEL) {
                if (e.state == 0) { e.y=e.baseY+(float)Math.sin(animationClock*2.5f+e.x*.015f)*10; if(Math.abs(dx)<150&&Math.abs(py-e.y)<95){e.state=1;e.stateTime=.58f;audio.enemyWarning();} }
                else if (e.state == 1) { e.stateTime-=dt; if(e.stateTime<=0){e.state=2;e.stateTime=.22f;audio.enemySwoop();} }
                else if (e.state == 2) { if(Math.abs(px-e.x)<115&&Math.abs(py-e.y)<90) damage(2); e.stateTime-=dt; if(e.stateTime<=0)e.state=0; }
            } else if (e.kind == EnemyController.WIND_WISP) {
                if(e.state==0){e.x+=e.dir*e.speed*dt;if(e.x<e.minX||e.x>e.maxX)e.dir*=-1;if(Math.abs(dx)<260&&Math.abs(py-e.y)<125){e.state=1;e.stateTime=.38f;e.dir=dx<0?-1:1;e.targetX=px;e.targetY=py;audio.enemyWarning();}}
                else if(e.state==1){e.stateTime-=dt;if(e.stateTime<=0){e.state=2;e.stateTime=.28f;audio.enemyDash();}}
                else {e.x=approach(e.x,e.targetX,405*dt);e.y=approach(e.y,e.targetY,320*dt);e.stateTime-=dt;if(e.stateTime<=0){e.state=0;e.baseY=e.y;}}
            } else if (e.kind == EnemyController.SHIELD_GUARD) {
                if (e.state == 0) { e.x += e.dir * e.speed * dt; if (e.x < e.minX || e.x > e.maxX) e.dir *= -1; if (Math.abs(dx) < 180 && Math.abs(py-e.y)<90) { e.state=1; e.stateTime=.52f; e.dir=dx<0?-1:1; audio.enemyWarning(); } }
                else if (e.state == 1) { e.stateTime -= dt; if (e.stateTime <= 0) { e.state=2; e.stateTime=.26f; audio.enemyDash(); } }
                else { e.x += e.dir * 118 * dt; e.stateTime -= dt; if (e.stateTime <= 0) e.state=0; }
            } else if (e.kind == EnemyController.HEAVY_BRUTE) {
                if (e.state == 0) { e.x += e.dir * e.speed * dt; if (e.x < e.minX || e.x > e.maxX) e.dir *= -1; if (Math.abs(dx) < 205 && Math.abs(py-e.y)<100) { e.state=1; e.stateTime=.72f; e.dir=dx<0?-1:1; audio.enemyWarning(); } }
                else if (e.state == 1) { e.stateTime -= dt; if (e.stateTime <= 0) { e.state=2; e.stateTime=.28f; audio.enemyDash(); } }
                else { if (Math.abs(px-e.x)<92 && Math.abs(py-e.y)<95) damage(2); e.stateTime -= dt; if (e.stateTime <= 0) { e.state=3; e.stateTime=.42f; } }
                if (e.state == 3) { e.stateTime -= dt; if (e.stateTime <= 0) e.state=0; }
            } else { // Rune Caster: a long telegraph followed by one readable projectile.
                e.y = e.baseY + (float)Math.sin(animationClock*2.1f+e.x*.018f)*6;
                if (e.state == 0 && Math.abs(dx) < 315 && Math.abs(py-e.y) < 165) { e.state=1; e.stateTime=.66f; e.dir=dx<0?-1:1; e.targetX=px; e.targetY=py+18; audio.enemyWarning(); }
                else if (e.state == 1) { e.stateTime -= dt; if (e.stateTime <= 0) { e.state=2; e.stateTime=.16f; audio.enemySwoop(); } }
                else if (e.state == 2) { e.stateTime -= dt; if (e.stateTime <= 0) { float shotDx=e.targetX-e.x, shotDy=e.targetY-e.y; float length=Math.max(1f,(float)Math.sqrt(shotDx*shotDx+shotDy*shotDy)); enemyProjectiles.add(new EnemyProjectile(e.x,e.y-18,shotDx/length*265f,shotDy/length*265f)); e.state=3; e.stateTime=.58f; } }
                else if (e.state == 3) { e.stateTime -= dt; if (e.stateTime <= 0) e.state=0; }
            }
            boolean committedContactAttack = (e.kind == EnemyController.GROUND_PATROLLER
                    || e.kind == EnemyController.FLYING_SWOOOPER
                    || e.kind == EnemyController.FAST_SKIRMISHER
                    || e.kind == EnemyController.WIND_WISP
                    || e.kind == EnemyController.SHIELD_GUARD
                    || e.kind == EnemyController.HEAVY_BRUTE) && e.state == 2;
            if (Math.abs(px - e.x) < (e.kind == EnemyController.HEAVY_BRUTE ? 54 : 44) && Math.abs(py - e.y) < 48) damage(committedContactAttack ? 2 : 1);
            float reach = chargedAttack ? 152 : attackStage >= 4 ? 148 : attackStage >= 2 ? 126 : 104;
            boolean airStrikeHit = CombatSystem.canHitFromAir(airAttack, px, py, e.x, e.y, 86, 68, 82, 58);
            boolean groundStrikeHit = CombatSystem.canHitFromGround(airAttack, facingLeft, px, py, e.x, e.y, reach, 72);
            boolean shieldBlocks = e.kind == EnemyController.SHIELD_GUARD && groundStrikeHit && !airAttack
                    && ((e.dir < 0 && px < e.x) || (e.dir > 0 && px > e.x));
            if (attackTime > 0 && (groundStrikeHit || airStrikeHit) && e.hitLock <= 0) {
                if (shieldBlocks) { e.hitLock=.14f; triggerShake(2.4f,.045f); audio.impact(); }
                else { e.hp -= CombatSystem.comboDamage(attackStage, save.attackRank(), chargedAttack, counterTime > 0); energy=Math.min(maxEnergy,energy+5f); e.hurtTime = .22f; e.hitLock = .20f; hitFxTime = .18f; hitPause = chargedAttack || attackStage >= 4 ? .085f : .055f; fxX = e.x; fxY = e.y - 18; triggerSquash(false, chargedAttack || attackStage >= 4 ? .10f : .07f); triggerShake(chargedAttack || attackStage >= 4 ? 6.2f : 4.4f, chargedAttack || attackStage >= 4 ? .095f : .075f); spawnJuiceParticles(e.x, e.y - 18, Color.rgb(255, 230, 145), chargedAttack || attackStage >= 4 ? 11 : 6, chargedAttack || attackStage >= 4 ? 175f : 135f); triggerCombatCallout(e.x, e.y - 18, false); if(counterTime>0) counterTime=0; if(airAttack){vy=-430;canDouble=true;airAttack=false;} audio.impact(); }
            }
        }
        updateEnemyProjectiles(dt);
        for (Foe foe : foes) {
            if (foe.hp <= 0) {
                spawnJuiceParticles(foe.x, foe.y - 14, EnemyController.archetype(foe.kind).accentColor, 10, 145f);
            }
        }
        int remaining = foes.size();
        foes.removeIf(e -> e.hp <= 0);
        if (foes.size() < remaining) {
            save.recordEnemyDefeats(remaining - foes.size());
            audio.enemyDefeat();
        }
    }

    private void updateEnemyProjectiles(float dt) {
        Iterator<EnemyProjectile> iterator = enemyProjectiles.iterator();
        RectF player = playerRect();
        while (iterator.hasNext()) {
            EnemyProjectile projectile = iterator.next();
            projectile.life -= dt;
            projectile.x += projectile.velocityX * dt;
            projectile.y += projectile.velocityY * dt;
            if (projectile.life <= 0 || projectile.x < -30 || projectile.x > 2330 || projectile.y < -30 || projectile.y > 750) {
                iterator.remove();
            } else if (RectF.intersects(player, projectile.rect())) {
                damage(1);
                iterator.remove();
            }
        }
    }

    void updateBossInternal(float dt) {
        if (boss == null || boss.hp <= 0) return;
        if (boss.burnTime > 0) {
            boss.burnTime -= dt;
            boss.burnTick -= dt;
            if (boss.burnTick <= 0) {
                boss.hp -= PowerSystem.emberTickDamage(save.attackRank());
                boss.burnTick = PowerSystem.emberTickInterval();
                spawnJuiceParticles(boss.x, boss.y - 28, Color.rgb(255, 137, 68), 6, 105f);
            }
        }
        if (boss.frostSlowTime > 0) boss.frostSlowTime -= dt;
        float frostScale = boss.frostSlowTime > 0 ? .58f : 1f;
        BossController.Profile profile = BossController.profile(boss.world);
        int nextPhase = boss.hp <= boss.maxHp * profile.phaseThreeThreshold ? 3
                : boss.hp <= boss.maxHp * profile.phaseTwoThreshold ? 2 : 1;
        if (nextPhase != boss.phase) {
            boss.phase = nextPhase;
            boss.transitionTime = .72f;
            boss.cooldown = Math.max(boss.cooldown, .52f);
            audio.bossWarning();
            triggerShake(7.2f, .12f);
            spawnJuiceParticles(boss.x, boss.y - 34, data.accent, 15, 165f);
        }
        if (boss.transitionTime > 0) boss.transitionTime -= dt;
        float phaseSpeed = boss.phase == 3 ? profile.phaseThreeSpeed
                : boss.phase == 2 ? profile.phaseTwoSpeed : profile.phaseOneSpeed;
        boss.cooldown -= dt * frostScale; if (boss.hitLock > 0) boss.hitLock -= dt;
        boolean locomotionLocked = boss.chargeTelegraph > 0 || boss.chargeTime > 0 || boss.leapTelegraph > 0 || boss.leapTime > 0;
        if (!locomotionLocked) {
            boss.x += boss.dir * phaseSpeed * frostScale * dt;
            if (boss.x < 1740 || boss.x > 2070) boss.dir *= -1;
        }
        updateBossCharge(dt, frostScale);
        updateBossLeap(dt, frostScale);
        if (boss.rangedCooldown > 0) boss.rangedCooldown -= dt * frostScale;
        if (Math.abs(px - boss.x) < 72 && Math.abs(py - boss.y) < 75) damage(boss.chargeTime > 0 ? 2 : boss.phase);
        boolean bossAirStrike = CombatSystem.canHitFromAir(
                airAttack, px, py, boss.x, boss.y, 110, 90, 92, 70);
        boolean bossGroundStrike = CombatSystem.canHitFromGround(
                airAttack, facingLeft, px, py, boss.x, boss.y,
                chargedAttack ? 158 : attackStage >= 4 ? 152 : attackStage >= 2 ? 140 : 118, 90);
        if (attackTime > 0 && (bossGroundStrike || bossAirStrike) && boss.hitLock<=0) { boss.hp -= CombatSystem.comboDamage(attackStage, save.attackRank(), chargedAttack, counterTime > 0); boss.hitLock=.20f; hitFxTime = .18f; hitPause = chargedAttack || attackStage >= 4 ? .11f : .070f; fxX = boss.x; fxY = boss.y - 25; triggerSquash(false, chargedAttack || attackStage >= 4 ? .12f : .085f); triggerShake(chargedAttack || attackStage >= 4 ? 8.5f : 6.4f, chargedAttack || attackStage >= 4 ? .13f : .10f); spawnJuiceParticles(boss.x, boss.y - 25, Color.rgb(255, 178, 96), chargedAttack || attackStage >= 4 ? 18 : 11, chargedAttack || attackStage >= 4 ? 205f : 170f); triggerCombatCallout(boss.x, boss.y - 25, true); if(counterTime>0) counterTime=0; if(airAttack){vy=-390;canDouble=true;airAttack=false;} audio.impact(); }
        // Smarter targeting: the boss reads player distance and phase to choose between its
        // ranged, charge, leap-slam, and hazard-lane attacks instead of firing a fixed rotation.
        float distToPlayer = Math.abs(px - boss.x);
        boolean canAct = boss.chargeTelegraph <= 0 && boss.chargeTime <= 0 && boss.leapTelegraph <= 0 && boss.leapTime <= 0;
        if (canAct && boss.rangedCooldown <= 0 && distToPlayer > 420) {
            fireBossProjectile();
            boss.rangedCooldown = boss.phase == 3 ? 1.35f : boss.phase == 2 ? 1.7f : 2.2f;
        } else if (canAct && boss.cooldown <= 0) {
            audio.bossWarning();
            float target=Math.max(90,Math.min(2180,px));
            int cycle = boss.attackCycle++;
            // Close range favors the charge/leap melee attacks; mid/far range favors hazard lanes.
            boolean preferMelee = distToPlayer < 340;
            boolean useCharge = preferMelee && (boss.phase >= 2 || cycle % 3 == 0);
            boolean useLeap = preferMelee && !useCharge && (boss.phase >= 2 && cycle % 2 == 0);
            if (useCharge) {
                startBossCharge(target);
                boss.cooldown = boss.phase==3?1.35f:boss.phase==2?1.7f:2.05f;
            } else if (useLeap) {
                startBossLeap(target);
                boss.cooldown = boss.phase==3?1.45f:boss.phase==2?1.85f:2.2f;
            } else if (boss.world==1) { // Thornwold: root lanes become a wall in phase three.
                boss.cooldown=boss.phase==1?2.10f:boss.phase==2?1.42f:1.02f;
                float hx=boss.x+(boss.dir>0?60:-120);
                hazards.add(new Hazard(hx,555,hx+85,620,.78f,.42f,1));
                if(boss.phase>=2) hazards.add(new Hazard(hx+(boss.dir>0?118:-118),555,hx+(boss.dir>0?203:-33),620,.78f,.42f,1));
                if(boss.phase==3) hazards.add(new Hazard(target-58,555,target+58,620,.88f,.48f,1));
            } else if (boss.world==2) { // Akaros: sand slams spread from one lane to a quake pattern.
                boss.cooldown=boss.phase==1?1.90f:boss.phase==2?1.32f:1.00f;
                hazards.add(new Hazard(target-48,555,target+48,620,1.02f,.62f,2));
                if(boss.phase>=2) hazards.add(new Hazard(target+(cycle%2==0?108:-108)-42,555,target+(cycle%2==0?108:-108)+42,620,1.02f,.62f,2));
                if(boss.phase==3) hazards.add(new Hazard(target-175,555,target-95,620,1.02f,.62f,2));
            } else { // Vyrn: widening ice lanes and gusts force movement between safe gaps.
                boss.cooldown=boss.phase==1?1.65f:boss.phase==2?1.18f: .92f;
                hazards.add(new Hazard(target-145,555,target-55,620,.94f,.50f,3));
                hazards.add(new Hazard(target+55,555,target+145,620,.94f,.50f,3));
                if(boss.phase>=2) windTime=Math.max(windTime,.60f);
                if(boss.phase==3) { hazards.add(new Hazard(target-35,555,target+35,620,.94f,.50f,3)); windTime=Math.max(windTime,1.05f); }
            }
        }
        if (boss.hp <= 0) boss.hp = 0;
    }

    /** Telegraphed horizontal rush toward the player's lane; deals bonus contact damage mid-charge. */
    private void startBossCharge(float target) {
        boss.chargeTelegraph = .46f;
        boss.chargeVX = Math.signum(target - boss.x) * (boss.phase == 3 ? 620f : 520f);
        boss.dir = Math.signum(boss.chargeVX) != 0 ? Math.signum(boss.chargeVX) : boss.dir;
        spawnJuiceParticles(boss.x, boss.y - 20, data.accent, 8, 90f);
    }

    private void updateBossCharge(float dt, float frostScale) {
        if (boss.chargeTelegraph > 0) {
            boss.chargeTelegraph -= dt * frostScale;
            if (boss.chargeTelegraph <= 0) {
                boss.chargeTime = .62f;
                triggerShake(4.5f, .08f);
                audio.impact();
            }
        } else if (boss.chargeTime > 0) {
            boss.chargeTime -= dt * frostScale;
            boss.x += boss.chargeVX * frostScale * dt;
            boss.x = Math.max(1700, Math.min(2110, boss.x));
            if (boss.chargeTime <= 0) boss.dir = -Math.signum(boss.chargeVX);
        }
    }

    /** Telegraphed leap that slams down, spawning a wide shockwave hazard under the landing point. */
    private void startBossLeap(float target) {
        boss.leapTelegraph = .5f;
        boss.leapStartY = boss.y;
        boss.leapSlammed = false;
        spawnJuiceParticles(boss.x, boss.y - 20, data.accent, 8, 90f);
    }

    private void updateBossLeap(float dt, float frostScale) {
        if (boss.leapTelegraph > 0) {
            boss.leapTelegraph -= dt * frostScale;
            if (boss.leapTelegraph <= 0) boss.leapTime = .62f;
        } else if (boss.leapTime > 0) {
            boss.leapTime -= dt * frostScale;
            float t = 1f - Math.max(0f, boss.leapTime / .62f);
            // Simple up-then-down arc: peak at the midpoint, back to leapStartY at t=1.
            float arc = (float) Math.sin(Math.PI * t);
            boss.y = boss.leapStartY + boss.leapPeakOffset * arc;
            if (t >= .5f && !boss.leapSlammed) {
                boss.leapSlammed = true;
                boss.y = boss.leapStartY;
                float lx = boss.x;
                hazards.add(new Hazard(lx-150,555,lx+150,620,.5f,0f,boss.world));
                triggerShake(8.5f, .14f);
                triggerCombatCallout(boss.x, boss.y - 25, true);
                spawnJuiceParticles(boss.x, boss.y, data.accent, 16, 200f);
                audio.impact();
            }
            if (boss.leapTime <= 0) boss.y = boss.leapStartY;
        }
    }

    /** World-flavored ranged attack fired when the player is out of melee range. */
    private void fireBossProjectile() {
        float dx = px - boss.x, dy = (py - 22) - (boss.y - 40);
        float len = Math.max(1f, (float) Math.sqrt(dx*dx + dy*dy));
        float speed = boss.phase == 3 ? 340f : 300f;
        enemyProjectiles.add(new EnemyProjectile(boss.x, boss.y - 40, dx/len*speed, dy/len*speed));
        triggerCombatCallout(boss.x, boss.y - 60, true);
        audio.bossWarning();
    }

    private void startLevel(int id) {
        currentLevel = id; data = LevelData.get(id); screen = LEVEL;
        save.recordRunStarted();
        audio.setLevelMusic(getContext(), data.world, data.boss);
        health = 5 + save.vitalityRank() + save.relicRank(); maxHealth = health; power = Math.min(2, (id - 1) / 3); maxEnergy = 100 + save.energyRank() * 20 + save.relicRank() * 10; energy = maxEnergy;
        coinsRun = gemsRun = damageTaken = completedStars = completedCoinReward = completedGemReward = 0; newStarRecord = false; newBestTime = false; completedTimeMillis = 0; worldRestored = false; levelElapsed=0; px = 120; py = 380; vx = vy = cameraX = 0; facingLeft = false; checkpointX = 120; checkpointY = 380; checkpointMarkerX = currentLevel==1?960:currentLevel==2?1100:currentLevel==3?1290:960; checkpointMarkerY = 566; checkpointActive = false; jumpQueued = false;
        attackTime = attackDuration = comboWindow = chargeTime = counterTime = perfectDodgeTime = powerTime = invincible = hurtTime = defeatTime = swordFxTime = hitFxTime = sparkleFxTime = 0; windTime = galeBurstTime = coyoteTime = jumpBufferTime = landingPulse = hitPause = knockbackTime = 0; dashTime = dashCooldown = slideTime = dashFxTime = dodgeTime = dodgeFxTime = airStrikeFxTime = 0; screenShakeTime = screenShakeStrength = playerSquashTime = 0; playerSquashLanding = false; juiceParticles.clear(); wallSliding = false; airAttack = false; storyKind = 0; storyTime = 0; attackStage=0; comboQueued=false; attackHolding=false; chargeReady=false; chargedAttack=false; secretMessageTime=0; secretMessage=""; combatCalloutTime=0; combatCallout=""; jumpHeld = false; footstepTimer = 0; platforms.clear(); pickups.clear(); foes.clear(); hazards.clear(); windZones.clear(); heatZones.clear(); icicleSpawners.clear(); fallingIcicles.clear(); enemyProjectiles.clear(); groundMaterial = "STONE"; boss = null;
        buildLevel();
    }

    /** Builds the active stage from assets/levels/level_{id}.json. */
    private void buildLevel() {
        LevelLoader.LevelDefinition definition = LevelLoader.load(getContext(), currentLevel);
        levelDesignRole = definition.designRole;
        checkpointMarkerX = definition.checkpointX;
        checkpointMarkerY = definition.checkpointY;
        applyStoryDefinition(definition.story);

        for (LevelLoader.PlatformData platformData : definition.platforms) {
            addPlatform(platformData.x, platformData.y, platformData.width, platformData.crumble,
                    platformData.material, platformData.moveX, platformData.moveY, platformData.moveSpeed);
        }
        for (LevelLoader.PickupData pickupData : definition.pickups) {
            if (!"SECRET".equals(pickupData.route) || !save.hasSecret(pickupData.secretId)) {
                pickups.add(new Pickup(pickupData.x, pickupData.y, pickupData.gem, pickupData.route,
                        pickupData.secretId, pickupData.secretRewardGems));
            }
        }
        for (LevelLoader.FoeData foeData : definition.foes) {
            foes.add(new Foe(foeData.x, foeData.y, foeData.kind));
        }
        for (LevelLoader.HazardData hazardData : definition.hazards) {
            addStaticHazard(
                    hazardData.left,
                    hazardData.top,
                    hazardData.right,
                    hazardData.bottom);
        }
        for (LevelLoader.WindZoneData zoneData : definition.windZones) {
            windZones.add(new WindZone(zoneData.left, zoneData.top, zoneData.right, zoneData.bottom, zoneData.forceX));
        }
        for (LevelLoader.HeatZoneData zoneData : definition.heatZones) {
            heatZones.add(new HeatZone(zoneData.left, zoneData.top, zoneData.right, zoneData.bottom));
        }
        for (LevelLoader.IcicleSpawnerData spawnerData : definition.icicleSpawners) {
            icicleSpawners.add(new IcicleSpawner(
                    spawnerData.x, spawnerData.spawnY, spawnerData.landingY, spawnerData.interval));
        }
        if (definition.boss != null) {
            boss = new Boss(
                    definition.boss.x,
                    definition.boss.y,
                    definition.boss.name,
                    definition.boss.world);
        }
    }

    private void applyStoryDefinition(String story) {
        if ("boss_intro".equals(story)) {
            storyKind = 2;
            storyTime = 2.9f;
            audio.bossWarning();
        } else if ("realm_intro".equals(story)) {
            storyKind = 1;
            storyTime = 2.9f;
            audio.powerSelect();
        }
    }

    private void addPlatform(float x, float y, float width, boolean crumble, String material,
            float moveX, float moveY, float moveSpeed) {
        platforms.add(new Platform(x, y, width, y >= 600 ? 120 : 24, crumble,
                material, moveX, moveY, moveSpeed));
    }

    private void addStaticHazard(float left, float top, float right, float bottom) {
        hazards.add(new Hazard(left, top, right, bottom, 9999f, 0));
    }

    private void updateEnvironmentalSystems(float dt) {
        for (Platform platform : platforms) {
            platform.update(animationClock);
            if (grounded && Math.abs(py + 54 - platform.y) < 9
                    && px + 26 > platform.x && px - 26 < platform.x + platform.w) {
                px += platform.frameDeltaX;
            }
        }
        for (IcicleSpawner spawner : icicleSpawners) {
            spawner.timer -= dt;
            if (spawner.timer <= 0) {
                fallingIcicles.add(new FallingIcicle(spawner.x, spawner.spawnY, spawner.landingY));
                spawner.timer += spawner.interval;
            }
        }
        Iterator<FallingIcicle> iterator = fallingIcicles.iterator();
        RectF player = playerRect();
        while (iterator.hasNext()) {
            FallingIcicle icicle = iterator.next();
            icicle.velocityY = Math.min(980f, icicle.velocityY + 1450f * dt);
            icicle.y += icicle.velocityY * dt;
            if (RectF.intersects(player, icicle.rect())) {
                damage(1);
                iterator.remove();
            } else if (icicle.y >= icicle.landingY) {
                triggerShake(2.4f, .05f);
                spawnJuiceParticles(icicle.x, icicle.landingY, Color.rgb(164, 233, 255), 5, 80f);
                iterator.remove();
            }
        }
    }

    private void applyWindForces(float dt) {
        float centerX = px;
        float centerY = py + 27;
        for (WindZone zone : windZones) {
            if (zone.contains(centerX, centerY)) {
                vx = Math.max(-720f, Math.min(720f, vx + zone.forceX * dt));
            }
        }
    }

    private void applyHeatZones(RectF player) {
        for (HeatZone zone : heatZones) {
            if (RectF.intersects(player, zone.rect())) {
                damage(1);
                return;
            }
        }
    }

    private void triggerShake(float strength, float duration) {
        if (!JUICE_ENABLED) {
            return;
        }
        screenShakeStrength = Math.max(screenShakeStrength, strength);
        screenShakeTime = Math.max(screenShakeTime, duration);
    }

    private void triggerSquash(boolean fromLanding, float duration) {
        if (!JUICE_ENABLED) {
            return;
        }
        playerSquashLanding = fromLanding;
        playerSquashTime = Math.max(playerSquashTime, duration);
    }

    private void triggerCombatCallout(float x, float y, boolean bossHit) {
        if (chargedAttack) combatCallout = "CHARGED STRIKE";
        else if (attackStage >= 4) combatCallout = "FINISHER";
        else if (counterTime > 0) combatCallout = "COUNTER";
        else return;
        combatCalloutX = x;
        combatCalloutY = y;
        combatCalloutTime = bossHit ? .56f : .42f;
    }

    private void spawnJuiceParticles(float x, float y, int color, int count, float speed) {
        if (!JUICE_ENABLED) {
            return;
        }
        for (int index = 0; index < count; index++) {
            if (juiceParticles.size() >= 72) {
                juiceParticles.remove(0);
            }
            float angle = (float) (rng.nextFloat() * Math.PI * 2);
            float magnitude = speed * (.42f + rng.nextFloat() * .72f);
            juiceParticles.add(new JuiceParticle(
                    x,
                    y,
                    (float) Math.cos(angle) * magnitude,
                    (float) Math.sin(angle) * magnitude - 42f,
                    .22f + rng.nextFloat() * .18f,
                    2.2f + rng.nextFloat() * 2.8f,
                    color));
        }
    }

    private void updateJuiceParticles(float dt) {
        if (!JUICE_ENABLED) {
            juiceParticles.clear();
            return;
        }
        Iterator<JuiceParticle> iterator = juiceParticles.iterator();
        while (iterator.hasNext()) {
            JuiceParticle particle = iterator.next();
            particle.life -= dt;
            if (particle.life <= 0) {
                iterator.remove();
                continue;
            }
            particle.x += particle.vx * dt;
            particle.y += particle.vy * dt;
            particle.vy += 470f * dt;
        }
    }

    private boolean jump() {
        if (screen != LEVEL) return false;
        if (wallSliding) {
            boolean atLedge = py + 54 >= wallTop - 10 && py + 54 <= wallTop + 34;
            if (atLedge) {
                px += wallDir * 50;
                py = wallTop - 54;
                vx = wallDir * 175;
                vy = 0;
                grounded = true;
                wallSliding = false;
                landingPulse = .10f;
                audio.jump(true);
                return true;
            }
            vx = wallDir * 360;
            vy = -620;
            wallSliding = false;
            canDouble = true;
            audio.jump(true);
            return true;
        }
        if (grounded || coyoteTime > 0) { vy = -680; grounded = false; coyoteTime = 0; canDouble = true; audio.jump(false); return true; }
        if (canDouble) { vy = -635; canDouble = false; audio.jump(true); return true; }
        return false;
    }

    private void directionTap(int direction) {
        if (screen != LEVEL) return;
        long now = System.nanoTime();
        long previous = direction < 0 ? lastLeftTap : lastRightTap;
        if (now - previous < 260_000_000L) startDash(direction);
        if (direction < 0) lastLeftTap = now; else lastRightTap = now;
    }

    private void startDash(int direction) {
        if (!grounded || dashCooldown > 0 || dashTime > 0 || knockbackTime > 0 || attackTime > 0 || powerTime > 0) return;
        facingLeft = direction < 0;
        audio.playerDash();
        dashTime = .17f;
        dodgeTime = .19f;
        dashCooldown = .52f;
        dashFxTime = .24f;
        slideTime = 0;
        vx = direction * 650f;
    }

    void pressAttack() {
        if (screen == LEVEL) {
            attackHolding = true;
            chargeTime = 0;
            chargeReady = false;
        }
    }

    void releaseAttack() {
        if (screen != LEVEL) return;
        attackHolding = false;
        if (chargeReady && attackTime <= 0) {
            chargedAttack = true;
            chargeReady = false;
            chargeTime = 0;
            beginAttack(1);
        } else {
            chargeReady = false;
            chargeTime = 0;
            strike();
        }
    }

    void cancelAttackHold() {
        attackHolding = false;
        chargeReady = false;
        chargeTime = 0;
    }

    private void strike() {
        if (screen != LEVEL) return;
        if (attackTime > 0 && attackStage > 0 && attackStage < 4 && comboWindow > 0 && !chargedAttack) {
            comboQueued = true;
            return;
        }
        if (attackTime <= 0) {
            chargedAttack = false;
            beginAttack(1);
        }
    }

    private void beginAttack(int stage) {
        airAttack = !grounded && !wallSliding;
        attackStage = stage;
        attackDuration = airAttack ? .34f : (stage == 4 ? .40f : stage == 3 ? .34f : stage == 2 ? .29f : chargedAttack ? .36f : .22f);
        attackTime = attackDuration;
        comboWindow = !airAttack && !chargedAttack && stage < 4 ? attackDuration : 0;
        comboQueued = false;
        swordFxTime = airAttack ? .28f : (chargedAttack ? .34f : .20f);
        if (airAttack) {
            airStrikeFxTime = .36f;
            audio.airStrike();
        }
        audio.attack();
    }
    private void usePower() {
        if (screen != LEVEL || powerTime > 0) return;
        float cost = PowerSystem.energyCost(power, save.relicRank());
        if (energy < cost) { audio.powerFail(); return; }
        energy -= cost;
        powerTime = PowerSystem.castDuration(power);
        applySelectedPower();
        audio.powerCast(power);
        audio.power();
    }

    private void applySelectedPower() {
        int applied = 0;
        for (Foe foe : foes) {
            if (Math.abs(px - foe.x) > 175 || Math.abs(py - foe.y) > 120) continue;
            if (power == PowerSystem.EMBER) {
                foe.burnTime = Math.max(foe.burnTime, PowerSystem.emberDuration());
                foe.burnTick = 0;
                spawnJuiceParticles(foe.x, foe.y - 16, Color.rgb(255, 136, 68), 7, 115f);
            } else if (power == PowerSystem.FROST) {
                foe.frozen = Math.max(foe.frozen, PowerSystem.frostDuration());
                foe.hurtTime = .18f;
                spawnJuiceParticles(foe.x, foe.y - 16, Color.rgb(144, 231, 255), 7, 105f);
            } else {
                float deltaX = foe.x - px;
                foe.x += PowerSystem.galeKnockback(deltaX) * .16f;
                foe.state = 0;
                spawnJuiceParticles(foe.x, foe.y - 16, Color.rgb(170, 247, 236), 6, 125f);
            }
            applied++;
        }
        if (power == PowerSystem.GALE) {
            galeBurstTime = PowerSystem.galeBurstDuration();
            vy = Math.min(vy, -455);
            windTime = 1.65f;
            enemyProjectiles.clear();
        }
        if (boss != null && boss.hp > 0 && Math.abs(px - boss.x) < 205 && Math.abs(py - boss.y) < 145) {
            if (power == PowerSystem.EMBER) {
                boss.burnTime = Math.max(boss.burnTime, PowerSystem.emberDuration());
                boss.burnTick = 0;
            } else if (power == PowerSystem.FROST) {
                boss.frostSlowTime = Math.max(boss.frostSlowTime, 2.3f);
            } else {
                boss.x += PowerSystem.galeKnockback(boss.x - px) * .08f;
                boss.cooldown += .25f;
            }
            applied++;
        }
        if (applied == 0 && power == PowerSystem.EMBER) {
            spawnJuiceParticles(px + (facingLeft ? -66 : 66), py + 6, Color.rgb(255, 136, 68), 8, 115f);
        }
    }
    private void cyclePower() {
        if (screen == LEVEL) {
            power = (power + 1) % 3;
            audio.powerSelect();
        }
    }
    private void damage(int amount) {
        if (dodgeTime > 0) {
            if (CombatSystem.isPerfectDodge(dodgeTime) && dodgeFxTime <= 0) {
                perfectDodgeTime = Math.max(perfectDodgeTime, .26f);
                counterTime = Math.max(counterTime, .72f);
                energy = Math.min(maxEnergy, energy + 16f);
                triggerShake(3.8f, .07f);
                spawnJuiceParticles(px, py + 24, Color.rgb(132, 242, 255), 12, 150f);
            }
            if (dodgeFxTime <= 0) {
                dodgeFxTime = .16f;
                audio.playerDash();
            }
            return;
        }
        if (invincible > 0 || screen != LEVEL) {
            return;
        }
        triggerShake(amount >= 2 ? 6.5f : 4.2f, amount >= 2 ? .11f : .08f);
        spawnJuiceParticles(px, py + 22, Color.rgb(255, 95, 93), amount >= 2 ? 9 : 5, 115f);
        health -= amount;
        damageTaken += amount;
        invincible = .9f;
        hurtFlash = .18f;
        hurtTime = health > 0 ? .28f : 0;
        hitPause = .045f;
        knockbackTime = .16f;
        vx = facingLeft ? 235 : -235;
        vy = Math.min(vy, -245);
        audio.hurt();
        if (health <= 0) {
            save.recordDeath();
            defeatTime = 0;
            screen = GAMEOVER;
            audio.defeat();
        }
    }
    private void respawn() { health = Math.max(1, maxHealth - 1); px = checkpointX; py = checkpointY; vx = vy = 0; invincible = 1.4f; audio.respawn(); }
    private void completeLevel() {
        if (screen != LEVEL) return;
        completedStars = 1 + (gemsRun > 0 ? 1 : 0) + (damageTaken == 0 ? 1 : 0);
        completedCoinReward = coinsRun + 10 + completedStars * 3;
        completedGemReward = gemsRun;
        worldRestored = data.boss;
        completedTimeMillis = Math.max(1L, (long) (levelElapsed * 1000f));
        SaveManager.CompletionResult completion = save.recordLevelCompletion(currentLevel, completedTimeMillis, data.boss);
        newBestTime = completion.newBestTime;
        save.addRewards(completedCoinReward, completedGemReward);
        newStarRecord = save.recordLevelStars(currentLevel, completedStars);
        save.unlockAfter(currentLevel);
        screen = COMPLETE;
        audio.win();
        if (newStarRecord) audio.upgrade();
    }

    void drawSplash(Canvas c) {
        c.drawColor(Color.rgb(8,27,35));
        if (splashArt != null) c.drawBitmap(splashArt, null, new RectF(0, 0, VW, VH), p);
        overlay(c, Color.argb(110, 3, 12, 20));
        centered(c, "LEGENDS", 190, 78, Color.WHITE); centered(c, "OF THE LOST REALMS", 255, 30, Color.rgb(145,244,218));
        centered(c, "A realm awaits its guardian", 630, 20, Color.WHITE);
    }

    void drawMenu(Canvas c) {
        if (splashArt != null) c.drawBitmap(splashArt, null, new RectF(0,0,VW,VH), p);
        overlay(c, Color.argb(130, 4, 18, 26));
        c.drawRoundRect(50, 66, 610, 660, 30, 30, color(Color.argb(200, 8, 26, 36)));
        text(c, "LEGENDS", 90, 160, 70, Color.WHITE); text(c, "OF THE LOST REALMS", 92, 205, 27, Color.rgb(121,242,211));
        text(c, "The Heart of Realms has shattered.", 92, 255, 20, Color.rgb(220,240,232));
        text(c, "Guide Aster through three corrupted worlds.", 92, 285, 18, Color.rgb(190,220,215));
        button(c, 92, 335, 355, 405, "PLAY", Color.rgb(42,170,135));
        button(c, 92, 425, 355, 485, "UPGRADES", Color.rgb(55,107,144));
        button(c, 92, 505, 355, 565, "SETTINGS", Color.rgb(76,92,118));
        text(c, "Progress saved locally", 92, 620, 16, Color.rgb(190,216,209));
        badge(c, 1055, 72, "10 LEVELS"); badge(c, 1055, 118, "3 WORLDS");
        button(c, 1060, 600, 1235, 660, "DEV TOOLS", Color.rgb(82, 72, 126));
    }

    void drawMap(Canvas c) {
        c.drawColor(Color.rgb(30, 51, 50));
        if (mapArt != null) c.drawBitmap(mapArt, null, new RectF(0,0,VW,VH), p);
        overlay(c, Color.argb(40, 6,20,23));
        panel(c, 32, 22, 615, 92); text(c, "WORLD MAP", 58, 68, 30, Color.WHITE); text(c, "Select an unlocked realm gate  •  Stars unlock Realm Relic", 246, 65, 16, Color.rgb(225,238,225));
        button(c, 1100, 28, 1235, 82, "MENU", Color.rgb(52,78,98));
        float[][] nodes = {{160,478},{265,395},{350,310},{455,255},{570,320},{685,390},{790,450},{920,385},{1030,300},{1135,210}};
        for (int i=0;i<nodes.length;i++) {
            boolean unlocked = i+1 <= save.unlockedLevel(); LevelData level = LevelData.get(i+1); int world = level.world;
            int fill = unlocked ? (world==1?Color.rgb(79,210,151):world==2?Color.rgb(243,166,70):Color.rgb(103,208,244)) : Color.rgb(79,79,86);
            p.setColor(Color.argb(220, 12,25,35)); c.drawCircle(nodes[i][0]+3,nodes[i][1]+4,30,p); p.setColor(fill); c.drawCircle(nodes[i][0],nodes[i][1],27,p);
            centeredAt(c, unlocked ? String.valueOf(i+1) : "•", nodes[i][0], nodes[i][1]+8, 22, Color.WHITE);
            drawProgressStars(c, nodes[i][0], nodes[i][1]+40, save.levelStars(i+1));
            String nodeStatus = !unlocked ? "LOCKED" : save.levelStars(i+1) > 0 ? "CLEARED" : "NEW";
            centeredAt(c, nodeStatus, nodes[i][0], nodes[i][1] + 62, 10, unlocked ? Color.rgb(228,244,237) : Color.rgb(180,180,190));
            if (unlocked && save.bestTimeMillis(i+1) > 0) centeredAt(c, SaveManager.formatDuration(save.bestTimeMillis(i+1)), nodes[i][0], nodes[i][1] - 40, 10, Color.rgb(230,241,245));
        }
        panel(c, 40, 590, 730, 687); text(c, "Coins: " + save.coins() + "   Gems: " + save.gems() + "   Realm Stars: " + save.totalStars() + " / 30", 68, 628, 18, Color.WHITE); text(c, "Secrets: " + save.secretsFound() + " / 3   •   Best times are shown above cleared gates", 68, 656, 15, Color.rgb(202,224,233));
        text(c, "Verdant Kingdom", 68, 670, 16, Color.rgb(121,242,184)); text(c, "Burning Dunes", 255, 670, 16, Color.rgb(255,194,102)); text(c, "Frozen Peaks", 420, 670, 16, Color.rgb(158,228,255));
    }

    void drawUpgrades(Canvas c) {
        c.drawColor(Color.rgb(10, 28, 41)); drawStars(c, 80, Color.rgb(58,104,137));
        panel(c, 50, 35, 1230, 110); text(c, "ASTER'S UPGRADES", 82, 82, 34, Color.WHITE); text(c, "Coins: " + save.coins(), 790, 72, 20, Color.rgb(255,222,112)); text(c, "Gems: " + save.gems(), 990, 72, 20, Color.rgb(126,220,255));
        upgradeCard(c, 55, 110, "EMBER BLADE", "Increase basic attack power", "attack", save.attackRank(), Color.rgb(229,112,65));
        upgradeCard(c, 350, 110, "HEARTWARD", "Increase maximum health", "vitality", save.vitalityRank(), Color.rgb(90,202,169));
        upgradeCard(c, 645, 110, "GALE STEP", "Increase movement speed", "wind", save.windRank(), Color.rgb(87,180,238));
        upgradeGemCard(c, 940, 110, "REALM FOCUS", "Increase maximum power energy", save.energyRank(), Color.rgb(126,211,255));
        upgradeStarCard(c, 300, 495);
        button(c, 80, 615, 235, 675, "BACK", Color.rgb(63,91,115));
    }

    void drawSettings(Canvas c) {
        c.drawColor(Color.rgb(13,31,43));
        drawStars(c, 55, Color.rgb(51,109,131));
        panel(c, 230, 70, 1050, 670);
        centered(c, "SETTINGS", 135, 40, Color.WHITE);
        text(c, "Choose how the realms sound on your device.", 432, 200, 20, Color.rgb(181,209,213));
        button(c, 330, 240, 620, 310, "MUSIC: " + (save.musicEnabled()?"ON":"OFF"), save.musicEnabled()?Color.rgb(55,151,133):Color.rgb(84,88,106));
        button(c, 660, 240, 950, 310, "EFFECTS: " + (save.sfxEnabled()?"ON":"OFF"), save.sfxEnabled()?Color.rgb(75,137,188):Color.rgb(84,88,106));
        text(c, "Landscape touch controls are designed for play with both thumbs.", 330, 362, 18, Color.rgb(181,209,213));
        text(c, "Music: " + (save.musicEnabled()?"active":"muted") + "   •   Effects: " + (save.sfxEnabled()?"active":"muted"), 470, 400, 16, Color.rgb(151,222,218));
        centered(c, "CAREER STATS", 444, 17, Color.rgb(255,225,125));
        centered(c, "Clears: " + save.runsCompleted() + "   •   Defeated: " + save.enemiesDefeated() + "   •   Deaths: " + save.totalDeaths(), 472, 16, Color.rgb(197,227,232));
        centered(c, "Bosses: " + save.bossesDefeated() + "   •   Secret caches: " + save.secretsFound() + " / 3", 498, 16, Color.rgb(220,198,255));
        button(c, 470, 525, 810, 580, "RESET SAVE DATA", Color.rgb(158,68,68));
        button(c, 470, 595, 810, 650, "BACK", Color.rgb(58,111,133));
    }

    void drawDevTools(Canvas c) {
        c.drawColor(Color.rgb(17, 25, 44));
        drawStars(c, 48, Color.rgb(106, 84, 168));
        panel(c, 125, 45, 1155, 675);
        centered(c, "DEVELOPER TOOLS", 108, 38, Color.rgb(210, 192, 255));
        centered(c, "Quick validation tools — not part of the normal play flow", 142, 17, Color.rgb(193, 211, 230));
        for (int index = 0; index < 10; index++) {
            float x = 180 + (index % 5) * 185;
            float y = index < 5 ? 185 : 270;
            button(c, x, y, x + 150, y + 58, "LEVEL " + (index + 1), Color.rgb(64, 111, 146));
        }
        button(c, 210, 385, 520, 445, "STATS: " + (devStatsOverlay ? "ON" : "OFF"), devStatsOverlay ? Color.rgb(55, 151, 133) : Color.rgb(84, 88, 106));
        button(c, 560, 385, 870, 445, "COORDS: " + (devCoordinatesOverlay ? "ON" : "OFF"), devCoordinatesOverlay ? Color.rgb(75, 137, 188) : Color.rgb(84, 88, 106));
        centered(c, "Save schema v" + save.schemaVersion() + "   •   Clears: " + save.runsCompleted() + "   •   Deaths: " + save.totalDeaths(), 492, 17, Color.rgb(195, 223, 231));
        button(c, 210, 525, 520, 585, "CLEAR SAVE", Color.rgb(160, 70, 76));
        button(c, 650, 525, 960, 585, "BACK TO MENU", Color.rgb(62, 100, 132));
    }

    private void drawDevOverlay(Canvas c) {
        float bottom = devStatsOverlay && devCoordinatesOverlay ? 226 : (devStatsOverlay ? 178 : 152);
        panel(c, 16, 104, 470, bottom);
        if (devStatsOverlay) {
            text(c, "DEV  •  Runs " + save.runsStarted() + " / " + save.runsCompleted() + "  •  Deaths " + save.totalDeaths(), 32, 132, 15, Color.rgb(255, 226, 137));
            text(c, "Foes " + save.enemiesDefeated() + "  •  Coins " + save.coinsCollected() + "  •  Gems " + save.gemsCollected(), 32, 158, 15, Color.rgb(201, 227, 236));
        }
        if (devCoordinatesOverlay) {
            float y = devStatsOverlay ? 202 : 132;
            text(c, "POS " + (int) px + "," + (int) py + "  •  VEL " + (int) vx + "," + (int) vy + "  •  CAM " + (int) cameraX, 32, y, 15, Color.rgb(164, 238, 229));
        }
    }

    void drawLevel(Canvas c) {
        drawWorld(c);
        if (screen == LEVEL || screen == PAUSE || screen == COMPLETE || screen == GAMEOVER) {
            drawPlatforms(c); drawPickups(c); drawFoes(c); drawBoss(c); drawPlayer(c); drawEffects(c); drawHud(c);
            if (screen == LEVEL && (devStatsOverlay || devCoordinatesOverlay)) drawDevOverlay(c);
        }
        if (screen == LEVEL && storyTime > 0) drawStoryOverlay(c);
        if (screen == PAUSE) { overlay(c, Color.argb(175, 5,12,20)); panel(c, 420,135,860,600); centered(c,"PAUSED",205,38,Color.WHITE); centered(c,data.title,242,18,Color.rgb(190,224,222)); drawPauseProgress(c); button(c,490,285,790,345,"RESUME",Color.rgb(46,162,137)); button(c,490,375,790,435,"RESTART LEVEL",Color.rgb(153,98,72)); button(c,490,465,790,525,"WORLD MAP",Color.rgb(65,98,130)); }
        if (screen == COMPLETE) drawCompleteOverlay(c);
        if (screen == GAMEOVER) { overlay(c, Color.argb(175, 19,8,22)); panel(c,385,170,895,540); centered(c,"ASTER HAS FALLEN",245,36,Color.WHITE); centered(c,"The realm calls you back to the last checkpoint.",300,18,Color.rgb(225,208,226)); button(c,465,370,655,430,"RETRY",Color.rgb(53,151,133)); button(c,675,370,820,430,"MAP",Color.rgb(71,88,120)); }
    }

    private void drawCompleteOverlay(Canvas c) {
        overlay(c, Color.argb(170, 5,14,20));
        panel(c, 320, 130, 960, 600);
        centered(c, worldRestored ? "REALM RESTORED" : "LEVEL COMPLETE", 195, 40, Color.rgb(255,232,128));
        centered(c, data.title, 234, 24, Color.WHITE);
        centered(c, "Rewards secured: " + completedCoinReward + " coins  •  " + completedGemReward + " realm gems", 274, 19, Color.rgb(193,237,220));
        centered(c, "Objectives: Finish  •  Explorer  •  Untouched", 308, 16, Color.rgb(184,215,229));
        centered(c, "Career secrets: " + save.secretsFound() + " / 3", 332, 14, Color.rgb(221, 191, 255));
        drawProgressStars(c, 640, 360, completedStars);
        centered(c, "Time: " + SaveManager.formatDuration(completedTimeMillis), 396, 19, Color.WHITE);
        centered(c, newBestTime ? "NEW BEST TIME" : "Best: " + SaveManager.formatDuration(save.bestTimeMillis(currentLevel)), 426, 16, newBestTime ? Color.rgb(150,238,255) : Color.rgb(200,220,228));
        centered(c, newStarRecord ? "NEW STAR RECORD" : completedStars + " / 3 Realm Stars earned", 456, 17, newStarRecord ? Color.rgb(150,238,255) : Color.rgb(255,223,109));
        if (worldRestored) centered(c, worldConclusion(), 482, 15, Color.rgb(199,229,225));
        button(c, 440, 500, 665, 565, currentLevel < 10 ? "NEXT LEVEL" : "WORLD MAP", Color.rgb(45,163,137));
        button(c, 690, 500, 850, 565, "MAP", Color.rgb(65,95,122));
    }

    private String worldConclusion() {
        if (data.world == 1) return "The Grove breathes again. A burning shard calls from the western dunes.";
        if (data.world == 2) return "The ruins fall silent. A cold light rises from the northern peaks.";
        return "The Heart of Realms beats as one. Aster has brought the lost realms home.";
    }

    private void drawStoryOverlay(Canvas c) {
        float fade = Math.min(1f, Math.min(storyTime / .30f, (2.9f-storyTime) / .30f));
        overlay(c, Color.argb((int)(175*fade), 4, 12, 22));
        panel(c, 230, 185, 1050, 500);
        String title, line1, line2;
        if (storyKind == 2) {
            if (data.world == 1) { title="THORNWOLD AWAKENS"; line1="The Elder Grove's guardian mistakes Aster for the corruption."; line2="Restore the Heart fragment before its roots consume the grove."; }
            else if (data.world == 2) { title="AKAROS STIRS"; line1="Stone and sand answer the warden's rage beneath the ruined temple."; line2="Read the marked ground, then strike between the tremors."; }
            else { title="VYRN'S LAST BREATH"; line1="The Icebound Maw guards the final shard inside the storm."; line2="Climb above the frost lanes and let the wind carry Aster forward."; }
        } else {
            if (data.world == 1) { title="THE VERDANT KINGDOM"; line1="Aster follows the fractured Heart's glow into a grove that has forgotten spring."; line2="Every shard restored will call the lost realms back to life."; }
            else if (data.world == 2) { title="THE BURNING DUNES"; line1="A shard burns beneath the dunes, beyond ruins that move with the wind."; line2="Keep moving: the sand remembers every step."; }
            else { title="THE FROZEN PEAKS"; line1="The final shard shines beyond Frostwind Climb, where the mountain never sleeps."; line2="The cold tests resolve, but the Heart still answers Aster's call."; }
        }
        float pulse=1f+.10f*(float)Math.sin(animationClock*4.5f);
        drawImageTransform(c,gemNew,null,new RectF(586,202,694,310),animationClock*34f,pulse,pulse);
        centered(c, storyKind==2?"THE GUARDIAN SPEAKS":"THE HEART OF REALMS", 214, 13, Color.rgb(157,230,226));
        centered(c, title, 330, 34, data.accent);
        centered(c, line1, 390, 19, Color.WHITE);
        centered(c, line2, 426, 19, Color.rgb(201,222,232));
        int hintAlpha=(int)(150+80*Math.abs(Math.sin(animationClock*3.4f)));
        centered(c, "TAP TO CONTINUE", 476, 15, Color.argb(hintAlpha,157,230,226));
    }

    private void drawWorld(Canvas c) {
        p.setAlpha(255);
        if (data.world == 1) drawVerdantLevelBackdrop(c);
        else {
            Bitmap background = data.world == 2 ? dunesArt : frozenArt;
            if (background != null) {
                float parallax = cameraX * 0.08f;
                drawImage(c, background, null, new RectF(-parallax, 0, VW + 35 - parallax, VH));
                overlay(c, Color.argb(92, 4, 16, 28));
            } else c.drawColor(data.sky);
        }
        drawWorldAtmosphere(c);
        p.setColor(Color.argb(110, 4, 17, 24)); c.drawRect(0, 570, VW, VH, p);
        if (hurtFlash>0) overlay(c, Color.argb(80,255,50,45));
    }

    private void drawVerdantLevelBackdrop(Canvas c) {
        c.drawColor(Color.rgb(14, 67, 52));
        float parallax=cameraX*.022f;
        drawImage(c,verdantWaterfallBackdrop,null,new RectF(-parallax,0,VW+28-parallax,VH));
        overlay(c, Color.argb(12, 3, 17, 20));
        for(int i=0;i<12;i++){float mx=(i*113+animationClock*(13+i%3*4))%(VW+80)-40, my=110+(i*67%390)+(float)Math.sin(animationClock*1.6f+i)*13; p.setColor(Color.argb(28+(i%3)*13,255,240,164));c.drawCircle(mx,my,2+(i%2),p);}
    }

    private void drawWorldAtmosphere(Canvas c) {
        float far = cameraX * .018f;
        float mid = cameraX * .055f;
        float near = cameraX * .13f;
        if (data.world == 1) {
            for (int index = 0; index < 9; index++) {
                float x = (index * 163 - near + animationClock * (5 + index % 3 * 2)) % (VW + 150) - 75;
                float y = 120 + (index * 71 % 330);
                p.setColor(Color.argb(38 + index % 3 * 14, 210, 255, 188));
                c.drawCircle(x, y + (float)Math.sin(animationClock * 1.8f + index) * 8, 2.5f + index % 2, p);
            }
        } else if (data.world == 2) {
            drawDuneLayer(c, far, 408, Color.rgb(126, 66, 49), 118);
            drawDuneLayer(c, mid, 494, Color.rgb(186, 96, 53), 142);
            for (int index = 0; index < 18; index++) {
                float x = (index * 91 - near + animationClock * (22 + index % 4 * 5)) % (VW + 110) - 55;
                float y = 145 + (index * 53 % 390);
                p.setColor(Color.argb(34 + index % 3 * 12, 255, 222, 139));
                c.drawCircle(x, y + (float)Math.sin(animationClock * 2.1f + index) * 5, 1.5f + index % 2, p);
            }
        } else {
            drawMountainLayer(c, far, 375, Color.rgb(62, 100, 158), 128);
            drawMountainLayer(c, mid, 475, Color.rgb(92, 142, 194), 152);
            for (int index = 0; index < 24; index++) {
                float x = (index * 67 - near + animationClock * (7 + index % 3 * 2)) % (VW + 100) - 50;
                float y = 80 + (index * 89 % 450);
                p.setColor(Color.argb(58 + index % 3 * 16, 224, 249, 255));
                c.drawCircle(x, y + (float)Math.sin(animationClock * 1.2f + index) * 7, 1.6f + index % 2, p);
            }
        }
    }

    private void drawDuneLayer(Canvas c, float offset, float baseY, int color, int alpha) {
        p.setColor(Color.argb(alpha, Color.red(color), Color.green(color), Color.blue(color)));
        for (int index = -1; index < 5; index++) {
            float center = index * 340 - offset % 340;
            Path dune = new Path();
            dune.moveTo(center - 230, VH);
            dune.quadTo(center - 90, baseY - 95, center + 35, baseY - 28);
            dune.quadTo(center + 155, baseY + 22, center + 255, VH);
            dune.close();
            c.drawPath(dune, p);
        }
    }

    private void drawMountainLayer(Canvas c, float offset, float baseY, int color, int alpha) {
        p.setColor(Color.argb(alpha, Color.red(color), Color.green(color), Color.blue(color)));
        for (int index = -1; index < 6; index++) {
            float peak = index * 275 - offset % 275;
            c.drawPath(triangle(peak, baseY - 178, peak - 178, VH, peak + 178, VH), p);
            p.setColor(Color.argb(Math.max(25, alpha - 48), 218, 243, 255));
            c.drawPath(triangle(peak, baseY - 178, peak - 48, baseY - 64, peak + 17, baseY - 78), p);
            p.setColor(Color.argb(alpha, Color.red(color), Color.green(color), Color.blue(color)));
        }
    }

    private void drawEnvironmentalZones(Canvas c) {
        for (WindZone zone : windZones) {
            float left = zone.left - cameraX;
            float right = zone.right - cameraX;
            if (right < 0 || left > VW) continue;
            int color = data.world == 2 ? Color.rgb(255, 208, 104) : Color.rgb(145, 232, 255);
            p.setColor(Color.argb(34, Color.red(color), Color.green(color), Color.blue(color)));
            c.drawRoundRect(left, zone.top, right, zone.bottom, 16, 16, p);
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(2f);
            p.setColor(Color.argb(105, Color.red(color), Color.green(color), Color.blue(color)));
            float arrow = zone.forceX > 0 ? 1f : -1f;
            for (float y = zone.top + 34; y < zone.bottom; y += 72) {
                float x = left + 18 + (float)Math.sin(animationClock * 3.2f + y * .03f) * 8;
                c.drawLine(x, y, x + 30 * arrow, y, p);
                c.drawLine(x + 30 * arrow, y, x + 21 * arrow, y - 6, p);
                c.drawLine(x + 30 * arrow, y, x + 21 * arrow, y + 6, p);
            }
            p.setStyle(Paint.Style.FILL);
        }
        for (HeatZone zone : heatZones) {
            float left = zone.left - cameraX;
            float right = zone.right - cameraX;
            if (right < 0 || left > VW) continue;
            p.setColor(Color.argb(92, 255, 112, 48));
            c.drawRoundRect(left, zone.top, right, zone.bottom, 10, 10, p);
            for (float x = left + 10; x < right; x += 20) {
                float flame = 6 + 4 * (float)Math.sin(animationClock * 5f + x * .09f);
                p.setColor(Color.argb(155, 255, 204, 88));
                c.drawCircle(x, zone.top + 6 - flame, 3.5f, p);
            }
        }
        for (IcicleSpawner spawner : icicleSpawners) {
            float x = spawner.x - cameraX;
            if (x > -100 && x < VW + 100) {
                Rect iceSpikeSource = new Rect(2 * 384, 0, 3 * 384, 384);
                drawImage(c, hangingIceSpikesMotionSheet, iceSpikeSource, new RectF(x - 82, spawner.spawnY - 8, x + 82, spawner.spawnY + 158));
                float pulse = 7 + 2 * (float)Math.sin(animationClock * 4f + spawner.x);
                p.setColor(Color.argb(70, 175, 240, 255));
                c.drawCircle(x, spawner.spawnY + 12, pulse, p);
            }
        }
        for (FallingIcicle icicle : fallingIcicles) {
            float x = icicle.x - cameraX;
            if (x < -40 || x > VW + 40) continue;
            p.setColor(Color.rgb(174, 239, 255));
            c.drawPath(triangle(x, icicle.y - 20, x - 10, icicle.y + 16, x + 10, icicle.y + 16), p);
        }
    }

    private void drawPlatforms(Canvas c) {
        drawEnvironmentalZones(c);
        Rect platformArt = data.world == 2 ? new Rect(500, 200, 1024, 707) : new Rect(493, 547, 1024, 1000);
        for (Platform pl: platforms) {
            float x=pl.x-cameraX; if(x+pl.w<0||x>VW)continue;
            float bob = pl.crumble ? 0 : (float)Math.sin(animationClock * 1.45f + pl.x * .012f) * 1.8f;
            if (pl.h < 80 && "ICE".equals(pl.material)) {
                drawUploadedFloatingPlatform(c, icePlatformMotionSheet, pl, x, bob);
            } else if (pl.h < 80 && "SAND".equals(pl.material)) {
                drawUploadedFloatingPlatform(c, goldenPlatformMotionSheet, pl, x, bob);
            } else if(data.world==1) {
                Bitmap art=pl.h>=80?jungleGroundPlatform:jungleFloatingPlatform;
                float top=pl.y-(pl.h>=80?46:31)+bob, bottom=pl.y+(pl.h>=80?94:66)+bob;
                drawImage(c,art,null,new RectF(x-12,top,x+pl.w+12,bottom));
            } else drawImage(c, worldProps, platformArt, new RectF(x, pl.y-34+bob, x+pl.w, pl.y+Math.max(48, pl.h)+bob));
            if ("ICE".equals(pl.material)) {
                int iceColor = theme().iceMaterial;
                p.setColor(Color.argb(118, Color.red(iceColor), Color.green(iceColor), Color.blue(iceColor)));
                c.drawRoundRect(x + 4, pl.y - 6 + bob, x + pl.w - 4, pl.y + 2 + bob, 4, 4, p);
            } else if ("SAND".equals(pl.material) && pl.h < 80) {
                int sandColor = theme().sandMaterial;
                p.setColor(Color.argb(88, Color.red(sandColor), Color.green(sandColor), Color.blue(sandColor)));
                c.drawCircle(x + pl.w * .22f, pl.y + 4 + bob, 3, p);
                c.drawCircle(x + pl.w * .76f, pl.y + 7 + bob, 2.5f, p);
            }
            if (pl.moveX != 0 || pl.moveY != 0) {
                p.setColor(Color.argb(180, 117, 242, 218));
                c.drawCircle(x + pl.w / 2, pl.y + 12 + bob, 5, p);
            }
            if(pl.crumble) { p.setColor(Color.argb(130,255,214,106)); c.drawCircle(x+pl.w/2,pl.y+12,6,p); }
        }
        for (Hazard h: hazards) {
            RectF rect=h.rect(); float x=rect.left-cameraX, pulse = 1f + .07f * (float)Math.sin(animationClock*5f+rect.left*.03f), mid=(x+rect.right-cameraX)*.5f, half=(rect.right-rect.left)*.5f*pulse;
            int trapColumn;
            if (!h.active()) {
                // A telegraphed boss trap visibly rises from a clear platform into its armed state.
                float warningProgress = h.age / Math.max(.01f, h.warning);
                trapColumn = Math.min(5, (int) (warningProgress * 6f));
                int warnColor=h.style==1?Color.rgb(114,238,141):h.style==2?Color.rgb(255,188,80):h.style==3?Color.rgb(126,225,255):Color.rgb(255,186,64);
                p.setColor(Color.argb((int)(50+120*warningProgress),Color.red(warnColor),Color.green(warnColor),Color.blue(warnColor)));
                c.drawOval(new RectF(mid-half-12,rect.top-13,mid+half+12,rect.bottom+12),p);
            } else {
                // Pulse through tall, readable armed frames: 2 → 3 → 4 → 5 → 4 → 3.
                int trapPulse = ((int) (animationClock * 4.2f + rect.left * .019f)) % 6;
                trapColumn = trapPulse <= 3 ? 2 + trapPulse : 8 - trapPulse;
            }
            Rect trapSource = new Rect(trapColumn * 256, 0, (trapColumn + 1) * 256, 342);
            float surfaceY = rect.bottom;
            drawImage(c, trapPlatformMotionSheet, trapSource, new RectF(mid - half - 26, surfaceY - 156, mid + half + 26, surfaceY + 4));
        }
        float flag=checkpointMarkerX-cameraX;
        if(flag>-80&&flag<VW) {
            float checkpointBaseY = checkpointMarkerY + 54f;
            if(checkpointActive) { p.setColor(Color.argb(82,93,236,207)); c.drawCircle(flag+18,checkpointBaseY-112,52,p); p.setColor(Color.argb(225,253,232,111)); c.drawCircle(flag+18,checkpointBaseY-112,9,p); }
            // Restore the previous compact flag while preserving the current fixed platform anchor.
            int checkpointFrame = checkpointActive ? 2 + ((int) (animationClock * 6f) % 2) : 0;
            Rect checkpointSource = new Rect(checkpointFrame * 384, 0, (checkpointFrame + 1) * 384, 384);
            drawImageTransform(c, worldInteractiveAtlas, checkpointSource, new RectF(flag - 38, checkpointBaseY - 192, flag + 90, checkpointBaseY), 0, 1f, 1f);
        }
    }

    private void drawUploadedFloatingPlatform(Canvas c, Bitmap sheet, Platform platform, float screenX, float bob) {
        int frame = platform.crumble ? Math.min(5, Math.max(0, (int)((2.6f - platform.life) / .52f))) : 0;
        Rect source = new Rect(frame * 384, 0, (frame + 1) * 384, 384);
        // The prepared sheet is top-anchored: the visible walkable top aligns with platform.y.
        drawImage(c, sheet, source, new RectF(screenX - 16, platform.y - 12 + bob, screenX + platform.w + 16, platform.y + 154 + bob));
    }

    private void drawPickups(Canvas c) {
        for (Pickup k: pickups) {
            float phase = animationClock * 4f + k.x * .017f;
            float x = k.x - cameraX;
            float y = k.y + (float) Math.sin(phase) * 3.4f;
            if (x < -70 || x > VW + 70) continue;
            float size = (k.gem ? 38 : 30) * (1f + .065f * (float) Math.sin(phase * 1.25f));
            if ("SECRET".equals(k.route)) {
                p.setStyle(Paint.Style.STROKE);
                p.setStrokeWidth(3.5f);
                int secretColor = theme().secretRoute;
                p.setColor(Color.argb(225, Color.red(secretColor), Color.green(secretColor), Color.blue(secretColor)));
                c.drawCircle(x, y, size * .92f + 4f * (float) Math.sin(phase * 1.25f), p);
                p.setStyle(Paint.Style.FILL);
                p.setColor(Color.argb(75, Color.red(secretColor), Color.green(secretColor), Color.blue(secretColor)));
                c.drawCircle(x, y, size * .82f, p);
            } else if ("RISK".equals(k.route)) {
                p.setStyle(Paint.Style.STROKE);
                p.setStrokeWidth(2.5f);
                int riskColor = theme().riskRoute;
                p.setColor(Color.argb(185, Color.red(riskColor), Color.green(riskColor), Color.blue(riskColor)));
                c.drawCircle(x, y, size * .74f + 3f * (float) Math.sin(phase * 1.25f), p);
                p.setStyle(Paint.Style.FILL);
            }
            if (k.gem) drawProceduralGemDrop(c, x, y, size * ("SECRET".equals(k.route) ? .52f : .46f), phase, "SECRET".equals(k.route));
            else drawProceduralCoinDrop(c, x, y, size * .47f, phase);
        }
    }

    // Procedural vector drops: Canvas paths, gradients and transforms instead of sprite-sheet frames.
    private void drawProceduralCoinDrop(Canvas c, float x, float y, float radius, float phase) {
        float spin = .26f + .74f * Math.abs((float) Math.cos(phase * .55f));
        c.save();
        c.translate(x, y);
        c.scale(spin, 1f);
        p.setStyle(Paint.Style.FILL);
        p.setShader(new RadialGradient(-radius * .30f, -radius * .34f, radius * 1.32f,
                new int[]{Color.rgb(255, 250, 192), Color.rgb(255, 213, 52), Color.rgb(241, 154, 20), Color.rgb(139, 73, 8)},
                new float[]{0f, .30f, .72f, 1f}, Shader.TileMode.CLAMP));
        c.drawCircle(0, 0, radius, p);
        p.setShader(null);
        p.setStyle(Paint.Style.STROKE);
        p.setStrokeWidth(1.8f);
        p.setColor(Color.rgb(130, 75, 7));
        c.drawCircle(0, 0, radius, p);
        if (spin > .48f) {
            p.setStyle(Paint.Style.FILL);
            p.setTextAlign(Paint.Align.CENTER);
            p.setTextSize(radius * 1.04f);
            p.setColor(Color.rgb(255, 250, 222));
            c.drawText("+", 0, radius * .34f, p);
            p.setTextAlign(Paint.Align.LEFT);
        }
        p.setStyle(Paint.Style.FILL);
        p.setColor(Color.argb(205, 255, 255, 238));
        c.drawOval(new RectF(-radius * .47f, -radius * .56f, -radius * .06f, -radius * .30f), p);
        c.restore();
    }

    private void drawProceduralGemDrop(Canvas c, float x, float y, float radius, float phase, boolean secret) {
        float pulse = 1f + .055f * (float) Math.sin(phase * .75f);
        int bright = secret ? Color.rgb(234, 174, 255) : Color.rgb(150, 246, 255);
        int middle = secret ? Color.rgb(165, 83, 242) : Color.rgb(45, 175, 244);
        int deep = secret ? Color.rgb(72, 26, 142) : Color.rgb(8, 76, 177);
        c.save();
        c.translate(x, y);
        c.scale(pulse, pulse);
        p.setStyle(Paint.Style.FILL);
        p.setColor(Color.argb(66, Color.red(bright), Color.green(bright), Color.blue(bright)));
        c.drawCircle(0, 0, radius * 1.36f, p);
        Path crystal = new Path();
        crystal.moveTo(0, -radius);
        crystal.lineTo(radius * .78f, -radius * .18f);
        crystal.lineTo(0, radius * 1.08f);
        crystal.lineTo(-radius * .78f, -radius * .18f);
        crystal.close();
        p.setShader(new LinearGradient(0, -radius, 0, radius * 1.08f, bright, deep, Shader.TileMode.CLAMP));
        c.drawPath(crystal, p);
        p.setShader(null);
        p.setStyle(Paint.Style.STROKE);
        p.setStrokeWidth(1.55f);
        p.setColor(Color.argb(235, 236, 255, 255));
        c.drawPath(crystal, p);
        p.setStyle(Paint.Style.FILL);
        Path leftFacet = new Path();
        leftFacet.moveTo(0, -radius * .88f);
        leftFacet.lineTo(-radius * .62f, -radius * .13f);
        leftFacet.lineTo(0, radius * .18f);
        leftFacet.close();
        p.setColor(Color.argb(145, 255, 255, 255));
        c.drawPath(leftFacet, p);
        Path rightFacet = new Path();
        rightFacet.moveTo(0, -radius * .88f);
        rightFacet.lineTo(radius * .62f, -radius * .13f);
        rightFacet.lineTo(0, radius * .18f);
        rightFacet.close();
        p.setColor(Color.argb(118, Color.red(middle), Color.green(middle), Color.blue(middle)));
        c.drawPath(rightFacet, p);
        p.setColor(Color.argb(230, 255, 255, 255));
        c.drawCircle(-radius * .24f, -radius * .36f, radius * .11f, p);
        c.restore();
    }

    private void drawFoes(Canvas c) {
        for(Foe e:foes) {
            float phase=animationClock*(e.kind==1?5.2f:3.4f)+e.x*.025f, x=e.x-cameraX; if(x<-120||x>VW+120)continue;
            boolean attacking=e.state==2||e.state==3;
            boolean warning=e.state==1;
            float bob=((e.kind==1||e.kind==3||e.kind==7)?(float)Math.sin(phase)*8:(float)Math.sin(phase)*2f);
            if(warning) { int warnColor=EnemyController.archetype(e.kind).accentColor; p.setColor(Color.argb(96,Color.red(warnColor),Color.green(warnColor),Color.blue(warnColor))); c.drawCircle(x,e.y-22+bob,42+4*(float)Math.sin(animationClock*14f),p); }
            if(e.kind==3&&e.state==2){p.setColor(Color.argb(105,126,226,255));c.drawCircle(x,e.y-6+bob,58,p);}
            if(e.kind==0 || e.kind==1) {
                Bitmap sheet=e.kind==0?mossCrawlerSheet:emberMothSheet;
                int frame=e.hurtTime>0?4:attacking?3:1+((int)(phase*1.15f)%2);
                Rect source=new Rect(frame*512,0,frame*512+512,512);
                float width=e.kind==0?86:108, height=e.kind==0?86:106;
                float bottom=e.kind==0?e.y+42+bob:e.y+30+bob;
                RectF destination=new RectF(x-width/2,bottom-height,x+width/2,bottom);
                float facingScale=e.kind==0?(e.dir>0?-1f:1f):(e.dir<0?-1f:1f);
                drawImageTransform(c,sheet,source,destination,attacking?(float)Math.sin(phase)*3f:0,facingScale,1f);
            } else {
                // The shared atlas has six rows for kinds 2–7 and four animation poses per row.
                int atlasRow = e.kind - EnemyController.FAST_SKIRMISHER;
                int atlasColumn = e.hurtTime > 0 ? 3 : (attacking || warning ? 2 : ((int) (phase * 1.25f) % 2));
                Rect source = new Rect(atlasColumn * 384, atlasRow * 384, (atlasColumn + 1) * 384, (atlasRow + 1) * 384);
                float squash=1f+.045f*(float)Math.sin(phase);
                float scale = e.kind == EnemyController.HEAVY_BRUTE ? 1.36f : e.kind == EnemyController.SHIELD_GUARD ? 1.10f : e.kind == EnemyController.WIND_WISP ? .88f : 1.0f;
                if (e.kind == EnemyController.HEAVY_BRUTE) { p.setColor(Color.argb(70, 255, 106, 82)); c.drawCircle(x, e.y - 10 + bob, 58, p); }
                if (e.kind == EnemyController.RUNE_CASTER) { p.setColor(Color.argb(78, 181, 161, 255)); c.drawCircle(x, e.y - 18 + bob, 46, p); }
                float facingScale = e.dir < 0 ? -1f / squash : 1f / squash;
                drawImageTransform(c, enemyMotionAtlas, source, new RectF(x-50*scale,e.y-72*scale+bob,x+50*scale,e.y+42*scale+bob), 0, facingScale, squash);
                if (e.kind == EnemyController.SHIELD_GUARD) {
                    p.setColor(Color.rgb(92, 219, 207));
                    float shieldX = x + (e.dir > 0 ? 36 : -36);
                    c.drawRoundRect(shieldX - 6, e.y - 36 + bob, shieldX + 6, e.y + 18 + bob, 6, 6, p);
                } else if (e.kind == EnemyController.RUNE_CASTER) {
                    p.setColor(Color.rgb(202, 183, 255));
                    c.drawCircle(x + e.dir * 30, e.y - 28 + bob, 7 + 2*(float)Math.sin(phase*2), p);
                }
            }
            if (e.burnTime > 0) { p.setColor(Color.argb(105, 255, 120, 52)); c.drawCircle(x, e.y - 11 + bob, 46 + 3*(float)Math.sin(animationClock*8f), p); }
            if (e.hurtTime > 0) { p.setColor(Color.argb(92, 255, 240, 177)); c.drawCircle(x, e.y - 12 + bob, 38, p); }
            if(e.frozen>0) { p.setColor(Color.argb(95,142,232,255)); c.drawCircle(x,e.y-5+bob,44,p); }
        }
        for (EnemyProjectile projectile : enemyProjectiles) {
            float x = projectile.x - cameraX;
            if (x < -30 || x > VW + 30) continue;
            p.setColor(Color.argb(95, 181, 161, 255)); c.drawCircle(x, projectile.y, 15, p);
            p.setColor(Color.rgb(220, 202, 255)); c.drawCircle(x, projectile.y, 7, p);
        }
    }

    // ---- Boss skeletal rig data ----
    // Each part's source Rect is a tight crop of that part's actual artwork within its
    // 512x512 sheet cell (no empty padding), so scaling stays uniform across parts.
    // anchorX/anchorY mark the joint point as a fraction of that part's own box; when drawn,
    // the part is positioned so its anchor lands exactly on the parent's attach point, then
    // rotated around that same shared point -- guaranteeing the seam never gaps or floats.
    private static final class RigPart {
        final Rect source; final float destW, destH, anchorX, anchorY;
        RigPart(Rect source, float destW, float destH, float anchorX, float anchorY) {
            this.source = source; this.destW = destW; this.destH = destH; this.anchorX = anchorX; this.anchorY = anchorY;
        }
    }
    private static final class BossRig {
        final Rect torsoSource; final float torsoDestW, torsoDestH, torsoLeft, torsoTop;
        final RigPart head, armR, armL, legA, legB;
        final float neckX, neckY, lShoulderX, lShoulderY, rShoulderX, rShoulderY, lHipX, lHipY, rHipX, rHipY;
        BossRig(Rect torsoSource, float torsoDestW, float torsoDestH, float torsoLeft, float torsoTop,
                RigPart head, RigPart armR, RigPart armL, RigPart legA, RigPart legB,
                float neckX, float neckY, float lShoulderX, float lShoulderY, float rShoulderX, float rShoulderY,
                float lHipX, float lHipY, float rHipX, float rHipY) {
            this.torsoSource=torsoSource; this.torsoDestW=torsoDestW; this.torsoDestH=torsoDestH; this.torsoLeft=torsoLeft; this.torsoTop=torsoTop;
            this.head=head; this.armR=armR; this.armL=armL; this.legA=legA; this.legB=legB;
            this.neckX=neckX; this.neckY=neckY; this.lShoulderX=lShoulderX; this.lShoulderY=lShoulderY; this.rShoulderX=rShoulderX; this.rShoulderY=rShoulderY;
            this.lHipX=lHipX; this.lHipY=lHipY; this.rHipX=rHipX; this.rHipY=rHipY;
        }
    }

    // ---- FOREST ----
    // Regions below are pixel rects into the repacked side-profile rig sheet
    // (boss_forest_rig_parts.png): a tight 2-row grid of head/torso/armA at top,
    // armB/legA/legB below. Joint offsets were hand-calibrated against rendered
    // previews so parts visually socket together through the full animation cycle.
    private static final Rect FOREST_HEAD_SRC = new Rect(0, 0, 260, 342);
    private static final Rect FOREST_TORSO_SRC = new Rect(260, 0, 450, 513);
    private static final Rect FOREST_ARM_A_SRC = new Rect(497, 0, 662, 406);
    private static final Rect FOREST_ARM_B_SRC = new Rect(0, 513, 149, 885);
    private static final Rect FOREST_LEG_A_SRC = new Rect(260, 513, 497, 979);
    private static final Rect FOREST_LEG_B_SRC = new Rect(497, 513, 722, 993);
    private static final BossRig FOREST_RIG = new BossRig(
        FOREST_TORSO_SRC, 89.869f, 242.647f, -44.935f, -403.068f,
        new RigPart(FOREST_HEAD_SRC, 122.979f, 161.764f, 0.65f, 0.92f),
        new RigPart(FOREST_ARM_B_SRC, 78.044f, 192.036f, 0.30f, 0.05f),
        new RigPart(FOREST_ARM_A_SRC, 78.044f, 192.036f, 0.30f, 0.05f),
        new RigPart(FOREST_LEG_A_SRC, 112.100f, 220.416f, 0.48f, 0.03f),
        new RigPart(FOREST_LEG_B_SRC, 112.100f, 220.416f, 0.48f, 0.03f),
        17.0f, -325.0f, 31.454f, -369.656f, -31.454f, -369.656f,
        17.974f, -213.803f, -17.974f, -213.803f);

    // ---- STONE ----
    private static final Rect STONE_HEAD_SRC = new Rect(0, 0, 191, 214);
    private static final Rect STONE_TORSO_SRC = new Rect(194, 0, 435, 466);
    private static final Rect STONE_ARM_A_SRC = new Rect(476, 0, 663, 450);
    private static final Rect STONE_ARM_B_SRC = new Rect(0, 466, 194, 880);
    private static final Rect STONE_LEG_A_SRC = new Rect(194, 466, 476, 931);
    private static final Rect STONE_LEG_B_SRC = new Rect(476, 466, 761, 946);
    private static final BossRig STONE_RIG = new BossRig(
        STONE_TORSO_SRC, 120.325f, 232.662f, -60.163f, -406.674f,
        new RigPart(STONE_HEAD_SRC, 95.361f, 106.845f, 0.65f, 0.92f),
        new RigPart(STONE_ARM_B_SRC, 93.364f, 224.673f, 0.25f, 0.05f),
        new RigPart(STONE_ARM_A_SRC, 93.364f, 224.673f, 0.25f, 0.05f),
        new RigPart(STONE_LEG_A_SRC, 140.795f, 232.163f, 0.35f, 0.03f),
        new RigPart(STONE_LEG_B_SRC, 140.795f, 232.163f, 0.35f, 0.03f),
        10.0f, -358.0f, 40.0f, -378.0f, -40.0f, -378.0f,
        35.0f, -230.0f, -35.0f, -230.0f);

    // ---- ICE ----
    private static final Rect ICE_HEAD_SRC = new Rect(0, 0, 220, 310);
    private static final Rect ICE_TORSO_SRC = new Rect(220, 0, 497, 555);
    private static final Rect ICE_ARM_A_SRC = new Rect(501, 0, 688, 460);
    private static final Rect ICE_ARM_B_SRC = new Rect(0, 555, 190, 994);
    private static final Rect ICE_LEG_A_SRC = new Rect(220, 555, 501, 1016);
    private static final Rect ICE_LEG_B_SRC = new Rect(501, 555, 796, 1034);
    private static final BossRig ICE_RIG = new BossRig(
        ICE_TORSO_SRC, 125.796f, 252.046f, -62.898f, -392.111f,
        new RigPart(ICE_HEAD_SRC, 99.910f, 140.783f, 0.65f, 0.92f),
        new RigPart(ICE_ARM_B_SRC, 84.924f, 208.903f, 0.25f, 0.05f),
        new RigPart(ICE_ARM_A_SRC, 84.924f, 208.903f, 0.25f, 0.05f),
        new RigPart(ICE_LEG_A_SRC, 127.613f, 209.357f, 0.48f, 0.03f),
        new RigPart(ICE_LEG_B_SRC, 127.613f, 209.357f, 0.48f, 0.03f),
        12.0f, -345.0f, 30.0f, -355.0f, -30.0f, -355.0f,
        22.0f, -250.0f, -22.0f, -250.0f);

    private static BossRig rigFor(int world) { return world == 1 ? FOREST_RIG : world == 2 ? STONE_RIG : ICE_RIG; }

    private void drawBoss(Canvas c) {
        if (boss==null)return;
        float x=boss.x-cameraX, phase=animationClock*(boss.phase==2?3.4f:2.1f), bob=(float)Math.sin(phase)*4f;
        // 2D skeletal-animation rig: independent head, torso, arms and legs are posed around shared
        // joint points every frame, so parts stay visually attached through the whole animation cycle.
        Bitmap rigSheet = boss.world == 1 ? bossForestRigParts : boss.world == 2 ? bossStoneRigParts : bossIceRigParts;
        BossRig rig = rigFor(boss.world);
        float bossFacingScale = px < boss.x ? -1f : 1f;
        float bossGroundY = boss.y + 80f + bob;
        if (boss.burnTime > 0) { p.setColor(Color.argb(95,255,126,58)); c.drawCircle(x,bossGroundY-148,126+5*(float)Math.sin(animationClock*8f),p); }
        if (boss.frostSlowTime > 0) { p.setColor(Color.argb(85,142,232,255)); c.drawCircle(x,bossGroundY-148,132,p); }
        if (boss.chargeTelegraph > 0 || boss.leapTelegraph > 0) {
            // Bright pulsing ring reads clearly as "an attack is about to fire" independent of hazard tells.
            float telegraphT = boss.chargeTelegraph > 0 ? boss.chargeTelegraph / .46f : boss.leapTelegraph / .5f;
            float ringPulse = 1f + .12f * (float) Math.sin(animationClock * 16f);
            p.setColor(Color.argb((int) (150 * (1f - telegraphT) + 60), 255, 210, 90));
            c.drawCircle(x, bossGroundY - 148, (118 + 20 * (1f - telegraphT)) * ringPulse, p);
        }
        drawSkeletalBoss(c, rigSheet, rig, x, bossGroundY, phase, bossFacingScale);
        // Keep the boss label and health bar safely above the tallest sprite frame.
        float bossHudY = boss.y - 380 + bob;
        p.setColor(Color.argb(230,255,255,255)); c.drawRect(x-96, bossHudY, x+96, bossHudY+13, p);
        p.setColor(Color.rgb(211,64,68)); c.drawRect(x-96, bossHudY, x-96+192*(boss.hp/(float)boss.maxHp), bossHudY+13, p);
        BossController.Profile profile=BossController.profile(boss.world);
        String phaseName=boss.phase==1?"Awakening":boss.phase==2?profile.phaseTwoName:profile.phaseThreeName;
        centeredAt(c,boss.name+" • "+phaseName, x, bossHudY-15, 14, Color.WHITE);
    }

    private void drawSkeletalBoss(Canvas c, Bitmap rigSheet, BossRig rig, float x, float groundY, float phase, float facingScale) {
        float breath = (float) Math.sin(phase * 1.35f);
        float stride = (float) Math.sin(phase * 1.8f);
        float charge = boss.transitionTime > 0 ? 1f : (boss.chargeTelegraph > 0 || boss.leapTelegraph > 0) ? 1f : boss.cooldown < .44f ? .72f : 0f;
        float dash = boss.chargeTime > 0 ? 1f : 0f;
        float hurt = boss.hitLock > 0 ? 1f : 0f;
        float torsoTilt = breath * 1.4f - charge * 5f + hurt * 3.5f + dash * (boss.dir >= 0 ? 8f : -8f);
        float leftArmAngle = -stride * 8f - charge * 30f + hurt * 10f;
        float rightArmAngle = stride * 8f + charge * 24f - hurt * 15f;
        float leftLegAngle = stride * 4.8f;
        float rightLegAngle = -stride * 4.8f;
        c.save();
        c.translate(x, groundY);
        c.scale(facingScale, 1f);
        // Legs are first so the hips and torso naturally overlap their attachment seams.
        drawRigPart(c, rigSheet, rig.legA, rig.lHipX, rig.lHipY, leftLegAngle);
        drawRigPart(c, rigSheet, rig.legB, rig.rHipX, rig.rHipY, rightLegAngle);
        // The rear arm sits under the torso; the forward arm sits above it for a layered silhouette.
        drawRigPart(c, rigSheet, rig.armR, rig.rShoulderX, rig.rShoulderY, rightArmAngle);
        float torsoPivotX = rig.torsoLeft + rig.torsoDestW * 0.5f, torsoPivotY = rig.torsoTop + rig.torsoDestH * 0.65f;
        c.save();
        c.rotate(torsoTilt, torsoPivotX, torsoPivotY);
        drawImage(c, rigSheet, rig.torsoSource, new RectF(rig.torsoLeft, rig.torsoTop, rig.torsoLeft + rig.torsoDestW, rig.torsoTop + rig.torsoDestH));
        c.restore();
        drawRigPart(c, rigSheet, rig.armL, rig.lShoulderX, rig.lShoulderY, leftArmAngle);
        drawRigPart(c, rigSheet, rig.head, rig.neckX, rig.neckY + breath * 3f, breath * 1.2f - charge * 3f);
        c.restore();
    }

    private void drawRigPart(Canvas c, Bitmap sheet, RigPart part, float targetX, float targetY, float rotation) {
        c.save();
        c.rotate(rotation, targetX, targetY);
        float left = targetX - part.anchorX * part.destW, top = targetY - part.anchorY * part.destH;
        drawImage(c, sheet, part.source, new RectF(left, top, left + part.destW, top + part.destH));
        c.restore();
    }

    private void drawPlayer(Canvas c) {
        float x=px-cameraX; if(invincible>0 && ((int)(invincible*18)%2==0) && screen!=GAMEOVER) return;
        float stride=(float)Math.sin(animationClock*11f), idle=(float)Math.sin(animationClock*2.1f);
        boolean running=grounded&&Math.abs(vx)>55;
        boolean dashing=dashTime>0, sliding=slideTime>0&&grounded;
        Bitmap sheet; Rect source; float width; float bob; float lean;
        if(screen==GAMEOVER){sheet=asterDefeatSheet;int frame=defeatTime>.22f?1:0;source=new Rect(frame*512,0,frame*512+512,512);width=150;bob=0;lean=0;}
        else if(hurtTime>0){sheet=asterHurtSheet;int frame=((int)(animationClock*9f))%2;source=new Rect(frame*512,0,frame*512+512,512);width=148;bob=0;lean=-4;}
        else if(attackTime>0){sheet=asterAttackSheet;int frame=Math.min(2,(int)((attackDuration-attackTime)*12f));source=new Rect(frame*512,0,frame*512+512,512);width=airAttack?166:(chargedAttack?188:attackStage>=4?190:attackStage==3?178:attackStage==2?172:160);bob=0;lean=airAttack?-18:(chargedAttack?-14:attackStage>=4?-13:attackStage==3?-10:attackStage==2?-8:-4);}
        else if(dashing||sliding){sheet=asterRunSheet;int frame=((int)(animationClock*18f))%8;source=new Rect(frame*512,0,frame*512+512,512);width=dashing?154:145;bob=sliding?4:0;lean=dashing?6:-8;}
        else if(!grounded){sheet=asterJump;source=null;width=132;bob=0;lean=0;}
        else if(running){sheet=asterRunSheet;int frame=((int)(animationClock*13f))%8;source=new Rect(frame*512,0,frame*512+512,512);width=142;bob=Math.abs(stride)*3f;lean=stride*1.6f;}
        else {sheet=asterIdleSheet;source=new Rect(0,0,512,512);width=140;bob=idle*1.5f;lean=idle*1.1f;}
        float land = Math.max(0, landingPulse / .14f);
        float sx=(running?1f+stride*.016f:1f-idle*.01f) - land*.075f, sy=(running?1f-stride*.02f:1f+idle*.012f) + land*.14f;
        if (JUICE_ENABLED && playerSquashTime > 0) {
            float squash = Math.min(1f, playerSquashTime / (playerSquashLanding ? .12f : .085f));
            if (playerSquashLanding) { sx += .10f * squash; sy -= .13f * squash; }
            else { sx -= .055f * squash; sy += .075f * squash; }
        }
        if(grounded){p.setColor(Color.argb(72,3,12,16));c.drawOval(new RectF(x-width*.31f,py+48,x+width*.31f,py+61),p);}
        RectF dest=new RectF(x-width/2,py-110+bob,x+width/2,py+66+bob);
        drawImageTransform(c,sheet,source,dest,lean,facingLeft?-sx:sx,sy);
        if(powerTime>0){p.setColor(Color.argb(80,Color.red(data.accent),Color.green(data.accent),Color.blue(data.accent)));c.drawCircle(x,py+25+bob,65+4*(float)Math.sin(animationClock*9f),p);}
    }

    private void drawEffects(Canvas c) {
        float playerX=px-cameraX;
        if (Math.abs(vx)>70 && grounded && screen==LEVEL) {
            float tailX=playerX+(facingLeft?35:-35);
            int runFrame = ((int) (animationClock * 7f)) % 3;
            Rect runSource = new Rect(runFrame * 544, 0, (runFrame + 1) * 544, 544);
            // Anchor the visible ink of the trail at the player/ground contact line.
            float runContactY = py + 52f;
            drawImageTransform(c, actionFxAtlas, runSource, new RectF(tailX-74, runContactY-21, tailX+74, runContactY+9), 0, facingLeft ? -.86f : .86f, .86f);
        }
        if (dashFxTime > 0) {
            float dashTail=playerX+(facingLeft?74:-74), strength=dashFxTime/.24f;
            Rect dashSource = new Rect(2 * 544, 0, 3 * 544, 544);
            drawImageTransform(c, actionFxAtlas, dashSource, new RectF(dashTail-118,py-62,dashTail+118,py+70), 0, facingLeft ? -1.18f : 1.18f, 1.18f);
            p.setColor(Color.argb((int)(95*strength),160,246,255));
            c.drawCircle(playerX+(facingLeft?44:-44),py+22,36+18*strength,p);
        }
        if(dodgeFxTime>0){float strength=dodgeFxTime/.16f;p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(4);p.setColor(Color.argb((int)(210*strength),137,246,255));c.drawCircle(playerX,py+22,34+28*(1f-strength),p);p.setStyle(Paint.Style.FILL);}
        if (perfectDodgeTime > 0) {
            float pulse = 1f - perfectDodgeTime / .26f;
            p.setStyle(Paint.Style.STROKE); p.setStrokeWidth(3f); p.setColor(Color.argb((int)(170*(1-pulse)), 171, 248, 255));
            c.drawCircle(playerX, py+20, 58+32*pulse, p); p.setStyle(Paint.Style.FILL);
            centeredAt(c, "PERFECT DODGE", playerX, py-72, 15, Color.rgb(188, 250, 255));
        }
        if (galeBurstTime > 0) { float strength=galeBurstTime/PowerSystem.galeBurstDuration(); p.setStyle(Paint.Style.STROKE); p.setStrokeWidth(5f); p.setColor(Color.argb((int)(185*strength), 170,247,236)); c.drawCircle(playerX,py+18,64+42*(1f-strength),p); p.setStyle(Paint.Style.FILL); }
        if (combatCalloutTime > 0) {
            float alpha=Math.min(1f,combatCalloutTime/.16f);
            int calloutColor="COUNTER".equals(combatCallout)?Color.rgb(180,248,255):"FINISHER".equals(combatCallout)?Color.rgb(255,222,122):Color.rgb(255,186,112);
            centeredAt(c, combatCallout, combatCalloutX-cameraX, combatCalloutY-58-(1f-alpha)*18, 17, Color.argb((int)(235*alpha),Color.red(calloutColor),Color.green(calloutColor),Color.blue(calloutColor)));
        }
        if (secretMessageTime > 0) {
            float alpha = Math.min(1f, secretMessageTime / .35f);
            p.setColor(Color.argb((int)(170*alpha), 23, 14, 42)); c.drawRoundRect(430, 145, 850, 190, 16, 16, p);
            centered(c, secretMessage, 175, 18, Color.rgb(231, 194, 255));
        }
        if (boss != null && boss.transitionTime > 0) {
            float alpha=boss.transitionTime/.72f;
            p.setColor(Color.argb((int)(72*alpha),Color.red(data.accent),Color.green(data.accent),Color.blue(data.accent))); c.drawRect(0,0,VW,VH,p);
            centered(c,"PHASE "+boss.phase+" • "+(boss.phase==1?"AWAKENING":boss.phase==2?BossController.profile(boss.world).phaseTwoName:BossController.profile(boss.world).phaseThreeName),186,24,Color.WHITE);
        }
        if(airStrikeFxTime>0){float strength=airStrikeFxTime/.36f;drawImageAlpha(c,superHitLine,null,new RectF(playerX-16,py+18,playerX+16,py+142),Math.max(35,(int)(180*strength)));}
        if (wallSliding) {
            float wallX=playerX-wallDir*28;
            drawImageAlpha(c,fxTrail,null,new RectF(wallX-28,py-42,wallX+28,py+38),92);
        }
        if (landingPulse > 0) {
            float pulse=landingPulse/.14f;
            drawImageAlpha(c,fxTrail,null,new RectF(playerX-74,py+22,playerX+74,py+56),Math.max(0,(int)(130*pulse)));
        }
        if (swordFxTime>0) {
            float side=facingLeft?-1:1, x=playerX+side*66;
            float effectDuration = chargedAttack ? .34f : .20f;
            float progress = 1f - Math.max(0f, swordFxTime) / effectDuration;
            int attackFrame = Math.min(3, (int) (progress * 4f));
            Rect attackSource = new Rect(attackFrame * 544, 544, (attackFrame + 1) * 544, 1088);
            float scale=.76f+(.18f*(swordFxTime/effectDuration)) + (attackStage >= 4 ? .10f : 0f);
            if(airAttack){
                drawImageTransform(c,actionFxAtlas,attackSource,new RectF(playerX-72,py-2,playerX+72,py+142),90,scale,scale);
                drawImageTransform(c,superHitLine,null,new RectF(playerX-16,py+35,playerX+18,py+120),90,1,1);
            } else {
                drawImageTransform(c,actionFxAtlas,attackSource,new RectF(x-72,py-66,x+72,py+78),side<0?-18:18,side*scale,scale);
                drawImageTransform(c,superHitLine,null,new RectF(x-42,py+2,x+42,py+23),side<0?180:0,side,1f);
            }
        }
        if (hitFxTime>0) {
            float x=fxX-cameraX, scale=.76f+.36f*(hitFxTime/.18f);
            Rect impactSource = new Rect(2 * 544, 544, 3 * 544, 1088);
            drawImageTransform(c,actionFxAtlas,impactSource,new RectF(x-74,fxY-74,x+74,fxY+74),animationClock*34f,scale,scale);
        }
        if (sparkleFxTime>0) {
            float x=fxX-cameraX, scale=.82f+.42f*(sparkleFxTime/.34f);
            int sparkleFrame = Math.min(3, (int) ((1f - sparkleFxTime / .34f) * 4f));
            Rect sparkleSource = new Rect(sparkleFrame * 544, 2 * 544, (sparkleFrame + 1) * 544, 3 * 544);
            drawImageTransform(c,actionFxAtlas,sparkleSource,new RectF(x-66,fxY-66,x+66,fxY+66),0,scale,scale);
        }
        drawJuiceParticles(c);
    }

    private void drawJuiceParticles(Canvas c) {
        for (JuiceParticle particle : juiceParticles) {
            float life = particle.life / particle.maxLife;
            int alpha = Math.max(0, Math.min(220, (int) (220 * life)));
            p.setColor(Color.argb(alpha, Color.red(particle.color), Color.green(particle.color), Color.blue(particle.color)));
            float x = particle.x - cameraX;
            float size = particle.size * (.55f + life * .45f);
            c.drawCircle(x, particle.y, size, p);
        }
    }

    private void drawHud(Canvas c) {
        if(levelElapsed<3.2f){ float intro=Math.min(1f,levelElapsed/.25f); p.setColor(Color.argb((int)(210*intro),9,24,35));c.drawRoundRect(16,15,390,91,22,22,p);stroke.setColor(Color.argb((int)(130*intro),174,235,225));c.drawRoundRect(16,15,390,91,22,22,stroke);text(c,data.worldName,30,41,16,Color.WHITE);text(c,data.title,30,63,15,Color.rgb(196,226,219));text(c,levelDesignRole+"  •  "+data.mechanic,30,83,13,Color.rgb(152,229,230)); }
        if(levelElapsed<7.6f){float hint=Math.min(1f,levelElapsed/.35f);p.setColor(Color.argb((int)(175*hint),8,22,34));c.drawRoundRect(255,679,1025,711,14,14,p);centeredAt(c,"DASH DODGES HITS   •   ATTACK IN AIR: DIVE SLASH   •   WALL + JUMP: CLIMB",640,701,13,Color.rgb(216,245,240));}
        panel(c,366,21,570,68); drawImage(c,uiShield,null,new RectF(374,28,402,60));
        for(int i=0;i<maxHealth;i++) drawImage(c,i<health?superHeartFull:superHeartEmpty,null,new RectF(410+i*18,32,426+i*18,47));
        drawImageAlpha(c,superHudBox,null,new RectF(580,27,611,58),78); p.setColor(Color.rgb(255,219,80));c.drawCircle(595,43,8,p);text(c,""+coinsRun,610,50,19,Color.WHITE);
        drawImageAlpha(c,superHudBox,null,new RectF(662,27,693,58),78); p.setColor(Color.rgb(103,236,255));c.drawCircle(677,43,8,p);text(c,""+gemsRun,692,50,19,Color.WHITE);
        drawStageProgress(c);
        panel(c,950,18,1115,72); drawImage(c,uiEnergyBolt,null,new RectF(956,21,982,68)); text(c,powers[power],990,45,18,Color.rgb(232,250,244)); p.setColor(Color.rgb(36,55,68)); c.drawRoundRect(990,53,1100,61,4,4,p); p.setColor(Color.rgb(100,224,248)); c.drawRoundRect(990,53,990+110*(energy/maxEnergy),61,4,4,p); button(c,1140,18,1260,72,"II",Color.rgb(51,77,102));
        if (data.boss && boss!=null && boss.hp>0) { panel(c,430,85,850,118); p.setColor(Color.rgb(75,43,51));c.drawRoundRect(445,94,835,109,7,7,p);p.setColor(Color.rgb(218,76,76));c.drawRoundRect(445,94,445+390*boss.hp/(float)boss.maxHp,109,7,7,p);centered(c,boss.name+" • Phase "+boss.phase,138,16,Color.WHITE); }
        if(screen==LEVEL)drawControls(c);
    }

    private void drawStageProgress(Canvas c) {
        float progress=Math.max(0f,Math.min(1f,(px-120f)/2030f));
        panel(c,710,21,930,68); text(c,data.boss?"BOSS TRAIL":"TRAIL",724,41,14,Color.rgb(198,230,230));
        p.setColor(Color.rgb(33,53,68)); c.drawRoundRect(724,49,914,58,5,5,p);
        p.setColor(data.accent); c.drawRoundRect(724,49,724+190*progress,58,5,5,p);
        float checkpointProgress=Math.max(0f,Math.min(1f,(checkpointMarkerX-120f)/2030f));
        p.setColor(checkpointActive?Color.rgb(255,228,114):Color.rgb(150,183,198)); c.drawCircle(724+190*checkpointProgress,53.5f,4.5f,p);
        text(c,(int)(progress*100)+"%",882,42,13,Color.WHITE);
    }

    private void drawPauseProgress(Canvas c) {
        float progress=Math.max(0f,Math.min(1f,(px-120f)/2030f));
        centered(c,"Trail progress  "+(int)(progress*100)+"%   •   Coins "+coinsRun+"   •   Gems "+gemsRun,267,16,Color.rgb(207,231,231));
        p.setColor(Color.rgb(32,52,66));c.drawRoundRect(485,272,795,282,6,6,p);p.setColor(data.accent);c.drawRoundRect(485,272,485+310*progress,282,6,6,p);
        float checkpointProgress=Math.max(0f,Math.min(1f,(checkpointMarkerX-120f)/2030f));p.setColor(checkpointActive?Color.rgb(255,228,114):Color.rgb(150,183,198));c.drawCircle(485+310*checkpointProgress,277,5,p);
    }

    private void drawControls(Canvas c) {
        int controlColor = theme().controlFill;
        p.setColor(Color.argb(110,Color.red(controlColor),Color.green(controlColor),Color.blue(controlColor))); c.drawCircle(91,626,51,p); c.drawCircle(204,626,51,p); c.drawCircle(960,545,46,p); c.drawCircle(1070,626,52,p); c.drawCircle(1188,626,52,p);
        p.setColor(Color.argb(190,185,237,236)); centeredAt(c,"‹",91,640,52,Color.WHITE); centeredAt(c,"›",204,640,52,Color.WHITE); centeredAt(c,"JUMP",1070,633,16,Color.WHITE); centeredAt(c,"POWER",1188,633,14,Color.WHITE); if(dashCooldown<=0){centeredAt(c,"×2",91,684,13,Color.rgb(165,245,224));centeredAt(c,"×2",204,684,13,Color.rgb(165,245,224));}
        drawImage(c,attackButton,null,new RectF(914,499,1006,591));
        if (attackHolding) {
            float progress = Math.min(1f, chargeTime / .38f);
            p.setStyle(Paint.Style.STROKE); p.setStrokeWidth(4f);
            p.setColor(chargeReady ? Color.rgb(255, 221, 116) : Color.rgb(136, 239, 229));
            c.drawArc(new RectF(904,489,1016,601), -90, 360*progress, false, p);
            p.setStyle(Paint.Style.FILL);
            centeredAt(c, chargeReady ? "READY" : "HOLD", 960, 613, 12, chargeReady ? Color.rgb(255,229,126) : Color.rgb(205,245,238));
        } else if (attackStage > 0 && attackTime > 0) {
            centeredAt(c, attackStage + "/4", 960, 613, 13, Color.rgb(214,246,237));
        }
    }

    private void upgradeCard(Canvas c,float x,float y,String title,String sub,String key,int rank,int accent){panel(c,x,y,x+290,y+360);Rect token=key.equals("attack")?new Rect(600,1067,800,1453):new Rect(773,967,1024,1453);float pulse=1f+.08f*(float)Math.sin(animationClock*3f+x*.01f);drawImageTransform(c,worldProps,token,new RectF(x+195,y+24,x+260,y+89),key.equals("attack")?animationClock*70:0,pulse,pulse);text(c,title,x+25,y+53,22,Color.WHITE);text(c,sub,x+25,y+84,15,Color.rgb(190,213,222));for(int i=0;i<3;i++){p.setColor(i<rank?accent:Color.rgb(64,84,103));c.drawCircle(x+55+i*42,y+145,13,p);}text(c,"Rank "+rank+" / 3",x+25,y+190,18,Color.WHITE);button(c,x+30,y+250,x+260,y+310,"BUY • 30",accent);}
    private void upgradeGemCard(Canvas c,float x,float y,String title,String sub,int rank,int accent){panel(c,x,y,x+290,y+360);float pulse=1f+.08f*(float)Math.sin(animationClock*3f+x*.01f);drawImageTransform(c,gemNew,null,new RectF(x+195,y+24,x+260,y+89),animationClock*48,pulse,pulse);text(c,title,x+25,y+53,22,Color.WHITE);text(c,sub,x+25,y+84,15,Color.rgb(190,213,222));for(int i=0;i<3;i++){p.setColor(i<rank?accent:Color.rgb(64,84,103));c.drawCircle(x+55+i*42,y+145,13,p);}text(c,"Rank "+rank+" / 3",x+25,y+190,18,Color.WHITE);button(c,x+30,y+250,x+260,y+310,"BUY • 3 GEMS",accent);}
    private void upgradeStarCard(Canvas c,float x,float y){int rank=save.relicRank(), threshold=save.relicThreshold();panel(c,x,y,x+650,y+180);float pulse=1f+.08f*(float)Math.sin(animationClock*3.4f);drawImageTransform(c,gemNew,null,new RectF(x+42,y+38,x+112,y+108),animationClock*48,pulse,pulse);text(c,"REALM RELIC",x+140,y+48,25,Color.rgb(255,229,126));text(c,"Spend no stars: each rank grants +1 max health, +10 energy, and cheaper powers.",x+140,y+76,15,Color.rgb(205,224,232));drawProgressStars(c,x+195,y+118,rank);text(c,"Realm Stars: "+save.totalStars()+" / "+(rank<3?String.valueOf(threshold):"MAX"),x+285,y+124,19,Color.WHITE);button(c,x+390,y+98,x+610,y+152,rank<3?"UNLOCK • "+threshold+" STARS":"MAX RANK",Color.rgb(159,120,218));}

    private void drawImage(Canvas c, Bitmap image, Rect source, RectF destination) { if (image != null) { p.setAlpha(255); c.drawBitmap(image, source, destination, p); } }
    private void drawImageAlpha(Canvas c, Bitmap image, Rect source, RectF destination, int alpha) { if (image != null) { p.setAlpha(alpha); c.drawBitmap(image, source, destination, p); p.setAlpha(255); } }
    private void drawImageTransform(Canvas c, Bitmap image, Rect source, RectF destination, float degrees, float scaleX, float scaleY) { if (image == null) return; float cx=destination.centerX(),cy=destination.centerY();c.save();c.rotate(degrees,cx,cy);c.scale(scaleX,scaleY,cx,cy);p.setAlpha(255);c.drawBitmap(image,source,destination,p);c.restore(); }
    private VisualStyle.Theme theme(){ return VisualStyle.forWorld(data == null ? 1 : data.world); }
    private void panel(Canvas c,float l,float t,float r,float b){VisualStyle.Theme theme=theme();p.setColor(theme.uiPanel);c.drawRoundRect(l,t,r,b,22,22,p);stroke.setColor(theme.uiStroke);c.drawRoundRect(l,t,r,b,22,22,stroke);}
    private void button(Canvas c,float l,float t,float r,float b,String s,int fill){p.setColor(fill);c.drawRoundRect(l,t,r,b,16,16,p);p.setColor(Color.argb(70,255,255,255));c.drawRoundRect(l+2,t+2,r-2,t+6,12,12,p);centeredAt(c,s,(l+r)/2,(t+b)/2+7,17,Color.WHITE);}
    private void badge(Canvas c,float x,float y,String s){p.setColor(Color.argb(175,9,28,39));c.drawRoundRect(x,y,x+160,y+34,12,12,p);centeredAt(c,s,x+80,y+23,14,Color.rgb(192,244,221));}
    private void overlay(Canvas c,int clr){p.setColor(clr);c.drawRect(0,0,VW,VH,p);}
    private Paint color(int c){p.setColor(c);return p;}
    private void text(Canvas c,String s,float x,float y,float size,int clr){p.setTextSize(size);p.setColor(clr);p.setTextAlign(Paint.Align.LEFT);p.setTypeface(Typeface.create("sans",Typeface.BOLD));c.drawText(s,x,y,p);}
    private void centered(Canvas c,String s,float y,float size,int clr){centeredAt(c,s,VW/2,y,size,clr);}
    private void centeredAt(Canvas c,String s,float x,float y,float size,int clr){p.setTextSize(size);p.setColor(clr);p.setTextAlign(Paint.Align.CENTER);p.setTypeface(Typeface.create("sans",Typeface.BOLD));c.drawText(s,x,y,p);p.setTextAlign(Paint.Align.LEFT);}
    private void drawStars(Canvas c,int n,int col){p.setColor(col);for(int i=0;i<n;i++)c.drawCircle((i*83)%1280,25+(i*47)%620,(i%3)+1,p);}
    private void drawProgressStars(Canvas c,float centerX,float y,int count){for(int i=0;i<3;i++){p.setColor(i<count?Color.rgb(255,218,91):Color.rgb(83,103,116));c.drawCircle(centerX+(i-1)*18,y,6,p);}}
    private void drawHeart(Canvas c,float x,float y,float r,int color){ Path heart=new Path(); heart.moveTo(x,y+r*.86f); heart.cubicTo(x-r*1.12f,y+r*.10f,x-r*.92f,y-r*.68f,x-r*.43f,y-r*.68f); heart.cubicTo(x-r*.12f,y-r*.68f,x,y-r*.39f,x,y-r*.13f); heart.cubicTo(x,y-r*.39f,x+r*.12f,y-r*.68f,x+r*.43f,y-r*.68f); heart.cubicTo(x+r*.92f,y-r*.68f,x+r*1.12f,y+r*.10f,x,y+r*.86f); heart.close(); p.setColor(color); c.drawPath(heart,p); }
    private Path triangle(float a,float b,float c,float d,float e,float f){Path q=new Path();q.moveTo(a,b);q.lineTo(c,d);q.lineTo(e,f);q.close();return q;}
    private RectF playerRect(){return new RectF(px-18,py,px+18,py+54);}

    @Override public boolean onTouchEvent(MotionEvent event) {
        if (event.getActionMasked() == MotionEvent.ACTION_UP) {
            performClick();
        }
        return inputHandler.onTouchEvent(this, event);
    }

    @Override public boolean performClick() {
        super.performClick();
        return true;
    }

    int hitAction(float x,float y){
        if(screen==SPLASH)return 100;
        if(screen==MENU){if(in(x,y,92,335,355,405))return 101;if(in(x,y,92,425,355,485))return 102;if(in(x,y,92,505,355,565))return 103;if(in(x,y,1060,600,1235,660))return 105;}
        if(screen==MAP){if(in(x,y,1100,28,1235,82))return 104;float[][]n={{160,478},{265,395},{350,310},{455,255},{570,320},{685,390},{790,450},{920,385},{1030,300},{1135,210}};for(int i=0;i<n.length;i++)if(Math.hypot(x-n[i][0],y-n[i][1])<40&&i+1<=save.unlockedLevel())return 120+i;}
        if(screen==UPGRADES){if(in(x,y,85,360,315,420))return 110;if(in(x,y,380,360,610,420))return 111;if(in(x,y,675,360,905,420))return 112;if(in(x,y,970,360,1200,420))return 118;if(in(x,y,690,593,910,647))return 119;if(in(x,y,80,615,235,675))return 104;}
        if(screen==SETTINGS){if(in(x,y,330,240,620,310))return 130;if(in(x,y,660,240,950,310))return 131;if(in(x,y,470,500,810,560))return 113;if(in(x,y,470,585,810,645))return 104;}
        if(screen==DEV_TOOLS){for(int index=0;index<10;index++){float left=180+(index%5)*185;float top=index<5?185:270;if(in(x,y,left,top,left+150,top+58))return 140+index;}if(in(x,y,210,385,520,445))return 150;if(in(x,y,560,385,870,445))return 151;if(in(x,y,210,525,520,585))return 152;if(in(x,y,650,525,960,585))return 104;}
        if(screen==PAUSE){if(in(x,y,490,285,790,345))return 114;if(in(x,y,490,375,790,435))return 132;if(in(x,y,490,465,790,525))return 133;}
        if(screen==COMPLETE){if(in(x,y,440,500,665,565))return 115;if(in(x,y,690,500,850,565))return 104;}
        if(screen==GAMEOVER){if(in(x,y,465,370,655,430))return 116;if(in(x,y,675,370,820,430))return 104;}
        if(screen==LEVEL){if(in(x,y,40,575,145,680))return 1;if(in(x,y,150,575,255,680))return 2;if(Math.hypot(x-1070,y-626)<58)return 3;if(Math.hypot(x-960,y-545)<52)return 4;if(Math.hypot(x-1188,y-626)<58)return 5;if(in(x,y,1140,18,1260,72))return 6;if(in(x,y,950,18,1115,72))return 7;}
        return 0;
    }
    void handleAction(int a){
        if(a==1)directionTap(-1);if(a==2)directionTap(1);if(a==3)jumpQueued=true;if(a==4)strike();if(a==5)usePower();if(a==6){screen=PAUSE;audio.pauseMusic();}if(a==7)cyclePower();
        if(a==100)screen=MENU;if(a==101)screen=MAP;if(a==102)screen=UPGRADES;if(a==103)screen=SETTINGS;if(a==104)screen=MENU;if(a==105)screen=DEV_TOOLS;if(a==133){screen=MAP;audio.resumeMusic();}if(a==130){save.toggleMusic();audio.configure(save.musicEnabled(),save.sfxEnabled());}if(a==131){save.toggleSfx();audio.configure(save.musicEnabled(),save.sfxEnabled());}if(a==150)devStatsOverlay=!devStatsOverlay;if(a==151)devCoordinatesOverlay=!devCoordinatesOverlay;if(a==152){save.reset();audio.configure(true,true);}
        if(a>=100)audio.menu();if(a>=120&&a<130)startLevel(a-119);if(a>=140&&a<150)startLevel(a-139);if(a==110&&save.buy("attack",30))audio.upgrade();if(a==111&&save.buy("vitality",30))audio.upgrade();if(a==112&&save.buy("wind",30))audio.upgrade();if(a==118&&save.buyWithGems("energy",3))audio.upgrade();if(a==119&&save.unlockRelic())audio.upgrade();if(a==113){save.reset();audio.configure(true,true);}if(a==114){screen=LEVEL;audio.resumeMusic();}if(a==115){if(currentLevel<10)startLevel(currentLevel+1);else screen=MAP;}if(a==116)startLevel(currentLevel);if(a==132)startLevel(currentLevel);
    }
    void refreshHeld() {
        leftHeld = inputHandler.isHeld(1);
        rightHeld = inputHandler.isHeld(2);
        jumpHeld = inputHandler.isHeld(3);
    }

    boolean isStoryOverlayVisible() {
        return screen == LEVEL && storyTime > 0;
    }

    void dismissStoryOverlay() {
        storyTime = 0;
    }

    int screenForRenderer() {
        return screen;
    }
    private boolean targetInFront(float targetX) {
        return CombatSystem.isTargetInFront(facingLeft, px, targetX);
    }
    private float approach(float value, float target, float amount) {
        return PlayerController.approach(value, target, amount);
    }
    private boolean in(float x,float y,float l,float t,float r,float b){return x>=l&&x<=r&&y>=t&&y<=b;}

    private static class JuiceParticle {
        float x, y, vx, vy, life, maxLife, size;
        int color;

        JuiceParticle(float x, float y, float vx, float vy, float life, float size, int color) {
            this.x = x;
            this.y = y;
            this.vx = vx;
            this.vy = vy;
            this.life = life;
            this.maxLife = life;
            this.size = size;
            this.color = color;
        }
    }

    private static class Platform {
        float x, y, w, h, life = 2.6f, baseX, baseY, moveX, moveY, moveSpeed, frameDeltaX;
        boolean crumble;
        String material;

        Platform(float x, float y, float w, float h, boolean crumble, String material,
                float moveX, float moveY, float moveSpeed) {
            this.x = this.baseX = x;
            this.y = this.baseY = y;
            this.w = w;
            this.h = h;
            this.crumble = crumble;
            this.material = material;
            this.moveX = moveX;
            this.moveY = moveY;
            this.moveSpeed = moveSpeed;
        }

        void update(float clock) {
            float previousX = x;
            if (moveSpeed > 0) {
                float wave = (float)Math.sin(clock * moveSpeed + baseX * .01f);
                x = baseX + moveX * wave;
                y = baseY + moveY * wave;
            }
            frameDeltaX = x - previousX;
        }
    }

    private static class WindZone {
        float left, top, right, bottom, forceX;
        WindZone(float left, float top, float right, float bottom, float forceX) {
            this.left = left; this.top = top; this.right = right; this.bottom = bottom; this.forceX = forceX;
        }
        boolean contains(float x, float y) { return x >= left && x <= right && y >= top && y <= bottom; }
    }

    private static class HeatZone {
        float left, top, right, bottom;
        HeatZone(float left, float top, float right, float bottom) {
            this.left = left; this.top = top; this.right = right; this.bottom = bottom;
        }
        RectF rect() { return new RectF(left, top, right, bottom); }
    }

    private static class IcicleSpawner {
        float x, spawnY, landingY, interval, timer;
        IcicleSpawner(float x, float spawnY, float landingY, float interval) {
            this.x = x; this.spawnY = spawnY; this.landingY = landingY; this.interval = interval; this.timer = interval * .55f;
        }
    }

    private static class FallingIcicle {
        float x, y, landingY, velocityY;
        FallingIcicle(float x, float y, float landingY) { this.x = x; this.y = y; this.landingY = landingY; }
        RectF rect() { return new RectF(x - 10, y - 20, x + 10, y + 16); }
    }
    private static class Pickup { float x,y; boolean gem; String route, secretId; int secretRewardGems; Pickup(float x,float y,boolean gem,String route,String secretId,int secretRewardGems){this.x=x;this.y=y;this.gem=gem;this.route=route;this.secretId=secretId;this.secretRewardGems=secretRewardGems;} RectF rect(){return new RectF(x-16,y-16,x+16,y+16);} }
    private static class Hazard { float l,t,r,b,life,warning,age; int style; Hazard(float l,float t,float r,float b,float life,float warning){this(l,t,r,b,life,warning,0);} Hazard(float l,float t,float r,float b,float life,float warning,int style){this.l=l;this.t=t;this.r=r;this.b=b;this.life=life;this.warning=warning;this.style=style;} boolean active(){return age>=warning;} RectF rect(){return new RectF(l,t,r,b);} }
    private static class Foe {
        float x, y, baseY, targetX, targetY, minX, maxX, dir = 1, speed, hurtTime, hitLock, stateTime;
        int kind, hp, state;
        float frozen, burnTime, burnTick;

        Foe(float x, float y, int kind) {
            EnemyController.Archetype archetype = EnemyController.archetype(kind);
            this.x = x; this.y = y; this.baseY = y; this.kind = kind;
            minX = x - 65; maxX = x + 65;
            speed = archetype.patrolSpeed;
            hp = archetype.maxHealth;
        }
    }

    private static class EnemyProjectile {
        float x, y, velocityX, velocityY, life = 2.6f;
        EnemyProjectile(float x, float y, float velocityX, float velocityY) {
            this.x = x; this.y = y; this.velocityX = velocityX; this.velocityY = velocityY;
        }
        RectF rect() { return new RectF(x - 10, y - 10, x + 10, y + 10); }
    }
    private static class Boss {
        float x,y,dir=-1,cooldown=1.8f,hitLock,burnTime,burnTick,frostSlowTime,transitionTime;
        int hp=24,maxHp=24,phase=1,world,attackCycle;
        String name;
        // Charge attack: boss rushes horizontally at high speed toward the player's last known lane.
        float chargeTelegraph, chargeTime, chargeVX, chargeDamageLock;
        // Leap-slam attack: boss rises then slams down, spawning a wider shockwave hazard on landing.
        float leapTelegraph, leapTime; boolean leapSlammed; float leapStartY, leapPeakOffset=-130f;
        // Ranged attack cooldown, tracked independently so it can interleave with melee attacks.
        float rangedCooldown = 2.4f;
        Boss(float x,float y,String n,int w){this.x=x;this.y=y;name=n;world=w;}
    }
}
