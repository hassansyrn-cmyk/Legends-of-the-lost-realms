package com.manus.lostrealms;

import android.content.Context;
import android.media.AudioAttributes;
import android.media.MediaPlayer;
import android.media.SoundPool;
import java.util.HashMap;

/** Handles looping realm music and short gameplay feedback sounds. */
public class GameAudio {
    private MediaPlayer music;
    private SoundPool sfx;
    private int musicResId = 0;
    private boolean musicEnabled = true, sfxEnabled = true;
    private final HashMap<String, Integer> clips = new HashMap<>();

    public void startMusic(Context context) {
        loadSfx(context);
        playMusic(context, R.raw.verdant_theme);
    }

    public void configure(boolean musicOn, boolean sfxOn) {
        musicEnabled = musicOn;
        sfxEnabled = sfxOn;
        if (music == null) return;
        if (!musicEnabled && music.isPlaying()) music.pause();
        else if (musicEnabled && !music.isPlaying()) music.start();
    }

    /** Chooses the exploration score for a realm, or the battle score for a boss arena. */
    public void setLevelMusic(Context context, int world, boolean bossBattle) {
        loadSfx(context);
        if (bossBattle) playMusic(context, R.raw.boss_battle_theme);
        else if (world == 2) playMusic(context, R.raw.desert_exploration_theme);
        else if (world == 3) playMusic(context, R.raw.frozen_exploration_theme);
        else playMusic(context, R.raw.verdant_theme);
    }

    private void playMusic(Context context, int resourceId) {
        if (music != null && musicResId == resourceId) return;
        if (music != null) {
            if (music.isPlaying()) music.stop();
            music.release();
            music = null;
        }
        musicResId = resourceId;
        music = MediaPlayer.create(context, resourceId);
        if (music != null) {
            music.setLooping(true);
            music.setVolume(0.28f, 0.28f);
            if (musicEnabled) music.start();
        }
    }

    private void loadSfx(Context context) {
        if (sfx != null) return;
        AudioAttributes attributes = new AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_GAME)
            .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
            .build();
        sfx = new SoundPool.Builder().setMaxStreams(10).setAudioAttributes(attributes).build();
        clips.put("menu", sfx.load(context, R.raw.sfx_menu, 1));
        clips.put("jump", sfx.load(context, R.raw.sfx_jump, 1));
        clips.put("double", sfx.load(context, R.raw.sfx_double_jump, 1));
        clips.put("blade", sfx.load(context, R.raw.sfx_blade, 1));
        clips.put("impact", sfx.load(context, R.raw.sfx_impact, 1));
        clips.put("enemy", sfx.load(context, R.raw.sfx_enemy_defeat, 1));
        clips.put("hurt", sfx.load(context, R.raw.sfx_hurt, 1));
        clips.put("defeat", sfx.load(context, R.raw.sfx_defeat, 1));
        clips.put("coin", sfx.load(context, R.raw.sfx_coin, 1));
        clips.put("gem", sfx.load(context, R.raw.sfx_gem, 1));
        clips.put("checkpoint", sfx.load(context, R.raw.sfx_checkpoint, 1));
        clips.put("power", sfx.load(context, R.raw.sfx_power, 1));
        clips.put("boss", sfx.load(context, R.raw.sfx_boss, 1));
        clips.put("complete", sfx.load(context, R.raw.sfx_complete, 1));
        clips.put("upgrade", sfx.load(context, R.raw.sfx_upgrade, 1));
        clips.put("step", sfx.load(context, R.raw.sfx_step, 1));
        clips.put("landSoft", sfx.load(context, R.raw.sfx_land_soft, 1));
        clips.put("landHard", sfx.load(context, R.raw.sfx_land_hard, 1));
        clips.put("enemyWarning", sfx.load(context, R.raw.sfx_enemy_warning, 1));
        clips.put("enemyDash", sfx.load(context, R.raw.sfx_enemy_dash, 1));
        clips.put("enemySwoop", sfx.load(context, R.raw.sfx_enemy_swoop, 1));
        clips.put("powerSelect", sfx.load(context, R.raw.sfx_power_select, 1));
        clips.put("powerFail", sfx.load(context, R.raw.sfx_power_fail, 1));
        clips.put("respawn", sfx.load(context, R.raw.sfx_respawn, 1));
        clips.put("bossWarning", sfx.load(context, R.raw.sfx_boss_warning, 1));
        clips.put("frostCast", sfx.load(context, R.raw.sfx_frost_cast, 1));
        clips.put("emberCast", sfx.load(context, R.raw.sfx_ember_cast, 1));
        clips.put("galeCast", sfx.load(context, R.raw.sfx_gale_cast, 1));
        clips.put("playerDash", sfx.load(context, R.raw.sfx_player_dash, 1));
        clips.put("airStrike", sfx.load(context, R.raw.sfx_air_strike, 1));
    }

    private void play(String key, float volume) { play(key, volume, 1f); }

    private void play(String key, float volume, float rate) {
        if (!sfxEnabled || sfx == null || !clips.containsKey(key)) return;
        sfx.play(clips.get(key), volume, volume, 1, 0, rate);
    }

    public void pauseMusic() { if (music != null && music.isPlaying()) music.pause(); }
    public void resumeMusic() { if (musicEnabled && music != null && !music.isPlaying()) music.start(); }
    public void menu() { play("menu", .55f); }
    public void jump(boolean doubleJump) { play(doubleJump ? "double" : "jump", .72f); }
    public void step(float speed) { play("step", .24f, speed > 260 ? 1.12f : .95f); }
    public void land(boolean hard) { play(hard ? "landHard" : "landSoft", hard ? .52f : .35f); }
    public void playerDash() { play("playerDash", .58f); }
    public void airStrike() { play("airStrike", .62f); }
    public void attack() { play("blade", .62f); }
    public void impact() { play("impact", .66f); }
    public void enemyDefeat() { play("enemy", .72f); }
    public void enemyWarning() { play("enemyWarning", .46f); }
    public void enemyDash() { play("enemyDash", .48f); }
    public void enemySwoop() { play("enemySwoop", .45f); }
    public void hurt() { play("hurt", .76f); }
    public void defeat() { play("defeat", .78f); }
    public void collect(boolean gem) { play(gem ? "gem" : "coin", gem ? .78f : .62f); }
    public void checkpoint() { play("checkpoint", .70f); }
    public void powerFail() { play("powerFail", .58f); }
    public void powerSelect() { play("powerSelect", .54f); }
    public void powerCast(int power) {
        if (power == 0) play("emberCast", .62f);
        else if (power == 1) play("frostCast", .60f);
        else play("galeCast", .60f);
    }
    public void respawn() { play("respawn", .70f); }
    public void power() { play("power", .72f); }
    public void bossImpact() { play("boss", .80f); }
    public void bossWarning() { play("bossWarning", .66f); }
    public void win() { play("complete", .80f); }
    public void upgrade() { play("upgrade", .72f); }

    public void release() {
        if (music != null) { music.release(); music = null; musicResId = 0; }
        if (sfx != null) { sfx.release(); sfx = null; clips.clear(); }
    }
}
