package com.manus.lostrealms;

import android.content.Context;
import android.content.SharedPreferences;

/**
 * Local persistence for progression, preferences, best times, and lightweight
 * player statistics. Existing save keys are never renamed or removed during a
 * schema upgrade, so older progress remains valid.
 */
public class SaveManager {
    private static final String PREFS = "lost_realms_save";
    private static final String KEY_SCHEMA_VERSION = "save_schema_version";
    private static final int CURRENT_SCHEMA_VERSION = 5;
    private final SharedPreferences prefs;

    public SaveManager(Context context) {
        prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        migrateIfNeeded();
    }

    private void migrateIfNeeded() {
        int storedVersion = prefs.getInt(KEY_SCHEMA_VERSION, 0);
        if (storedVersion >= CURRENT_SCHEMA_VERSION) {
            return;
        }
        // Versions 1–3 used the existing progression keys. Versions 4–5 only
        // add new keys with defaults, preserving all established values.
        prefs.edit().putInt(KEY_SCHEMA_VERSION, CURRENT_SCHEMA_VERSION).apply();
    }

    public int schemaVersion() { return prefs.getInt(KEY_SCHEMA_VERSION, CURRENT_SCHEMA_VERSION); }
    public int coins() { return prefs.getInt("coins", 0); }
    public int unlockedLevel() { return prefs.getInt("unlocked", 1); }
    public int attackRank() { return prefs.getInt("attack", 0); }
    public int vitalityRank() { return prefs.getInt("vitality", 0); }
    public int windRank() { return prefs.getInt("wind", 0); }
    public int gems() { return prefs.getInt("gems", 0); }
    public int energyRank() { return prefs.getInt("energy", 0); }
    public int relicRank() { return prefs.getInt("relic", 0); }
    public boolean musicEnabled() { return prefs.getBoolean("music_enabled", true); }
    public boolean sfxEnabled() { return prefs.getBoolean("sfx_enabled", true); }

    public void toggleMusic() { prefs.edit().putBoolean("music_enabled", !musicEnabled()).apply(); }
    public void toggleSfx() { prefs.edit().putBoolean("sfx_enabled", !sfxEnabled()).apply(); }
    public int levelStars(int level) { return prefs.getInt("stars_" + level, 0); }
    public long bestTimeMillis(int level) { return prefs.getLong("best_time_" + level, 0L); }
    public int runsStarted() { return prefs.getInt("stat_runs_started", 0); }
    public int runsCompleted() { return prefs.getInt("stat_runs_completed", 0); }
    public int totalDeaths() { return prefs.getInt("stat_deaths", 0); }
    public int enemiesDefeated() { return prefs.getInt("stat_enemies_defeated", 0); }
    public int coinsCollected() { return prefs.getInt("stat_coins_collected", 0); }
    public int gemsCollected() { return prefs.getInt("stat_gems_collected", 0); }
    public int bossesDefeated() { return prefs.getInt("stat_bosses_defeated", 0); }
    public int secretsFound() { return prefs.getInt("stat_secrets_found", 0); }
    public boolean hasSecret(String secretId) { return prefs.getBoolean("secret_" + secretId, false); }

    public int totalStars() {
        int total = 0;
        for (int level = 1; level <= 10; level++) total += levelStars(level);
        return total;
    }

    /** Keeps only a player's best objective score for a level. */
    public boolean recordLevelStars(int level, int stars) {
        int best = levelStars(level);
        if (stars <= best) return false;
        prefs.edit().putInt("stars_" + level, Math.min(3, stars)).apply();
        return true;
    }

    public void recordRunStarted() {
        prefs.edit().putInt("stat_runs_started", runsStarted() + 1).apply();
    }

    public void recordDeath() {
        prefs.edit().putInt("stat_deaths", totalDeaths() + 1).apply();
    }

    public void recordEnemyDefeats(int count) {
        if (count <= 0) return;
        prefs.edit().putInt("stat_enemies_defeated", enemiesDefeated() + count).apply();
    }

    public void recordPickup(boolean gem) {
        String key = gem ? "stat_gems_collected" : "stat_coins_collected";
        int previous = gem ? gemsCollected() : coinsCollected();
        prefs.edit().putInt(key, previous + 1).apply();
    }

    /** Records a unique secret cache once and returns whether it was new. */
    public boolean recordSecretFound(String secretId) {
        if (secretId == null || secretId.isEmpty() || hasSecret(secretId)) return false;
        prefs.edit()
                .putBoolean("secret_" + secretId, true)
                .putInt("stat_secrets_found", secretsFound() + 1)
                .apply();
        return true;
    }

    /** Stores completion statistics and preserves only the lowest time per level. */
    public CompletionResult recordLevelCompletion(int level, long elapsedMillis, boolean bossLevel) {
        long safeElapsed = Math.max(1L, elapsedMillis);
        long previousBest = bestTimeMillis(level);
        boolean newBest = previousBest == 0L || safeElapsed < previousBest;
        SharedPreferences.Editor editor = prefs.edit()
                .putInt("stat_runs_completed", runsCompleted() + 1);
        if (newBest) editor.putLong("best_time_" + level, safeElapsed);
        if (bossLevel) editor.putInt("stat_bosses_defeated", bossesDefeated() + 1);
        editor.apply();
        return new CompletionResult(newBest, newBest ? safeElapsed : previousBest);
    }

    public void addRewards(int coins, int gems) {
        prefs.edit().putInt("coins", coins() + coins).putInt("gems", gems() + gems).apply();
    }

    public void unlockAfter(int completedLevel) {
        int next = Math.min(10, completedLevel + 1);
        if (next > unlockedLevel()) prefs.edit().putInt("unlocked", next).apply();
    }

    public boolean buy(String key, int cost) {
        if (coins() < cost) return false;
        int value = prefs.getInt(key, 0);
        if (value >= 3) return false;
        prefs.edit().putInt("coins", coins() - cost).putInt(key, value + 1).apply();
        return true;
    }

    public boolean buyWithGems(String key, int cost) {
        if (gems() < cost) return false;
        int value = prefs.getInt(key, 0);
        if (value >= 3) return false;
        prefs.edit().putInt("gems", gems() - cost).putInt(key, value + 1).apply();
        return true;
    }

    public int relicThreshold() {
        int rank = relicRank();
        return rank == 0 ? 6 : rank == 1 ? 14 : rank == 2 ? 24 : 30;
    }

    /** Star upgrades are permanent milestones and never consume collected stars. */
    public boolean unlockRelic() {
        int rank = relicRank();
        if (rank >= 3 || totalStars() < relicThreshold()) return false;
        prefs.edit().putInt("relic", rank + 1).apply();
        return true;
    }

    public void reset() {
        prefs.edit().clear().putInt(KEY_SCHEMA_VERSION, CURRENT_SCHEMA_VERSION).apply();
    }

    public static String formatDuration(long millis) {
        long tenths = Math.max(0L, millis) / 100L;
        long minutes = tenths / 600L;
        long seconds = (tenths / 10L) % 60L;
        long decimal = tenths % 10L;
        return String.format(java.util.Locale.US, "%02d:%02d.%d", minutes, seconds, decimal);
    }

    public static final class CompletionResult {
        public final boolean newBestTime;
        public final long bestTimeMillis;

        CompletionResult(boolean newBestTime, long bestTimeMillis) {
            this.newBestTime = newBestTime;
            this.bestTimeMillis = bestTimeMillis;
        }
    }
}
