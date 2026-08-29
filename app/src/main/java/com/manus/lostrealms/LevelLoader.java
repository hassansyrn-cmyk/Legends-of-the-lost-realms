package com.manus.lostrealms;

import android.content.Context;
import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.util.ArrayList;
import org.json.JSONArray;
import org.json.JSONObject;

/** Loads immutable level definitions from assets/levels/level_{id}.json. */
final class LevelLoader {
    private LevelLoader() {
    }

    static LevelDefinition load(Context context, int levelId) {
        String assetName = "levels/level_" + levelId + ".json";
        try (InputStream stream = context.getAssets().open(assetName);
             BufferedReader reader = new BufferedReader(new InputStreamReader(stream))) {
            StringBuilder jsonText = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) {
                jsonText.append(line);
            }
            return parse(new JSONObject(jsonText.toString()));
        } catch (Exception error) {
            throw new IllegalStateException("Unable to load " + assetName, error);
        }
    }

    private static LevelDefinition parse(JSONObject root) throws Exception {
        JSONObject design = root.getJSONObject("design");
        LevelDefinition definition = new LevelDefinition(
                root.getInt("id"),
                root.getString("stageName"),
                root.optString("story", "none"),
                (float) root.getJSONObject("checkpoint").getDouble("x"),
                (float) root.getJSONObject("checkpoint").getDouble("y"),
                design.getString("role"),
                design.getString("primaryMechanic"),
                design.getString("playerBrief"));

        JSONArray platforms = root.getJSONArray("platforms");
        for (int index = 0; index < platforms.length(); index++) {
            JSONObject platform = platforms.getJSONObject(index);
            definition.platforms.add(new PlatformData(
                    (float) platform.getDouble("x"),
                    (float) platform.getDouble("y"),
                    (float) platform.getDouble("width"),
                    platform.optBoolean("crumble", false),
                    platform.optString("material", "STONE"),
                    (float) platform.optDouble("moveX", 0),
                    (float) platform.optDouble("moveY", 0),
                    (float) platform.optDouble("moveSpeed", 0)));
        }

        JSONArray pickups = root.getJSONArray("pickups");
        for (int index = 0; index < pickups.length(); index++) {
            JSONObject pickup = pickups.getJSONObject(index);
            definition.pickups.add(new PickupData(
                    (float) pickup.getDouble("x"),
                    (float) pickup.getDouble("y"),
                    pickup.optBoolean("gem", false),
                    pickup.optString("route", "SAFE"),
                    pickup.optString("secretId", ""),
                    pickup.optInt("secretRewardGems", 0)));
        }

        JSONArray foes = root.getJSONArray("foes");
        for (int index = 0; index < foes.length(); index++) {
            JSONObject foe = foes.getJSONObject(index);
            definition.foes.add(new FoeData(
                    (float) foe.getDouble("x"),
                    (float) foe.getDouble("y"),
                    foe.getInt("kind")));
        }

        JSONArray hazards = root.getJSONArray("hazards");
        for (int index = 0; index < hazards.length(); index++) {
            JSONObject hazard = hazards.getJSONObject(index);
            definition.hazards.add(new HazardData(
                    (float) hazard.getDouble("left"),
                    (float) hazard.getDouble("top"),
                    (float) hazard.getDouble("right"),
                    (float) hazard.getDouble("bottom")));
        }

        JSONObject environment = root.getJSONObject("environment");
        JSONArray windZones = environment.getJSONArray("windZones");
        for (int index = 0; index < windZones.length(); index++) {
            JSONObject zone = windZones.getJSONObject(index);
            definition.windZones.add(new WindZoneData(
                    (float) zone.getDouble("left"),
                    (float) zone.getDouble("top"),
                    (float) zone.getDouble("right"),
                    (float) zone.getDouble("bottom"),
                    (float) zone.getDouble("forceX")));
        }
        JSONArray heatZones = environment.getJSONArray("heatZones");
        for (int index = 0; index < heatZones.length(); index++) {
            JSONObject zone = heatZones.getJSONObject(index);
            definition.heatZones.add(new HeatZoneData(
                    (float) zone.getDouble("left"),
                    (float) zone.getDouble("top"),
                    (float) zone.getDouble("right"),
                    (float) zone.getDouble("bottom")));
        }
        JSONArray icicleSpawners = environment.getJSONArray("icicleSpawners");
        for (int index = 0; index < icicleSpawners.length(); index++) {
            JSONObject spawner = icicleSpawners.getJSONObject(index);
            definition.icicleSpawners.add(new IcicleSpawnerData(
                    (float) spawner.getDouble("x"),
                    (float) spawner.getDouble("spawnY"),
                    (float) spawner.getDouble("landingY"),
                    (float) spawner.getDouble("interval")));
        }

        if (!root.isNull("boss")) {
            JSONObject boss = root.getJSONObject("boss");
            definition.boss = new BossData(
                    (float) boss.getDouble("x"),
                    (float) boss.getDouble("y"),
                    boss.getString("name"),
                    boss.getInt("world"));
        }
        return definition;
    }

    static final class LevelDefinition {
        final int id;
        final String stageName;
        final String story;
        final float checkpointX;
        final float checkpointY;
        final String designRole;
        final String primaryMechanic;
        final String playerBrief;
        final ArrayList<PlatformData> platforms = new ArrayList<>();
        final ArrayList<PickupData> pickups = new ArrayList<>();
        final ArrayList<FoeData> foes = new ArrayList<>();
        final ArrayList<HazardData> hazards = new ArrayList<>();
        final ArrayList<WindZoneData> windZones = new ArrayList<>();
        final ArrayList<HeatZoneData> heatZones = new ArrayList<>();
        final ArrayList<IcicleSpawnerData> icicleSpawners = new ArrayList<>();
        BossData boss;

        LevelDefinition(int id, String stageName, String story, float checkpointX, float checkpointY,
                String designRole, String primaryMechanic, String playerBrief) {
            this.id = id;
            this.stageName = stageName;
            this.story = story;
            this.checkpointX = checkpointX;
            this.checkpointY = checkpointY;
            this.designRole = designRole;
            this.primaryMechanic = primaryMechanic;
            this.playerBrief = playerBrief;
        }
    }

    static final class PlatformData {
        final float x;
        final float y;
        final float width;
        final boolean crumble;
        final String material;
        final float moveX;
        final float moveY;
        final float moveSpeed;

        PlatformData(float x, float y, float width, boolean crumble, String material,
                float moveX, float moveY, float moveSpeed) {
            this.x = x;
            this.y = y;
            this.width = width;
            this.crumble = crumble;
            this.material = material;
            this.moveX = moveX;
            this.moveY = moveY;
            this.moveSpeed = moveSpeed;
        }
    }

    static final class PickupData {
        final float x;
        final float y;
        final boolean gem;
        final String route;
        final String secretId;
        final int secretRewardGems;

        PickupData(float x, float y, boolean gem, String route, String secretId, int secretRewardGems) {
            this.x = x;
            this.y = y;
            this.gem = gem;
            this.route = route;
            this.secretId = secretId;
            this.secretRewardGems = secretRewardGems;
        }
    }

    static final class FoeData {
        final float x;
        final float y;
        final int kind;

        FoeData(float x, float y, int kind) {
            this.x = x;
            this.y = y;
            this.kind = kind;
        }
    }

    static final class HazardData {
        final float left;
        final float top;
        final float right;
        final float bottom;

        HazardData(float left, float top, float right, float bottom) {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }

    static final class WindZoneData {
        final float left;
        final float top;
        final float right;
        final float bottom;
        final float forceX;

        WindZoneData(float left, float top, float right, float bottom, float forceX) {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.forceX = forceX;
        }
    }

    static final class HeatZoneData {
        final float left;
        final float top;
        final float right;
        final float bottom;

        HeatZoneData(float left, float top, float right, float bottom) {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }

    static final class IcicleSpawnerData {
        final float x;
        final float spawnY;
        final float landingY;
        final float interval;

        IcicleSpawnerData(float x, float spawnY, float landingY, float interval) {
            this.x = x;
            this.spawnY = spawnY;
            this.landingY = landingY;
            this.interval = interval;
        }
    }

    static final class BossData {
        final float x;
        final float y;
        final String name;
        final int world;

        BossData(float x, float y, String name, int world) {
            this.x = x;
            this.y = y;
            this.name = name;
            this.world = world;
        }
    }
}
